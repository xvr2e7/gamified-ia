using System;

[Serializable]
public class QuestionData
{
    public string[] x_labels;
    public float[] y_values;
    public TaskData task;
}

[Serializable]
public class TaskData
{
    public string task_type;
    public string question;
    public string answer;
    public string[] options; // For MIN_X questions
}