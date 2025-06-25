using UnityEditor;
using UnityEngine;

public static class StudyDataLoggerEditor
{
    [MenuItem("Tools/Open Study Log Folder")]
    public static void OpenLogFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
