using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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

    private List<string> questionFiles = new List<string>();
    private int currentQuestionIndex = 0;

    void Start()
    {
        LoadQuestionFiles();
        if (questionFiles.Count > 0)
        {
            LoadAndDisplayQuestion(questionFiles[0]);
        }
    }

    void LoadQuestionFiles()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, dataFolder);

        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"Questions directory not found: {fullPath}");
            return;
        }

        string[] files = Directory.GetFiles(fullPath, "*.json");
        questionFiles = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
        questionFiles.Sort();

        Debug.Log($"Found {questionFiles.Count} question files");
    }

    public void LoadNextQuestion()
    {
        if (questionFiles.Count == 0) return;

        currentQuestionIndex = (currentQuestionIndex + 1) % questionFiles.Count;
        LoadAndDisplayQuestion(questionFiles[currentQuestionIndex]);
        ResetSlider();
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
            Transform valueTransform = instantiatedSlider.transform.Find("Value");
            if (valueTransform != null)
                valueText = valueTransform.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogWarning("Value text child not found on slider prefab.");

            instantiatedSlider.onValueChanged.AddListener(UpdateValueText);
        }

        // Configure slider
        instantiatedSlider.minValue = 0;
        instantiatedSlider.maxValue = 100;

        instantiatedSlider.value = 50; // start at middle to avoid anchoring

        UpdateValueText(instantiatedSlider.value);
        instantiatedSlider.gameObject.SetActive(true);
    }

    void ResetSlider()
    {
        if (instantiatedSlider != null)
        {
            instantiatedSlider.value = 50; // Reset to middle position
            UpdateValueText(instantiatedSlider.value);
        }
    }

    private void UpdateValueText(float val)
    {
        if (valueText != null)
            valueText.text = $"{val:0}%";
    }

    public float GetCurrentSliderValue()
    {
        if (instantiatedSlider != null)
        {
            return instantiatedSlider.value;
        }
        return -1f; // if no slider exists
    }
}