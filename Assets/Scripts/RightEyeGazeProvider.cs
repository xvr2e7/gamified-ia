using UnityEngine;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class RightEyeGazeProvider : MonoBehaviour, IEyeGazeProvider
{
    [SerializeField] private Camera mainCamera;

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    public bool TryGetGazePose(out Pose gazePose)
    {
        gazePose = default;
        XrSingleEyeGazeDataHTC[] gazeData;
        XR_HTC_eye_tracker.Interop.GetEyeGazeData(out gazeData);
        var right = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];
        if (!right.isValid) return false;

        Vector3 worldPos = mainCamera.transform.TransformPoint(
            right.gazePose.position.ToUnityVector());
        Quaternion worldRot = mainCamera.transform.rotation *
                              right.gazePose.orientation.ToUnityQuaternion();

        gazePose = new Pose(worldPos, worldRot);
        return true;
    }
}
