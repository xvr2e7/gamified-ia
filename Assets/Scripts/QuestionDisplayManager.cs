using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionDisplayManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public RectTransform optionsContainer;
    public GameObject optionButtonPrefab;

    [Header("Data Settings")]
    public string dataFolder = "Data/Questions";

    [Header("Appearance Settings")]
    public Color[] buttonColors;

    private QuestionData currentQuestionData;
    private List<GameObject> activeOptionButtons = new List<GameObject>();

    void Start()
    {
        LoadAndDisplayQuestion("practice_1_data_8_GROUPED_BAR");
    }

    void LoadAndDisplayQuestion(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, dataFolder, fileName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        currentQuestionData = JsonUtility.FromJson<QuestionData>(json);
        if (currentQuestionData == null)
        {
            Debug.LogError("JSON parse failed");
            return;
        }

        if (currentQuestionData.x_labels == null || currentQuestionData.y_values == null ||
            currentQuestionData.x_labels.Length != currentQuestionData.y_values.Length)
        {
            Debug.LogError("x_labels and y_values are missing or mismatched");
            return;
        }

        questionText.text = currentQuestionData.task.question;
        DisplayOptions();
    }

    void DisplayOptions()
    {
        // Clear existing buttons
        foreach (var btn in activeOptionButtons)
            Destroy(btn);
        activeOptionButtons.Clear();

        for (int i = 0; i < currentQuestionData.x_labels.Length; i++)
        {
            CreateOptionButton(currentQuestionData.x_labels[i], currentQuestionData.y_values[i], i);
        }
    }

    void CreateOptionButton(string label, float value, int index)
    {
        GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
        activeOptionButtons.Add(btnObj);

        var layout = btnObj.GetComponent<LayoutElement>() ?? btnObj.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1;
        layout.minHeight = 50;

        // Tint background
        Image img = btnObj.GetComponent<Image>();
        if (img != null && buttonColors != null && buttonColors.Length > 0)
            img.color = buttonColors[index % buttonColors.Length];

        // Set text
        TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
            txt.text = $"{label}: {value}%";

        // Hook up click to log label/value
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null && txt != null)
        {
            string message = txt.text;
            btn.onClick.AddListener(() => Debug.Log(message));
        }
    }
}
