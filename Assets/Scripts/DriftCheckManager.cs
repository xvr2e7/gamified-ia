using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class DriftCheckManager : MonoBehaviour
{
    [SerializeField] private GameObject fixationSphere;
    [SerializeField] private Button validateButton;
    [SerializeField] private float maxAllowedDriftDeg = 1.0f;
    [SerializeField] private int samplesToAverage = 60;
    [SerializeField] private float timeoutSec = 2.0f;
    [SerializeField] private Camera cam;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (validateButton != null) validateButton.onClick.AddListener(Validate);
    }

    void Validate() { StopAllCoroutines(); StartCoroutine(Calibrate()); }

    IEnumerator Calibrate()
    {
        int nCal = samplesToAverage;
        int nVerify = Mathf.Max(15, samplesToAverage / 2);

        // collect calibration batch
        var cal = new List<(Vector3 o, Vector3 d)>(nCal);
        float deadline = Time.unscaledTime + timeoutSec;
        while (cal.Count < nCal && Time.unscaledTime < deadline)
        {
            if (TryGetBinocular(out var o, out var d) && d.sqrMagnitude > 1e-6f)
                cal.Add((o, d.normalized));
            yield return null;
        }
        if (cal.Count == 0 || fixationSphere == null) yield break;

        // avg dir + last origin
        Vector3 avg = Vector3.zero, lastO = Vector3.zero;
        for (int i = 0; i < cal.Count; i++) { avg += cal[i].d; lastO = cal[i].o; }
        avg = (avg.sqrMagnitude > 1e-9f) ? avg.normalized : Vector3.forward;

        Vector3 desired = (fixationSphere.transform.position - lastO).normalized;
        float raw = Vector3.Angle(avg, desired);

        // compute offset from calibration
        var offset = Quaternion.FromToRotation(avg, desired);

        // collect verification batch
        var ver = new List<(Vector3 o, Vector3 d)>(nVerify);
        deadline = Time.unscaledTime + timeoutSec;
        while (ver.Count < nVerify && Time.unscaledTime < deadline)
        {
            if (TryGetBinocular(out var o, out var d) && d.sqrMagnitude > 1e-6f)
                ver.Add((o, d.normalized));
            yield return null;
        }

        float residual = float.NaN;
        if (ver.Count > 0)
        {
            float sum = 0f;
            for (int i = 0; i < ver.Count; i++)
            {
                var o = ver[i].o;
                var d = ver[i].d;
                var desired_i = (fixationSphere.transform.position - o).normalized;
                var corrected = (offset * d).normalized;
                sum += Vector3.Angle(corrected, desired_i);
            }
            residual = sum / ver.Count;
        }

        string label = (raw > maxAllowedDriftDeg) ? "corrected" : "ok";

        DriftCalibration.Set(offset, raw, residual, label);

        Debug.Log($"[Drift] raw={raw:F2}°, residual~{(float.IsNaN(residual) ? -1f : residual):F2}° ({label})");
    }

    static bool TryGetBinocular(out Vector3 origin, out Vector3 dir)
    {
        origin = Vector3.zero; dir = Vector3.forward;
        XR_HTC_eye_tracker.Interop.GetEyeGazeData(out var g);
        if (g == null || g.Length < 2) return false;

        var l = g[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
        var r = g[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

        int c = 0; Vector3 o = Vector3.zero; Vector3 d = Vector3.zero;
        if (l.isValid) { o += l.gazePose.position.ToUnityVector(); d += l.gazePose.orientation.ToUnityQuaternion() * Vector3.forward; c++; }
        if (r.isValid) { o += r.gazePose.position.ToUnityVector(); d += r.gazePose.orientation.ToUnityQuaternion() * Vector3.forward; c++; }
        if (c == 0) return false;

        origin = o / c;
        dir = (d / c).normalized;
        return true;
    }
}