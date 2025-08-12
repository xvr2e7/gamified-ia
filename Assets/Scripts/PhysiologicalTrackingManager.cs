using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;
using Debug = UnityEngine.Debug;

public class PhysiologicalTrackingManager : MonoBehaviour
{
    [Header("Tracking References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Camera mainCamera;

    [Header("Sampling")]
    [SerializeField, Range(30, 200)] private int targetHz = 90;   // background sampler rate
    [SerializeField] private float gazeRayLength = 25f;
    [SerializeField] private float headRayLength = 25f;
    [SerializeField] private LayerMask raycastMask = ~0;

    [Header("Simulator Settings")]
    [SerializeField] private bool forceSimulatorMode = false;

    // Per-image storage (consumed by StudyDataLogger)
    private readonly Dictionary<int, List<PhysiologicalTrackingData>> imageTrackingData = new Dictionary<int, List<PhysiologicalTrackingData>>();
    private readonly List<PhysiologicalTrackingData> currentImageData = new List<PhysiologicalTrackingData>();
    private int currentImageIndex = -1;

    // Environment
    private bool useSimulator = false;
    private bool eyeTrackingAvailable = false;

    // Decoupling: latest snapshots (main thread) -> queue (background 90 Hz)
    private readonly object latestLock = new object();
    private PhysiologicalTrackingData latestHead;

    private Thread samplerThread;
    private volatile bool running;
    private double tickPeriodMs;

    // Thread-safe queue for sampled records
    private readonly ConcurrentQueue<PhysiologicalTrackingData> outQueue = new ConcurrentQueue<PhysiologicalTrackingData>();

    // For sampler-thread timestamps
    private double samplerBaseSeconds = 0.0; // captured on main thread when starting
    private System.Diagnostics.Stopwatch samplerClock; // used in the sampler thread

    void OnEnable()
    {
        if (xrOrigin == null)
        {
            var xrOriginGO = GameObject.Find("XR Origin");
            if (xrOriginGO != null) xrOrigin = xrOriginGO.transform;
        }
        if (mainCamera == null) mainCamera = Camera.main;

        DetectEnvironment();
        tickPeriodMs = 1000.0 / Math.Max(1, targetHz);
    }

    void OnDisable()
    {
        StopSampling();
        DrainQueueToImage();
    }

    void OnApplicationQuit()
    {
        StopSampling();
    }

    private void DetectEnvironment()
    {
#if UNITY_EDITOR
        var deviceSimulator = FindObjectOfType<XRDeviceSimulator>();
        useSimulator = forceSimulatorMode || (deviceSimulator != null);
#else
        useSimulator = false;
#endif
        if (useSimulator)
        {
            Debug.Log("[Physio] Simulator mode: eye tracking disabled.");
            eyeTrackingAvailable = false;
            return;
        }

        try
        {
            XrSingleEyeGazeDataHTC[] gazeData;
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);
            eyeTrackingAvailable = gazeData != null && gazeData.Length >= 2;
            Debug.Log(eyeTrackingAvailable
                ? "[Physio] HTC Vive eye tracking available."
                : "[Physio] HTC Vive eye tracking NOT available.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Physio] Eye-tracking probe failed: {ex.Message}");
            eyeTrackingAvailable = false;
        }
    }

    // =====================================================================
    // Public control (used by study flow & logger)
    // =====================================================================

    public void StartTrackingForImage(int imageIndex)
    {
        // Flush previous image
        if (currentImageIndex >= 0 && currentImageData.Count > 0)
            imageTrackingData[currentImageIndex] = new List<PhysiologicalTrackingData>(currentImageData);

        currentImageIndex = imageIndex;
        currentImageData.Clear();

        StartSampling();
        Debug.Log($"[Physio] Started tracking for image {imageIndex}");
    }

    public void StopTracking()
    {
        StopSampling();

        if (currentImageIndex >= 0 && currentImageData.Count > 0)
            imageTrackingData[currentImageIndex] = new List<PhysiologicalTrackingData>(currentImageData);

        Debug.Log($"[Physio] Stopped tracking. Stored images: {imageTrackingData.Count}");
    }

    public Dictionary<int, List<PhysiologicalTrackingData>> GetAllTrackingData()
    {
        if (currentImageIndex >= 0 && currentImageData.Count > 0)
            imageTrackingData[currentImageIndex] = new List<PhysiologicalTrackingData>(currentImageData);

        return imageTrackingData;
    }

    public List<PhysiologicalTrackingData> GetTrackingDataForImage(int imageIndex)
    {
        if (imageTrackingData.TryGetValue(imageIndex, out var list)) return list;
        return new List<PhysiologicalTrackingData>();
    }

    public void ClearAllData()
    {
        imageTrackingData.Clear();
        currentImageData.Clear();
        currentImageIndex = -1;
        Debug.Log("[Physio] All tracking data cleared.");
    }

    // =====================================================================
    // Main thread: read XR once per frame, do raycasts here, cache snapshots
    // =====================================================================

    void Update()
    {
        float now = Time.unscaledTime;

        Vector3 headPos;
        Quaternion headRot;
        GetHeadPose(out headPos, out headRot);

        var combined = new PhysiologicalTrackingData(
            now, "Head", headPos, headRot * Vector3.forward, "None", true
        );

        if (!useSimulator && eyeTrackingAvailable)
        {
            try
            {
                XrSingleEyeGazeDataHTC[] gazeData;
                XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);
                if (gazeData != null && gazeData.Length >= 2)
                {
                    var leftGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                    var rightGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

                    PopulateEyeGazeData(ref combined.leftEye, leftGaze);
                    PopulateEyeGazeData(ref combined.rightEye, rightGaze);

                    // Perform gaze-based raycast
                    combined.hitObjectName = PerformGazeRaycast(combined.leftEye, combined.rightEye);

                    XrSingleEyePupilDataHTC[] pupilData;
                    XR_HTC_eye_tracker.Interop.GetEyePupilData(out pupilData);
                    if (pupilData != null && pupilData.Length >= 2)
                    {
                        var leftPupil = pupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                        var rightPupil = pupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
                        PopulatePupil(ref combined.leftEye, leftPupil);
                        PopulatePupil(ref combined.rightEye, rightPupil);
                    }

                    XrSingleEyeGeometricDataHTC[] geometryData;
                    XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out geometryData);
                    if (geometryData != null && geometryData.Length >= 2)
                    {
                        var leftGeometry = geometryData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                        var rightGeometry = geometryData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
                        PopulateGeometry(ref combined.leftEye, leftGeometry);
                        PopulateGeometry(ref combined.rightEye, rightGeometry);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Physio] Eye tracking read failed: {ex.Message}");
            }
        }
        else
        {
            // Fallback to head raycast when eye tracking unavailable
            combined.hitObjectName = RaycastName(headPos, headRot * Vector3.forward, headRayLength);
        }

        lock (latestLock)
        {
            latestHead = combined;
        }

        DrainQueueToImage();
    }

    private string PerformGazeRaycast(EyeData leftEye, EyeData rightEye)
    {
        Vector3 gazeOrigin = Vector3.zero;
        Vector3 gazeDirection = Vector3.forward;
        bool validGaze = false;

        // Try combined gaze (average of both eyes)
        if (leftEye.gazeValid && rightEye.gazeValid)
        {
            gazeOrigin = (leftEye.gazeWorldPosition + rightEye.gazeWorldPosition) * 0.5f;
            Vector3 leftDir = leftEye.gazeWorldRotation * Vector3.forward;
            Vector3 rightDir = rightEye.gazeWorldRotation * Vector3.forward;
            gazeDirection = ((leftDir + rightDir) * 0.5f).normalized;
            validGaze = true;
        }
        // Fallback to left eye only
        else if (leftEye.gazeValid)
        {
            gazeOrigin = leftEye.gazeWorldPosition;
            gazeDirection = leftEye.gazeWorldRotation * Vector3.forward;
            validGaze = true;
        }
        // Fallback to right eye only
        else if (rightEye.gazeValid)
        {
            gazeOrigin = rightEye.gazeWorldPosition;
            gazeDirection = rightEye.gazeWorldRotation * Vector3.forward;
            validGaze = true;
        }

        if (!validGaze)
        {
            return "None";
        }

        return RaycastName(gazeOrigin, gazeDirection, headRayLength);
    }

    private void GetHeadPose(out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero; rot = Quaternion.identity;

        var hmd = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        if (hmd.isValid)
        {
            if (!hmd.TryGetFeatureValue(CommonUsages.centerEyePosition, out pos))
                hmd.TryGetFeatureValue(CommonUsages.devicePosition, out pos);

            if (!hmd.TryGetFeatureValue(CommonUsages.centerEyeRotation, out rot))
                hmd.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);
        }
        else if (mainCamera != null)
        {
            pos = mainCamera.transform.position;
            rot = mainCamera.transform.rotation;
        }
    }

    private string RaycastName(Vector3 origin, Vector3 dir, float length)
    {
        if (dir.sqrMagnitude < 1e-8f) return "None";

        if (Physics.Raycast(origin, dir, out var hit, length, raycastMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.gameObject != null)
            {
                return hit.collider.gameObject.name;
            }
        }

        return "None";
    }

    // =====================================================================
    // Background 90 Hz sampler (copies latest snapshots)
    // =====================================================================

    private void StartSampling()
    {
        if (running) return;
        running = true;

        // capture base time ON MAIN THREAD (seconds)
        samplerBaseSeconds = Time.realtimeSinceStartupAsDouble;

        samplerThread = new Thread(SamplerLoop)
        {
            IsBackground = true,
            Name = "PhysioSampler90Hz"
        };
        samplerThread.Start();
        Debug.Log("[Physio] Sampler started.");
    }

    private void StopSampling()
    {
        if (!running) return;
        running = false;
        try { samplerThread?.Join(); } catch { /* ignore */ }
        samplerThread = null;

        DrainQueueToImage();
        Debug.Log("[Physio] Sampler stopped.");
    }

    private void SamplerLoop()
    {
        // Create the stopwatch IN the sampler thread
        samplerClock = new System.Diagnostics.Stopwatch();
        samplerClock.Start();

        double nextTickMs = samplerClock.Elapsed.TotalMilliseconds;
        double periodMs = tickPeriodMs;

        while (running)
        {
            double nowMs = samplerClock.Elapsed.TotalMilliseconds;
            double wait = nextTickMs - nowMs;
            if (wait > 1.0) Thread.Sleep((int)(wait - 0.5));
            while (samplerClock.Elapsed.TotalMilliseconds < nextTickMs) { /* sub-ms spin */ }

            // Copy single combined snapshot
            PhysiologicalTrackingData snapshot = null;
            lock (latestLock)
            {
                if (latestHead != null) snapshot = Clone(latestHead);
            }

            // Compute thread-safe timestamp (seconds since startup)
            float ts = (float)(samplerBaseSeconds + samplerClock.Elapsed.TotalSeconds);

            if (snapshot != null)
            {
                snapshot.timestamp = ts;
                snapshot.trackingSource = "Head@90Hz";
                outQueue.Enqueue(snapshot);
            }

            nextTickMs += periodMs;
        }
    }

    // =====================================================================
    // Data population helpers
    // =====================================================================

    private void PopulateEyeGazeData(ref EyeData eyeData, XrSingleEyeGazeDataHTC gazeData)
    {
        eyeData.gazeValid = gazeData.isValid;
        if (!gazeData.isValid || mainCamera == null) return;

        eyeData.gazeLocalPosition = gazeData.gazePose.position.ToUnityVector();
        eyeData.gazeLocalRotation = gazeData.gazePose.orientation.ToUnityQuaternion();

        var wPos = mainCamera.transform.TransformPoint(eyeData.gazeLocalPosition);
        var wRot = mainCamera.transform.rotation * eyeData.gazeLocalRotation;

        // drift-correct world rotation
        wRot = DriftCalibration.Apply(wRot);

        eyeData.gazeWorldPosition = wPos;
        eyeData.gazeWorldRotation = wRot;
    }

    private void PopulatePupil(ref EyeData eyeData, XrSingleEyePupilDataHTC pupil)
    {
        eyeData.pupilDiameterValid = pupil.isDiameterValid;
        eyeData.pupilPositionValid = pupil.isPositionValid;
        if (pupil.isDiameterValid) eyeData.pupilDiameter = pupil.pupilDiameter;
        if (pupil.isPositionValid) eyeData.pupilPosition = new Vector2(pupil.pupilPosition.x, pupil.pupilPosition.y);
    }

    private void PopulateGeometry(ref EyeData eyeData, XrSingleEyeGeometricDataHTC geometry)
    {
        eyeData.geometryValid = geometry.isValid;
        if (geometry.isValid)
        {
            eyeData.eyeOpenness = geometry.eyeOpenness;
            eyeData.eyeSqueeze = geometry.eyeSqueeze;
            eyeData.eyeWide = geometry.eyeWide;
        }
    }

    private void DrainQueueToImage()
    {
        if (currentImageIndex < 0)
        {
            // No active image → discard queued samples
            while (outQueue.TryDequeue(out _)) { }
            return;
        }
        while (outQueue.TryDequeue(out var item))
            currentImageData.Add(item);
    }

    private static PhysiologicalTrackingData Clone(PhysiologicalTrackingData src)
    {
        var dst = new PhysiologicalTrackingData(
            src.timestamp, src.trackingSource, src.globalPosition, src.forwardVector, src.hitObjectName, src.isValid
        );
        dst.leftEye = CloneEye(src.leftEye);
        dst.rightEye = CloneEye(src.rightEye);
        return dst;
    }

    private static EyeData CloneEye(EyeData e)
    {
        return new EyeData
        {
            gazeValid = e.gazeValid,
            gazeLocalPosition = e.gazeLocalPosition,
            gazeWorldPosition = e.gazeWorldPosition,
            gazeLocalRotation = e.gazeLocalRotation,
            gazeWorldRotation = e.gazeWorldRotation,
            pupilDiameterValid = e.pupilDiameterValid,
            pupilPositionValid = e.pupilPositionValid,
            pupilDiameter = e.pupilDiameter,
            pupilPosition = e.pupilPosition,
            geometryValid = e.geometryValid,
            eyeOpenness = e.eyeOpenness,
            eyeSqueeze = e.eyeSqueeze,
            eyeWide = e.eyeWide
        };
    }
}