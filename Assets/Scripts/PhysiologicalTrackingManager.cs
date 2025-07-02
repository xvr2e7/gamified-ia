using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

[Serializable]
public class PhysiologicalTrackingData
{
    public float timestamp;
    public string trackingSource;
    public Vector3 globalPosition;
    public Vector3 forwardVector;
    public string hitObjectName;
    public bool isValid;

    public PhysiologicalTrackingData(float time, string source, Vector3 pos, Vector3 forward, string hitObject, bool valid = true)
    {
        timestamp = time;
        trackingSource = source;
        globalPosition = pos;
        forwardVector = forward;
        hitObjectName = hitObject;
        isValid = valid;
    }
}

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

    // Tracking data
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
        // Auto-find references
        if (xrOrigin == null)
        {
            var xrOriginGO = GameObject.Find("XR Origin");
            if (xrOriginGO != null)
                xrOrigin = xrOriginGO.transform;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Auto-find ray interactors
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

        if (!useSimulator)
        {
            try
            {
                // Attempt to get eye gaze data to verify availability
                XrSingleEyeGazeDataHTC[] gazeData;
                XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);
                eyeTrackingAvailable = gazeData != null && gazeData.Length >= 2;

                if (eyeTrackingAvailable)
                    Debug.Log("[PhysiologicalTracking] VIVE Eye tracking available");
                else
                    Debug.LogWarning("[PhysiologicalTracking] Eye tracking data invalid or incomplete");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysiologicalTracking] Eye tracking not available: {e.Message}");
                eyeTrackingAvailable = false;
            }
        }
        else
        {
            Debug.Log("[PhysiologicalTracking] Using simulator mode");
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

        if (useSimulator)
            CollectSimulatedEyeTracking(currentTime);
        else if (eyeTrackingAvailable)
            CollectViveEyeTracking(currentTime);

        CollectHeadTracking(currentTime);

        if (leftRayInteractor != null)
            CollectRayInteractorData(leftRayInteractor, "LeftHand", currentTime);

        if (rightRayInteractor != null)
            CollectRayInteractorData(rightRayInteractor, "RightHand", currentTime);
    }

    void CollectSimulatedEyeTracking(float timestamp)
    {
        if (Mouse.current == null) return;

        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Ray gazeRay = mainCamera.ScreenPointToRay(mousePosition);

        string hitObjectName = PerformRaycast(gazeRay.origin, gazeRay.direction);

        currentImageData.Add(new PhysiologicalTrackingData(
            timestamp,
            "Eyes_Simulated",
            gazeRay.origin,
            gazeRay.direction,
            hitObjectName,
            true
        ));
    }

    void CollectViveEyeTracking(float timestamp)
    {
        try
        {
            XrSingleEyeGazeDataHTC[] gazeData;
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);

            var leftGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            var rightGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
            bool validGaze = leftGaze.isValid && rightGaze.isValid;

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

                string hitObjectName = PerformRaycast(gazeOrigin, gazeDirection);

                currentImageData.Add(new PhysiologicalTrackingData(
                    timestamp,
                    "Eyes",
                    gazeOrigin,
                    gazeDirection,
                    hitObjectName,
                    true
                ));
            }
            else
            {
                currentImageData.Add(new PhysiologicalTrackingData(
                    timestamp,
                    "Eyes",
                    Vector3.zero,
                    Vector3.zero,
                    "Invalid",
                    false
                ));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhysiologicalTracking] Eye tracking error: {e.Message}");
        }
    }

    void CollectHeadTracking(float timestamp)
    {
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

    void CollectRayInteractorData(UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor, string sourceName, float timestamp)
    {
        string hitObjectName = "None";

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            hitObjectName = hit.collider.gameObject.name;

        currentImageData.Add(new PhysiologicalTrackingData(
            timestamp,
            sourceName,
            rayInteractor.transform.position,
            rayInteractor.transform.forward,
            hitObjectName,
            true
        ));
    }

    string PerformRaycast(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, raycastLayers))
            return hit.collider.gameObject.name;
        return "None";
    }

    public Dictionary<int, List<PhysiologicalTrackingData>> GetAllTrackingData()
    {
        if (currentImageIndex >= 0 && currentImageData.Count > 0)
            imageTrackingData[currentImageIndex] = new List<PhysiologicalTrackingData>(currentImageData);
        return imageTrackingData;
    }

    public void ClearAllData()
    {
        imageTrackingData.Clear();
        currentImageData.Clear();
        currentImageIndex = -1;
    }
}
