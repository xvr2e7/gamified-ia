using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using System.Linq;

public class StudyDataLogger : MonoBehaviour
{
    private List<ImageStudyRecord> studyRecords = new List<ImageStudyRecord>();
    private float currentImageStartTime;

    [Header("Physics Tracking")]
    [SerializeField]
    public PhysiologicalTrackingManager trackingManager;

    void Awake()
    {
        if (trackingManager == null)
        {
            trackingManager = FindObjectOfType<PhysiologicalTrackingManager>();
        }
    }

    public void StartImageTimer()
    {
        currentImageStartTime = Time.time;
    }

    public void RecordImageData(int imageIndex, string imageName, float sliderValue, string selectedOption, string taskType, string correctAnswer)
    {
        float timeSpent = Time.time - currentImageStartTime;

        // Get current streak multiplier
        int streakMultiplier = StreakMultiplier.Instance != null ? StreakMultiplier.Instance.CurrentStreak : 1;

        ImageStudyRecord record = new ImageStudyRecord(imageIndex, imageName, timeSpent, sliderValue, selectedOption, taskType, correctAnswer, streakMultiplier);
        studyRecords.Add(record);
        Debug.Log($"[StudyDataLogger] Recorded: Image {imageName}, Time: {timeSpent:F2}s, Slider: {sliderValue:F0}%, Option: {selectedOption}, Type: {taskType}, Streak: x{streakMultiplier}");
    }

    public void SaveToFile()
    {
        if (studyRecords.Count == 0)
        {
            Debug.LogWarning("[StudyDataLogger] No study data to save");
            return;
        }

        // Get final XP score
        int finalXP = 0;
        if (XPManager.Instance != null)
        {
            finalXP = XPManager.Instance.GetCurrentXP();
        }

        string fileName = $"StudyData_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        StringBuilder csv = new StringBuilder();

        // --- Study Summary ---
        csv.AppendLine("### Study Summary ###");
        csv.AppendLine($"Total Images Viewed,{studyRecords.Count}");
        csv.AppendLine($"Total Time (seconds),{studyRecords.Sum(r => r.timeSpent):F2}");
        csv.AppendLine($"Final XP Score,{finalXP}");
        csv.AppendLine();

        // --- Performance Metrics ---
        csv.AppendLine("### Performance Metrics ###");
        csv.AppendLine("ImageIndex,ImageName,TimeSpent(seconds),SliderValue(%),SelectedOption,TaskType,CorrectAnswer,IsCorrect,StreakMultiplier,Timestamp");

        foreach (var record in studyRecords)
        {
            string sliderValueStr = record.sliderValue == -1 ? "" : $"{record.sliderValue:F0}";

            // Determine if answer was correct based on whether it's a slider or button question
            bool isCorrect = false;

            // Check if it's a slider question (has a valid slider value)
            if (record.sliderValue >= 0 && !string.IsNullOrEmpty(record.correctAnswer))
            {
                float correctValue;
                if (float.TryParse(record.correctAnswer, out correctValue))
                {
                    // Use 5.0f tolerance for slider questions
                    isCorrect = Mathf.Abs(record.sliderValue - correctValue) < 5.0f;
                }
            }
            // Check if it's a button question (has a selected option)
            else if (!string.IsNullOrEmpty(record.selectedOption) && !string.IsNullOrEmpty(record.correctAnswer))
            {
                isCorrect = record.selectedOption == record.correctAnswer;
            }

            csv.AppendLine($"{record.imageIndex},{record.imageName},{record.timeSpent:F2},{sliderValueStr},{record.selectedOption},{record.taskType},{record.correctAnswer},{isCorrect},{record.streakMultiplier},{record.timestamp}");
        }

        // --- Physiological Tracking ---
        if (trackingManager != null)
        {
            var allTracking = trackingManager.GetAllTrackingData();
            if (allTracking.Count > 0)
            {
                csv.AppendLine();
                csv.AppendLine("### Physiological Tracking ###");
                WriteTrackingHeader(csv);
                WriteTrackingData(csv, allTracking);
            }
        }

        // Write out and finish
        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"[StudyDataLogger] Study data saved to: {filePath}");
    }

    void WriteTrackingHeader(StringBuilder csv)
    {
        var headers = new List<string>
        {
            // Basic tracking data
            "ImageIndex", "Timestamp", "TrackingSource", "PosX", "PosY", "PosZ",
            "DirX", "DirY", "DirZ", "HitObject", "IsValid",
            
            // Left eye gaze data
            "LeftGaze_Valid", "LeftGaze_LocalPosX", "LeftGaze_LocalPosY", "LeftGaze_LocalPosZ",
            "LeftGaze_WorldPosX", "LeftGaze_WorldPosY", "LeftGaze_WorldPosZ",
            "LeftGaze_LocalRotX", "LeftGaze_LocalRotY", "LeftGaze_LocalRotZ", "LeftGaze_LocalRotW",
            "LeftGaze_WorldRotX", "LeftGaze_WorldRotY", "LeftGaze_WorldRotZ", "LeftGaze_WorldRotW",
            
            // Right eye gaze data
            "RightGaze_Valid", "RightGaze_LocalPosX", "RightGaze_LocalPosY", "RightGaze_LocalPosZ",
            "RightGaze_WorldPosX", "RightGaze_WorldPosY", "RightGaze_WorldPosZ",
            "RightGaze_LocalRotX", "RightGaze_LocalRotY", "RightGaze_LocalRotZ", "RightGaze_LocalRotW",
            "RightGaze_WorldRotX", "RightGaze_WorldRotY", "RightGaze_WorldRotZ", "RightGaze_WorldRotW",
            
            // Left eye pupil data
            "LeftPupil_DiameterValid", "LeftPupil_Diameter", "LeftPupil_PositionValid",
            "LeftPupil_PosX", "LeftPupil_PosY",
            
            // Right eye pupil data
            "RightPupil_DiameterValid", "RightPupil_Diameter", "RightPupil_PositionValid",
            "RightPupil_PosX", "RightPupil_PosY",
            
            // Left eye geometry data
            "LeftEye_GeometryValid", "LeftEye_Openness", "LeftEye_Squeeze", "LeftEye_Wide",
            
            // Right eye geometry data
            "RightEye_GeometryValid", "RightEye_Openness", "RightEye_Squeeze", "RightEye_Wide"
        };

        csv.AppendLine(string.Join(",", headers));
    }

    void WriteTrackingData(StringBuilder csv, Dictionary<int, List<PhysiologicalTrackingData>> allTracking)
    {
        foreach (var kvp in allTracking)
        {
            int imageIdx = kvp.Key;
            foreach (var data in kvp.Value)
            {
                var values = new List<string>();

                // Basic tracking data
                Vector3 pos = data.globalPosition;
                Vector3 dir = data.forwardVector;
                values.AddRange(new string[]
                {
                    imageIdx.ToString(),
                    data.timestamp.ToString("F3"),
                    data.trackingSource,
                    pos.x.ToString("F4"), pos.y.ToString("F4"), pos.z.ToString("F4"),
                    dir.x.ToString("F4"), dir.y.ToString("F4"), dir.z.ToString("F4"),
                    data.hitObjectName,
                    data.isValid.ToString()
                });

                // Left eye gaze data
                var leftEye = data.leftEye;
                values.AddRange(new string[]
                {
                    leftEye.gazeValid.ToString(),
                    leftEye.gazeLocalPosition.x.ToString("F6"), leftEye.gazeLocalPosition.y.ToString("F6"), leftEye.gazeLocalPosition.z.ToString("F6"),
                    leftEye.gazeWorldPosition.x.ToString("F6"), leftEye.gazeWorldPosition.y.ToString("F6"), leftEye.gazeWorldPosition.z.ToString("F6"),
                    leftEye.gazeLocalRotation.x.ToString("F6"), leftEye.gazeLocalRotation.y.ToString("F6"), leftEye.gazeLocalRotation.z.ToString("F6"), leftEye.gazeLocalRotation.w.ToString("F6"),
                    leftEye.gazeWorldRotation.x.ToString("F6"), leftEye.gazeWorldRotation.y.ToString("F6"), leftEye.gazeWorldRotation.z.ToString("F6"), leftEye.gazeWorldRotation.w.ToString("F6")
                });

                // Right eye gaze data
                var rightEye = data.rightEye;
                values.AddRange(new string[]
                {
                    rightEye.gazeValid.ToString(),
                    rightEye.gazeLocalPosition.x.ToString("F6"), rightEye.gazeLocalPosition.y.ToString("F6"), rightEye.gazeLocalPosition.z.ToString("F6"),
                    rightEye.gazeWorldPosition.x.ToString("F6"), rightEye.gazeWorldPosition.y.ToString("F6"), rightEye.gazeWorldPosition.z.ToString("F6"),
                    rightEye.gazeLocalRotation.x.ToString("F6"), rightEye.gazeLocalRotation.y.ToString("F6"), rightEye.gazeLocalRotation.z.ToString("F6"), rightEye.gazeLocalRotation.w.ToString("F6"),
                    rightEye.gazeWorldRotation.x.ToString("F6"), rightEye.gazeWorldRotation.y.ToString("F6"), rightEye.gazeWorldRotation.z.ToString("F6"), rightEye.gazeWorldRotation.w.ToString("F6")
                });

                // Left eye pupil data
                values.AddRange(new string[]
                {
                    leftEye.pupilDiameterValid.ToString(),
                    leftEye.pupilDiameter.ToString("F4"),
                    leftEye.pupilPositionValid.ToString(),
                    leftEye.pupilPosition.x.ToString("F6"),
                    leftEye.pupilPosition.y.ToString("F6")
                });

                // Right eye pupil data
                values.AddRange(new string[]
                {
                    rightEye.pupilDiameterValid.ToString(),
                    rightEye.pupilDiameter.ToString("F4"),
                    rightEye.pupilPositionValid.ToString(),
                    rightEye.pupilPosition.x.ToString("F6"),
                    rightEye.pupilPosition.y.ToString("F6")
                });

                // Left eye geometry data
                values.AddRange(new string[]
                {
                    leftEye.geometryValid.ToString(),
                    leftEye.eyeOpenness.ToString("F4"),
                    leftEye.eyeSqueeze.ToString("F4"),
                    leftEye.eyeWide.ToString("F4")
                });

                // Right eye geometry data
                values.AddRange(new string[]
                {
                    rightEye.geometryValid.ToString(),
                    rightEye.eyeOpenness.ToString("F4"),
                    rightEye.eyeSqueeze.ToString("F4"),
                    rightEye.eyeWide.ToString("F4")
                });

                csv.AppendLine(string.Join(",", values));
            }
        }
    }

    public void ClearData()
    {
        studyRecords.Clear();
        Debug.Log("[StudyDataLogger] Cleared study records");
    }
}