using System;
using UnityEngine;

[Serializable]
public class PhysiologicalTrackingData
{
    // Basic tracking data
    public float timestamp;
    public string trackingSource;
    public Vector3 globalPosition;
    public Vector3 forwardVector;
    public string hitObjectName;
    public bool isValid;

    // Eye tracking data for each eye
    public EyeData leftEye;
    public EyeData rightEye;

    public PhysiologicalTrackingData(float time, string source, Vector3 pos, Vector3 forward, string hitObject, bool valid = true)
    {
        timestamp = time;
        trackingSource = source;
        globalPosition = pos;
        forwardVector = forward;
        hitObjectName = hitObject;
        isValid = valid;

        leftEye = new EyeData();
        rightEye = new EyeData();
    }
}

[Serializable]
public class EyeData
{
    // Gaze tracking
    public bool gazeValid;
    public Vector3 gazeLocalPosition;
    public Vector3 gazeWorldPosition;
    public Quaternion gazeLocalRotation;
    public Quaternion gazeWorldRotation;

    // Pupil tracking
    public bool pupilDiameterValid;
    public bool pupilPositionValid;
    public float pupilDiameter;        // in millimeters
    public Vector2 pupilPosition;      // normalized coordinates

    // Eye geometry
    public bool geometryValid;
    public float eyeOpenness;         // 0.0 = closed, 1.0 = fully open
    public float eyeSqueeze;          // amount of squeezing
    public float eyeWide;             // amount of widening

    public EyeData()
    {
        gazeValid = false;
        pupilDiameterValid = false;
        pupilPositionValid = false;
        geometryValid = false;
        pupilDiameter = 0f;
        pupilPosition = Vector2.zero;
        eyeOpenness = 0f;
        eyeSqueeze = 0f;
        eyeWide = 0f;
        gazeLocalPosition = Vector3.zero;
        gazeWorldPosition = Vector3.zero;
        gazeLocalRotation = Quaternion.identity;
        gazeWorldRotation = Quaternion.identity;
    }
}