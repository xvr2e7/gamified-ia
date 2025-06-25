using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;

[Serializable]
public class ImageStudyRecord
{
    public int imageIndex;
    public string imageName;
    public float timeSpent;
    public float sliderValue;
    public string timestamp;

    public ImageStudyRecord(int index, string name, float time, float value)
    {
        imageIndex = index;
        imageName = name;
        timeSpent = time;
        sliderValue = value;
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}

public class StudyDataLogger : MonoBehaviour
{
    private List<ImageStudyRecord> studyRecords = new List<ImageStudyRecord>();
    private float currentImageStartTime;

    public void StartImageTimer()
    {
        currentImageStartTime = Time.time;
    }

    public void RecordImageData(int imageIndex, string imageName, float sliderValue)
    {
        float timeSpent = Time.time - currentImageStartTime;
        ImageStudyRecord record = new ImageStudyRecord(imageIndex, imageName, timeSpent, sliderValue);
        studyRecords.Add(record);
        Debug.Log($"[StudyDataLogger] Recorded: Image {imageName}, Time: {timeSpent:F2}s, Slider: {sliderValue:F0}%");
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
        csv.AppendLine("ImageIndex,ImageName,TimeSpent(seconds),SliderValue(%),Timestamp");

        foreach (var record in studyRecords)
        {
            csv.AppendLine($"{record.imageIndex},{record.imageName},{record.timeSpent:F2},{record.sliderValue:F0},{record.timestamp}");
        }

        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"[StudyDataLogger] Study data saved to: {filePath}");
    }
    public void ClearData()
    {
        studyRecords.Clear();
    }
}

