using System;
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

    [Header("Ray Interactors")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor leftRayInteractor;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rightRayInteractor;

    [Header("Tracking Settings")]
    [SerializeField] private float trackingRate = 10f; // Hz
    [SerializeField] private float maxRayDistance = 20f;
    [SerializeField] private LayerMask raycastLayers = -1;

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

        // Auto-find ray interactors if not assigned
        if (leftRayInteractor == null || rightRayInteractor == null)
        {
            var rayInteractors = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            foreach (var interactor in rayInteractors)
            {
                if (interactor.name.ToLower().Contains("left"))
                    leftRayInteractor = interactor;
                else if (interactor.name.ToLower().Contains("right"))
                    rightRayInteractor = interactor;
            }
        }

        trackingInterval = 1f / trackingRate;
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
        if (isTracking && Time.time - lastTrackTime >= trackingInterval)
        {
            CollectAllTrackingData();
            lastTrackTime = Time.time;
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

        Debug.Log($"[PhysiologicalTracking] Started tracking for image index: {imageIndex}");
    }

    public void StopTracking()
    {
        isTracking = false;

        if (currentImageIndex >= 0 && currentImageData.Count > 0)
            imageTrackingData[currentImageIndex] = new List<PhysiologicalTrackingData>(currentImageData);

        Debug.Log($"[PhysiologicalTracking] Stopped tracking. Total images: {imageTrackingData.Count}");
    }

    void CollectAllTrackingData()
    {
        float currentTime = Time.time;

        // Only collect eye tracking if not using simulator
        if (!useSimulator && eyeTrackingAvailable)
            CollectEyeTrackingData(currentTime);

        // Always collect head and hand tracking
        CollectHeadTracking(currentTime);

        if (leftRayInteractor != null)
            CollectRayInteractorData(leftRayInteractor, "LeftHand", currentTime);

        if (rightRayInteractor != null)
            CollectRayInteractorData(rightRayInteractor, "RightHand", currentTime);
    }

    void CollectEyeTrackingData(float timestamp)
    {
        try
        {
            // Get gaze data
            XrSingleEyeGazeDataHTC[] gazeData;
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);

            var leftGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            var rightGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
            bool validGaze = leftGaze.isValid && rightGaze.isValid;

            // Create tracking data entry
            var trackingData = new PhysiologicalTrackingData(
                timestamp,
                "Eyes",
                Vector3.zero,  // Will be calculated below
                Vector3.zero,  // Will be calculated below
                "None",        // Will be calculated below
                validGaze
            );

            // Populate left eye data
            PopulateEyeGazeData(ref trackingData.leftEye, leftGaze);
            CollectEyePupilData(ref trackingData.leftEye, XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC);
            CollectEyeGeometryData(ref trackingData.leftEye, XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC);

            // Populate right eye data
            PopulateEyeGazeData(ref trackingData.rightEye, rightGaze);
            CollectEyePupilData(ref trackingData.rightEye, XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC);
            CollectEyeGeometryData(ref trackingData.rightEye, XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC);

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
            Debug.LogError($"[PhysiologicalTracking] Eye tracking error: {e.Message}");
        }
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

    void CollectEyePupilData(ref EyeData eyeData, XrEyePositionHTC eyePosition)
    {
        try
        {
            XrSingleEyePupilDataHTC[] pupilData;
            XR_HTC_eye_tracker.Interop.GetEyePupilData(out pupilData);
            var pupil = pupilData[(int)eyePosition];

            eyeData.pupilDiameterValid = pupil.isDiameterValid;
            eyeData.pupilPositionValid = pupil.isPositionValid;

            if (pupil.isDiameterValid)
                eyeData.pupilDiameter = pupil.pupilDiameter;

            if (pupil.isPositionValid)
                eyeData.pupilPosition = new Vector2(pupil.pupilPosition.x, pupil.pupilPosition.y);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhysiologicalTracking] Pupil data collection failed: {e.Message}");
        }
    }

    void CollectEyeGeometryData(ref EyeData eyeData, XrEyePositionHTC eyePosition)
    {
        try
        {
            XrSingleEyeGeometricDataHTC[] geometryData;
            XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out geometryData);
            var geometry = geometryData[(int)eyePosition];

            eyeData.geometryValid = geometry.isValid;
            if (geometry.isValid)
            {
                eyeData.eyeOpenness = geometry.eyeOpenness;
                eyeData.eyeSqueeze = geometry.eyeSqueeze;
                eyeData.eyeWide = geometry.eyeWide;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhysiologicalTracking] Eye geometry data collection failed: {e.Message}");
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

    void CollectRayInteractorData(UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor, string handName, float timestamp)
    {
        if (rayInteractor == null) return;

        Transform rayOrigin = rayInteractor.rayOriginTransform;
        Vector3 rayDirection = rayOrigin.forward;
        string hitObjectName = PerformRaycast(rayOrigin.position, rayDirection);

        currentImageData.Add(new PhysiologicalTrackingData(
            timestamp,
            handName,
            rayOrigin.position,
            rayDirection,
            hitObjectName,
            rayInteractor.enabled
        ));
    }

    string PerformRaycast(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, raycastLayers))
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
}