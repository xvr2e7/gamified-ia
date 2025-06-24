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
    public string question;
    public string answer;
}
