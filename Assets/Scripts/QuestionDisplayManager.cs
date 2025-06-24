using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionDisplayManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public RectTransform optionsPanel;
    public Slider sliderPrefab;

    [Header("Data Settings")]
    public string dataFolder = "Data/Questions";

    private QuestionData currentQuestionData;
    private Slider instantiatedSlider;
    private TextMeshProUGUI valueText;

    void Start()
    {
        LoadAndDisplayQuestion("practice_1_data_8_GROUPED_BAR");
    }

    void LoadAndDisplayQuestion(string fileName)
    {
        // Load JSON
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

        // Display question text
        questionText.text = currentQuestionData.task.question;

        // Instantiate slider
        if (instantiatedSlider == null)
        {
            instantiatedSlider = Instantiate(sliderPrefab, optionsPanel);
            // Find the Text child named "Value" in the prefab
            Transform valueTransform = instantiatedSlider.transform.Find("Value");
            if (valueTransform != null)
                valueText = valueTransform.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogWarning("Value text child not found on slider prefab.");

            // Add listener to update text in real time
            instantiatedSlider.onValueChanged.AddListener(UpdateValueText);
        }

        // Configure slider
        instantiatedSlider.minValue = 0;
        instantiatedSlider.maxValue = 100;
        if (currentQuestionData.y_values != null && currentQuestionData.y_values.Length > 0)
        {
            instantiatedSlider.value = currentQuestionData.y_values[0];
        }

        // Update the text immediately
        UpdateValueText(instantiatedSlider.value);
        instantiatedSlider.gameObject.SetActive(true);
    }

    private void UpdateValueText(float val)
    {
        if (valueText != null)
            valueText.text = $"{val:0}%";
    }
}