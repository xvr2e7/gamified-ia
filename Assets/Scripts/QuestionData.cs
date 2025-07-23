using System;

[Serializable]
public class QuestionData
{
    public string file;
    public string encoding;
    public string feature;
    public string[] categories;
    public TaskData[] tasks;
}

[Serializable]
public class TaskData
{
    public string type;
    public string family;
    public string question;
    public string answer;
    public string panel;

    public string GetStringAnswer()
    {
        return answer ?? "";
    }
}