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
    private float studyStartTime;

    [Header("Physics Tracking")]
    [SerializeField]
    public PhysiologicalTrackingManager trackingManager;

    void Awake()
    {
        if (trackingManager == null)
        {
            trackingManager = FindObjectOfType<PhysiologicalTrackingManager>();
        }
        studyStartTime = Time.time;
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

        // Get block number and condition
        int blockNumber = GetBlockNumber();
        string conditionName = GetConditionName();

        // Create base filename with block number
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string baseFileName = $"Block{blockNumber}_{timestamp}";
        string basePath = Application.persistentDataPath;

        // Save three separate files
        SaveSummaryFile(basePath, baseFileName, conditionName);
        SavePerformanceFile(basePath, baseFileName);
        SavePhysiologicalFile(basePath, baseFileName);

        Debug.Log($"[StudyDataLogger] Study data saved with base name: {baseFileName}");
    }

    private void SaveSummaryFile(string basePath, string baseFileName, string conditionName)
    {
        string fileName = $"{baseFileName}_Summary.csv";
        string filePath = Path.Combine(basePath, fileName);

        // Get final XP score
        int finalXP = 0;
        if (XPManager.Instance != null)
        {
            finalXP = XPManager.Instance.GetCurrentXP();
        }

        // Calculate total study time
        float totalStudyTime = Time.time - studyStartTime;

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Condition,{conditionName}");
        csv.AppendLine($"Block Number,{GetBlockNumber()}");
        csv.AppendLine($"Participant ID,{PlayerPrefs.GetInt("ParticipantID", 1)}");
        csv.AppendLine($"Total Images Viewed,{studyRecords.Count}");
        csv.AppendLine($"Total Time (seconds),{studyRecords.Sum(r => r.timeSpent):F2}");
        csv.AppendLine($"Total Study Time (seconds),{totalStudyTime:F2}");
        csv.AppendLine($"Average Time Per Image (seconds),{(studyRecords.Count > 0 ? studyRecords.Average(r => r.timeSpent) : 0):F2}");
        csv.AppendLine($"Final XP Score,{finalXP}");
        csv.AppendLine($"Timestamp,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"[StudyDataLogger] Summary saved to: {filePath}");
    }

    private void SavePerformanceFile(string basePath, string baseFileName)
    {
        string fileName = $"{baseFileName}_Performance.csv";
        string filePath = Path.Combine(basePath, fileName);

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("ImageIndex,ImageName,TimeSpent(seconds),SliderValue(%),SelectedOption,TaskType,CorrectAnswer,IsCorrect,StreakMultiplier,Timestamp");

        foreach (var record in studyRecords)
        {
            string sliderValueStr = record.sliderValue == -1 ? "" : $"{record.sliderValue:F0}";

            // Determine if answer was correct
            bool isCorrect = false;

            // Check if it's a slider question
            if (record.sliderValue >= 0 && !string.IsNullOrEmpty(record.correctAnswer))
            {
                float correctValue;
                if (float.TryParse(record.correctAnswer, out correctValue))
                {
                    isCorrect = Mathf.Abs(record.sliderValue - correctValue) < 5.0f;
                }
            }
            // Check if it's a button question
            else if (!string.IsNullOrEmpty(record.selectedOption) && !string.IsNullOrEmpty(record.correctAnswer))
            {
                isCorrect = record.selectedOption == record.correctAnswer;
            }

            csv.AppendLine($"{record.imageIndex},{record.imageName},{record.timeSpent:F2},{sliderValueStr},{record.selectedOption},{record.taskType},{record.correctAnswer},{isCorrect},{record.streakMultiplier},{record.timestamp}");
        }

        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"[StudyDataLogger] Performance data saved to: {filePath}");
    }

    private void SavePhysiologicalFile(string basePath, string baseFileName)
    {
        if (trackingManager == null)
        {
            return;
        }

        var allTracking = trackingManager.GetAllTrackingData();
        if (allTracking.Count == 0)
        {
            Debug.LogWarning("[StudyDataLogger] No physiological tracking data to save");
            return;
        }

        string fileName = $"{baseFileName}_Physiological.csv";
        string filePath = Path.Combine(basePath, fileName);

        StringBuilder csv = new StringBuilder();
        WriteTrackingHeader(csv);
        WriteTrackingData(csv, allTracking);

        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"[StudyDataLogger] Physiological data saved to: {filePath}");
    }

    private int GetBlockNumber()
    {
        // Check if in practice mode
        if (ExperimentManager.Instance != null && ExperimentManager.Instance.IsPracticeMode())
        {
            return 0; // Practice block is 0
        }

        // Get current condition index (0-based) and add 1 for block number
        int conditionIndex = PlayerPrefs.GetInt("CurrentCondition", 0);
        return conditionIndex + 1;
    }

    private string GetConditionName()
    {
        if (ExperimentManager.Instance != null)
        {
            var condition = ExperimentManager.Instance.GetCurrentCondition();
            return condition.ToString();
        }

        // Fallback if ExperimentManager not available
        return "UNKNOWN";
    }

    void WriteTrackingHeader(StringBuilder csv)
    {
        var headers = new List<string>
        {
            // Basic tracking data
            "ImageIndex", "Timestamp", "PosX", "PosY", "PosZ",
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

        headers.AddRange(new[]
        {
            "Drift_RawDeg", "Drift_ResidualDeg",
            "Drift_OffsetX", "Drift_OffsetY", "Drift_OffsetZ", "Drift_OffsetW",
            "Drift_Label"
        });

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

                // Drift info
                var off = DriftCalibration.Offset;
                values.AddRange(new string[]
                {
                    DriftCalibration.LastRawDeg.ToString("F2"),
                    DriftCalibration.LastResidualDeg.ToString("F2"),
                    off.x.ToString("F6"), off.y.ToString("F6"), off.z.ToString("F6"), off.w.ToString("F6"),
                    DriftCalibration.LastLabel
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