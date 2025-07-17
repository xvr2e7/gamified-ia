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

    [Header("Panels")]
    public GameObject sliderPanel;
    public GameObject buttonPanel;

    [Header("Prefabs")]
    public Slider sliderPrefab;
    public Button buttonPrefab;

    [Header("Data Settings")]
    public string dataFolder = "Data/Questions";

    private QuestionData currentQuestionData;
    private List<string> questionFiles = new List<string>();
    private int currentQuestionIndex = 0;

    private Slider instantiatedSlider;
    private TextMeshProUGUI valueText;

    void Awake()
    {
        // Hide panels before anything renders
        sliderPanel.SetActive(false);
        buttonPanel.SetActive(false);
    }

    public void InitializeQuestions()
    {
        LoadQuestionFiles();
        if (questionFiles.Count > 0)
            LoadAndDisplayQuestion(questionFiles[0]);

        if (QuestionTimer.Instance != null)
            QuestionTimer.Instance.StartTimer();
    }

    void LoadQuestionFiles()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, dataFolder);
        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"Questions directory not found: {fullPath}");
            return;
        }

        questionFiles = Directory
            .GetFiles(fullPath, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(name => name)
            .ToList();
    }

    public void LoadNextQuestion()
    {
        if (questionFiles.Count == 0) return;
        currentQuestionIndex = (currentQuestionIndex + 1) % questionFiles.Count;
        LoadAndDisplayQuestion(questionFiles[currentQuestionIndex]);

        if (QuestionTimer.Instance != null)
            QuestionTimer.Instance.StartTimer();
    }

    void LoadAndDisplayQuestion(string fileName)
    {
        // Parse JSON
        string path = Path.Combine(Application.streamingAssetsPath, dataFolder, fileName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found: {path}");
            return;
        }
        currentQuestionData = JsonUtility.FromJson<QuestionData>(File.ReadAllText(path));
        if (currentQuestionData == null)
        {
            Debug.LogError("JSON parse failed");
            return;
        }

        // Update question text
        questionText.text = currentQuestionData.task.question;

        // Clear previous UI
        ClearInstantiatedElements();

        // Show the right panel
        if (currentQuestionData.task.task_type == "VALUE_PART")
            SetupSlider();
        else if (currentQuestionData.task.task_type == "MIN_X")
            SetupCarousel();
        else
            Debug.LogError($"Unknown task type: {currentQuestionData.task.task_type}");
    }

    void SetupSlider()
    {
        // Activate slider panel; deactivate carousel
        sliderPanel.SetActive(true);
        buttonPanel.SetActive(false);

        // Instantiate and parent the slider
        instantiatedSlider = Instantiate(sliderPrefab, sliderPanel.transform);

        // Hook up the Value text
        var valueTransform = instantiatedSlider.transform.Find("Value");
        if (valueTransform != null)
            valueText = valueTransform.GetComponent<TextMeshProUGUI>();
        else
            Debug.LogWarning("Slider prefab missing child named 'Value'");


        // Start in midpoint
        float mid = (instantiatedSlider.minValue + instantiatedSlider.maxValue) * 0.5f;
        instantiatedSlider.value = mid;
        UpdateValueText(mid);

        // Listen for changes
        instantiatedSlider.onValueChanged.AddListener(UpdateValueText);
    }

    void SetupCarousel()
    {
        // Activate carousel; deactivate slider
        buttonPanel.SetActive(true);
        sliderPanel.SetActive(false);

        // Determine options array
        string[] opts = currentQuestionData.task.options;
        if ((opts == null || opts.Length == 0) && currentQuestionData.x_labels != null)
            opts = currentQuestionData.x_labels;

        if (opts == null || opts.Length == 0)
        {
            Debug.LogError("No options available for carousel");
            return;
        }

        // Initialize the existing GridControlledButtons on buttonPanel
        var carousel = buttonPanel.GetComponent<GridControlledButtons>();
        if (carousel == null)
        {
            Debug.LogError("GridControlledButtons component missing on buttonPanel");
            return;
        }
        carousel.Initialize(opts, buttonPrefab);
    }

    void ClearInstantiatedElements()
    {
        // Destroy old slider
        if (instantiatedSlider != null)
        {
            Destroy(instantiatedSlider.gameObject);
            instantiatedSlider = null;
            valueText = null;
        }

        // Clear carousel buttons
        var carousel = buttonPanel.GetComponent<GridControlledButtons>();
        if (carousel != null)
            carousel.Clear();
    }


    void UpdateValueText(float val)
    {
        if (valueText != null)
            valueText.text = $"{val:0}";
    }

    // --- Public getters ---

    public float GetCurrentSliderValue()
        => instantiatedSlider != null ? instantiatedSlider.value : -1f;

    public int GetSelectedOptionIndex()
    {
        var c = buttonPanel.GetComponent<GridControlledButtons>();
        return c != null ? c.CurrentIndex : -1;
    }

    public string GetSelectedOptionText()
    {
        var c = buttonPanel.GetComponent<GridControlledButtons>();
        return c != null ? c.CurrentText : "";
    }

    public string GetCurrentTaskType()
        => currentQuestionData?.task?.task_type ?? "";

    public string GetCorrectAnswer()
        => currentQuestionData?.task?.answer ?? "";
}
