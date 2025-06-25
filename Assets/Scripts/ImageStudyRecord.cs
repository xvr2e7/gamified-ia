using System;

[Serializable]
public class ImageStudyRecord
{
    public int imageIndex;
    public string imageName;
    public float timeSpent;
    public float sliderValue;
    public string selectedOption;
    public string taskType;
    public string correctAnswer;
    public string timestamp;

    public ImageStudyRecord(int index, string name, float time, float value, string option, string type, string answer)
    {
        imageIndex = index;
        imageName = name;
        timeSpent = time;
        sliderValue = value;
        selectedOption = option;
        taskType = type;
        correctAnswer = answer;
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}
