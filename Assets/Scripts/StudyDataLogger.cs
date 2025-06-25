using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;

public class StudyDataLogger : MonoBehaviour
{
    private List<ImageStudyRecord> studyRecords = new List<ImageStudyRecord>();
    private float currentImageStartTime;

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
            Debug.LogWarning("[StudyDataLogger] No data to save");
            return;
        }

        string fileName = $"StudyData_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("ImageIndex,ImageName,TimeSpent(seconds),SliderValue(%),SelectedOption,TaskType,CorrectAnswer,Timestamp");

        foreach (var record in studyRecords)
        {
            // Format slider value as empty string if it's -1 (for MIN_X questions)
            string sliderValueStr = record.sliderValue == -1 ? "" : $"{record.sliderValue:F0}";

            csv.AppendLine($"{record.imageIndex},{record.imageName},{record.timeSpent:F2},{sliderValueStr},{record.selectedOption},{record.taskType},{record.correctAnswer},{record.timestamp}");
        }

        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"[StudyDataLogger] Study data saved to: {filePath}");
    }

    public void ClearData()
    {
        studyRecords.Clear();
    }
}