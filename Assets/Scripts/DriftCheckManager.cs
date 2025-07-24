using UnityEngine;
using UnityEngine.UI;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class DriftCheckManager : MonoBehaviour
{
    [Header("References")]
    public GameObject driftCheckSphere;
    public Button validateButton;

    const float maxAllowedDrift = 1.0f;

    void Start()
    {
        validateButton.onClick.AddListener(ValidateGaze);
        validateButton.interactable = true;
    }

    public void ValidateGaze()
    {
        // Fetch eye‑tracker data
        XR_HTC_eye_tracker.Interop.GetEyeGazeData(out var gazes);
        var leftEye = gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
        if (!leftEye.isValid)
        {
            Debug.LogWarning("Left-eye gaze invalid, cannot compute drift.");
            return;
        }

        // Compute gaze ray in world space
        Vector3 origin = leftEye.gazePose.position.ToUnityVector();
        Vector3 direction = leftEye.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;

        // Compute angular error vs. sphere position
        Vector3 toSphereNorm = (driftCheckSphere.transform.position - origin).normalized;
        float angleDeg = Vector3.Angle(direction, toSphereNorm);

        // Log and provide feedback
        Debug.Log($"Drift check angle: {angleDeg:F2}°");
        if (angleDeg > maxAllowedDrift)
        {
            Debug.LogWarning($"Angle {angleDeg:F2}° > {maxAllowedDrift}° → recalibration needed.");
        }
        else
        {
            Debug.Log("OK!");
        }
    }

}
