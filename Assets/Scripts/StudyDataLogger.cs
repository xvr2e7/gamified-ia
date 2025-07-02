using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;

public class StudyDataLogger : MonoBehaviour
{
    private List<ImageStudyRecord> studyRecords = new List<ImageStudyRecord>();
    private float currentImageStartTime;

    [Header("Physics Tracking")]
    [SerializeField]
    private PhysiologicalTrackingManager trackingManager;

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
        ImageStudyRecord record = new ImageStudyRecord(imageIndex, imageName, timeSpent, sliderValue, selectedOption, taskType, correctAnswer);
        studyRecords.Add(record);
        Debug.Log($"[StudyDataLogger] Recorded: Image {imageName}, Time: {timeSpent:F2}s, Slider: {sliderValue:F0}%, Option: {selectedOption}, Type: {taskType}");
    }

    public void SaveToFile()
    {
        if (studyRecords.Count == 0)
        {
            Debug.LogWarning("[StudyDataLogger] No study data to save");
            return;
        }

        string fileName = $"StudyData_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        StringBuilder csv = new StringBuilder();

        // --- Performance Metrics ---
        csv.AppendLine("### Performance Metrics ###");
        csv.AppendLine("ImageIndex,ImageName,TimeSpent(seconds),SliderValue(%),SelectedOption,TaskType,CorrectAnswer,Timestamp");
        foreach (var record in studyRecords)
        {
            string sliderValueStr = record.sliderValue == -1 ? "" : $"{record.sliderValue:F0}";
            csv.AppendLine($"{record.imageIndex},{record.imageName},{record.timeSpent:F2},{sliderValueStr},{record.selectedOption},{record.taskType},{record.correctAnswer},{record.timestamp}");
        }

        // --- Physiological Tracking ---
        if (trackingManager != null)
        {
            var allTracking = trackingManager.GetAllTrackingData();
            if (allTracking.Count > 0)
            {
                csv.AppendLine();
                csv.AppendLine("### Physiological Tracking ###");
                csv.AppendLine("ImageIndex,Timestamp,TrackingSource,PosX,PosY,PosZ,DirX,DirY,DirZ,HitObject,IsValid");

                foreach (var kvp in allTracking)
                {
                    int imageIdx = kvp.Key;
                    foreach (var data in kvp.Value)
                    {
                        Vector3 pos = data.globalPosition;
                        Vector3 dir = data.forwardVector;
                        csv.AppendLine(
                            $"{imageIdx}," +
                            $"{data.timestamp:F3}," +
                            $"{data.trackingSource}," +
                            $"{pos.x:F4},{pos.y:F4},{pos.z:F4}," +
                            $"{dir.x:F4},{dir.y:F4},{dir.z:F4}," +
                            $"{data.hitObjectName}," +
                            $"{data.isValid}"
                        );
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[StudyDataLogger] No PhysiologicalTrackingManager assigned — skipping tracking data");
        }

        // Write out and finish
        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"[StudyDataLogger] Combined data saved to: {filePath}");
    }

    public void ClearData()
    {
        studyRecords.Clear();
        Debug.Log("[StudyDataLogger] Cleared study records");
    }
}
