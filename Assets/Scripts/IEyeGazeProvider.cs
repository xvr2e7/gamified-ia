using UnityEngine;

public interface IEyeGazeProvider
{
    bool TryGetGazePose(out Pose gazePose);
}