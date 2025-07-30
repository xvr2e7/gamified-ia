using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class PhysiologicalTrackingManager : MonoBehaviour
{
    [Header("Tracking References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Camera mainCamera;

    [Header("Tracking Settings")]
    [SerializeField] private float trackingRate = 90f; // Hz
    [SerializeField] private bool useCombinedTracking = true; // Combine head and eye data

    [Header("Performance Settings")]
    [SerializeField] private bool useFixedUpdate = true;
    [SerializeField] private bool enableDiagnostics = false;

    [Header("Simulator Settings")]
    [SerializeField] private bool forceSimulatorMode = false;

    // Tracking data storage
    private Dictionary<int, List<PhysiologicalTrackingData>> imageTrackingData = new Dictionary<int, List<PhysiologicalTrackingData>>();
    private List<PhysiologicalTrackingData> currentImageData = new List<PhysiologicalTrackingData>();
    private int currentImageIndex = -1;

    // Tracking state
    private float trackingInterval;
    private float lastTrackTime;
    private bool isTracking = false;
    private bool useSimulator = false;
    private bool eyeTrackingAvailable = false;

    // Performance monitoring
    private float actualSampleRate = 0f;
    private int sampleCount = 0;
    private float sampleRateTimer = 0f;
    private Coroutine trackingCoroutine;

    void Start()
    {
        // Auto-find references if not assigned
        if (xrOrigin == null)
        {
            var xrOriginGO = GameObject.Find("XR Origin");
            if (xrOriginGO != null)
                xrOrigin = xrOriginGO.transform;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        trackingInterval = 1f / trackingRate;

        // Set fixed timestep
        if (useFixedUpdate)
        {
            Time.fixedDeltaTime = trackingInterval;
        }

        CheckTrackingEnvironment();
    }

    void CheckTrackingEnvironment()
    {
#if UNITY_EDITOR
        var deviceSimulator = FindObjectOfType<XRDeviceSimulator>();
        useSimulator = forceSimulatorMode || (deviceSimulator != null);
#else
        useSimulator = false;
#endif

        if (useSimulator)
        {
            Debug.Log("[PhysiologicalTracking] Using simulator mode - eye tracking disabled");
            eyeTrackingAvailable = false;
            return;
        }

        // Test eye tracking availability
        try
        {
            XrSingleEyeGazeDataHTC[] gazeData;
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);
            eyeTrackingAvailable = gazeData != null && gazeData.Length >= 2;

            if (eyeTrackingAvailable)
                Debug.Log("[PhysiologicalTracking] HTC Vive eye tracking available");
            else
                Debug.LogWarning("[PhysiologicalTracking] HTC Vive eye tracking not available");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhysiologicalTracking] Eye tracking initialization failed: {e.Message}");
            eyeTrackingAvailable = false;
        }
    }

    void Update()
    {
        // Only use Update if not using FixedUpdate
        if (!useFixedUpdate && isTracking && Time.time - lastTrackTime >= trackingInterval)
        {
            CollectAllTrackingData();
            lastTrackTime = Time.time;
        }

        // Performance monitoring
        if (enableDiagnostics && isTracking)
        {
            sampleRateTimer += Time.deltaTime;
            if (sampleRateTimer >= 1f)
            {
                actualSampleRate = sampleCount;
                if (actualSampleRate < trackingRate * 0.9f) // Alert if below 90% of target
                {
                    Debug.LogWarning($"[PhysiologicalTracking] Low sample rate: {actualSampleRate:F1} Hz (target: {trackingRate} Hz)");
                }
                sampleCount = 0;
                sampleRateTimer = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        if (useFixedUpdate && isTracking)
        {
            CollectAllTrackingData();
        }
    }

    public void StartTrackingForImage(int imageIndex)
    {
        if (currentImageIndex >= 0 && currentImageData.Count > 0)
            imageTrackingData[currentImageIndex] = new List<PhysiologicalTrackingData>(currentImageData);

        currentImageIndex = imageIndex;
        currentImageData.Clear();
        isTracking = true;
        lastTrackTime = Time.time;
        sampleCount = 0;
        sampleRateTimer = 0f;

        // Start coroutine as alternative timing method
        if (!useFixedUpdate && trackingCoroutine == null)
        {
            trackingCoroutine = StartCoroutine(TrackingCoroutine());
        }

        Debug.Log($"[PhysiologicalTracking] Started tracking for image index: {imageIndex}");
    }

    public void StopTracking()
    {
        isTracking = false;

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
            trackingCoroutine = null;
        }

        if (currentImageIndex >= 0 && currentImageData.Count > 0)
            imageTrackingData[currentImageIndex] = new List<PhysiologicalTrackingData>(currentImageData);


        if (enableDiagnostics)
        {
            Debug.Log($"[PhysiologicalTracking] Final sample rate: {actualSampleRate:F1} Hz");
        }
    }

    IEnumerator TrackingCoroutine()
    {
        while (isTracking)
        {
            CollectAllTrackingData();
            yield return new WaitForSeconds(trackingInterval);
        }
    }

    void CollectAllTrackingData()
    {
        float currentTime = Time.time;
        sampleCount++;

        if (useCombinedTracking)
        {
            // Collect head and eye data in a single entry
            CollectCombinedData(currentTime);
        }
        else
        {
            // Fallback: separate collection
            if (!useSimulator && eyeTrackingAvailable)
                CollectEyeTrackingData(currentTime);
            CollectHeadTracking(currentTime);
        }
    }

    void CollectCombinedData(float timestamp)
    {
        // Create a single tracking entry with both head and eye data
        var trackingData = new PhysiologicalTrackingData(
            timestamp,
            "Combined",
            mainCamera.transform.position,
            mainCamera.transform.forward,
            PerformRaycast(mainCamera.transform.position, mainCamera.transform.forward),
            true
        );

        // Add eye tracking data if available
        if (!useSimulator && eyeTrackingAvailable)
        {
            try
            {
                // Batch all eye tracking API calls together
                XrSingleEyeGazeDataHTC[] gazeData;
                XrSingleEyePupilDataHTC[] pupilData;
                XrSingleEyeGeometricDataHTC[] geometryData;

                XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);
                XR_HTC_eye_tracker.Interop.GetEyePupilData(out pupilData);
                XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out geometryData);

                // Process left eye
                if (gazeData != null && gazeData.Length > (int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC)
                {
                    var leftGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                    PopulateEyeGazeData(ref trackingData.leftEye, leftGaze);
                }

                if (pupilData != null && pupilData.Length > (int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC)
                {
                    var leftPupil = pupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                    PopulateEyePupilData(ref trackingData.leftEye, leftPupil);
                }

                if (geometryData != null && geometryData.Length > (int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC)
                {
                    var leftGeometry = geometryData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
                    PopulateEyeGeometryData(ref trackingData.leftEye, leftGeometry);
                }

                // Process right eye
                if (gazeData != null && gazeData.Length > (int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC)
                {
                    var rightGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
                    PopulateEyeGazeData(ref trackingData.rightEye, rightGaze);
                }

                if (pupilData != null && pupilData.Length > (int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC)
                {
                    var rightPupil = pupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
                    PopulateEyePupilData(ref trackingData.rightEye, rightPupil);
                }

                if (geometryData != null && geometryData.Length > (int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC)
                {
                    var rightGeometry = geometryData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
                    PopulateEyeGeometryData(ref trackingData.rightEye, rightGeometry);
                }
            }
            catch (Exception e)
            {
                if (enableDiagnostics)
                    Debug.LogError($"[PhysiologicalTracking] Eye tracking error: {e.Message}");
            }
        }

        currentImageData.Add(trackingData);
    }

    void CollectEyeTrackingData(float timestamp)
    {
        try
        {
            // Get all eye data in one batch
            XrSingleEyeGazeDataHTC[] gazeData;
            XrSingleEyePupilDataHTC[] pupilData;
            XrSingleEyeGeometricDataHTC[] geometryData;

            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);
            XR_HTC_eye_tracker.Interop.GetEyePupilData(out pupilData);
            XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out geometryData);

            var leftGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            var rightGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
            bool validGaze = leftGaze.isValid && rightGaze.isValid;

            // Create tracking data entry
            var trackingData = new PhysiologicalTrackingData(
                timestamp,
                "Eyes",
                Vector3.zero,
                Vector3.zero,
                "None",
                validGaze
            );

            // Populate eye data using batched results
            PopulateEyeGazeData(ref trackingData.leftEye, leftGaze);
            PopulateEyePupilData(ref trackingData.leftEye, pupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC]);
            PopulateEyeGeometryData(ref trackingData.leftEye, geometryData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC]);

            PopulateEyeGazeData(ref trackingData.rightEye, rightGaze);
            PopulateEyePupilData(ref trackingData.rightEye, pupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC]);
            PopulateEyeGeometryData(ref trackingData.rightEye, geometryData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC]);

            // Calculate combined gaze for raycast
            if (validGaze)
            {
                Vector3 leftLocal = leftGaze.gazePose.position.ToUnityVector();
                Vector3 rightLocal = rightGaze.gazePose.position.ToUnityVector();
                Vector3 avgLocalOrigin = (leftLocal + rightLocal) / 2f;
                Vector3 gazeOrigin = mainCamera.transform.TransformPoint(avgLocalOrigin);

                Quaternion leftQuat = mainCamera.transform.rotation * leftGaze.gazePose.orientation.ToUnityQuaternion();
                Quaternion rightQuat = mainCamera.transform.rotation * rightGaze.gazePose.orientation.ToUnityQuaternion();
                Quaternion gazeRotation = Quaternion.Slerp(leftQuat, rightQuat, 0.5f);
                Vector3 gazeDirection = gazeRotation * Vector3.forward;

                trackingData.globalPosition = gazeOrigin;
                trackingData.forwardVector = gazeDirection;
                trackingData.hitObjectName = PerformRaycast(gazeOrigin, gazeDirection);
            }

            currentImageData.Add(trackingData);
        }
        catch (Exception e)
        {
            if (enableDiagnostics)
                Debug.LogError($"[PhysiologicalTracking] Eye tracking error: {e.Message}");
        }
    }

    void CollectHeadTracking(float timestamp)
    {
        if (mainCamera == null) return;

        string hitObjectName = PerformRaycast(mainCamera.transform.position, mainCamera.transform.forward);

        currentImageData.Add(new PhysiologicalTrackingData(
            timestamp,
            "Head",
            mainCamera.transform.position,
            mainCamera.transform.forward,
            hitObjectName,
            true
        ));
    }

    void PopulateEyeGazeData(ref EyeData eyeData, XrSingleEyeGazeDataHTC gazeData)
    {
        eyeData.gazeValid = gazeData.isValid;
        if (gazeData.isValid)
        {
            eyeData.gazeLocalPosition = gazeData.gazePose.position.ToUnityVector();
            eyeData.gazeLocalRotation = gazeData.gazePose.orientation.ToUnityQuaternion();
            eyeData.gazeWorldPosition = mainCamera.transform.TransformPoint(eyeData.gazeLocalPosition);
            eyeData.gazeWorldRotation = mainCamera.transform.rotation * eyeData.gazeLocalRotation;
        }
    }

    void PopulateEyePupilData(ref EyeData eyeData, XrSingleEyePupilDataHTC pupilData)
    {
        eyeData.pupilDiameterValid = pupilData.isDiameterValid;
        eyeData.pupilPositionValid = pupilData.isPositionValid;

        if (pupilData.isDiameterValid)
            eyeData.pupilDiameter = pupilData.pupilDiameter;

        if (pupilData.isPositionValid)
            eyeData.pupilPosition = new Vector2(pupilData.pupilPosition.x, pupilData.pupilPosition.y);
    }

    void PopulateEyeGeometryData(ref EyeData eyeData, XrSingleEyeGeometricDataHTC geometryData)
    {
        eyeData.geometryValid = geometryData.isValid;
        if (geometryData.isValid)
        {
            eyeData.eyeOpenness = geometryData.eyeOpenness;
            eyeData.eyeSqueeze = geometryData.eyeSqueeze;
            eyeData.eyeWide = geometryData.eyeWide;
        }
    }

    string PerformRaycast(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, 20f))
        {
            return hit.collider.gameObject.name;
        }
        return "None";
    }

    // Public interface methods
    public Dictionary<int, List<PhysiologicalTrackingData>> GetAllTrackingData()
    {
        if (currentImageIndex >= 0 && currentImageData.Count > 0)
            imageTrackingData[currentImageIndex] = new List<PhysiologicalTrackingData>(currentImageData);

        return imageTrackingData;
    }

    public List<PhysiologicalTrackingData> GetTrackingDataForImage(int imageIndex)
    {
        if (imageTrackingData.ContainsKey(imageIndex))
            return imageTrackingData[imageIndex];
        return new List<PhysiologicalTrackingData>();
    }

    public void ClearAllData()
    {
        imageTrackingData.Clear();
        currentImageData.Clear();
        currentImageIndex = -1;
        Debug.Log("[PhysiologicalTracking] All tracking data cleared");
    }

    public float GetActualSampleRate()
    {
        return actualSampleRate;
    }
}