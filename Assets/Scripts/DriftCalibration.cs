using UnityEngine;

public static class DriftCalibration
{
    public static Quaternion Offset = Quaternion.identity;
    public static float LastRawDeg = -1f;
    public static float LastResidualDeg = -1f;
    public static string LastLabel = "NA"; // "ok" | "corrected" | "NA"
    public static bool HasOffset { get; private set; }

    public static void Set(Quaternion q, float raw, float residual, string label)
    {
        Offset = q; LastRawDeg = raw; LastResidualDeg = residual; LastLabel = label; HasOffset = true;
    }

    public static void Clear()
    {
        Offset = Quaternion.identity; LastRawDeg = -1f; LastResidualDeg = -1f; LastLabel = "NA"; HasOffset = false;
    }

    public static Vector3 Apply(Vector3 dir) => HasOffset ? (Offset * dir).normalized : dir.normalized;
    public static Quaternion Apply(Quaternion rot) => HasOffset ? (Offset * rot) : rot;
}