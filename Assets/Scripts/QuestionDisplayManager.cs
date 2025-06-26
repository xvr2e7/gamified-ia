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
    public Button buttonPrefab;

    [Header("Layout Settings")]
    public int buttonsPerRow = 2;
    public float buttonSpacing = 10f;
    public Vector2 buttonSize = new Vector2(150, 50);

    [Header("Data Settings")]
    public string dataFolder = "Data/Questions";

    private QuestionData currentQuestionData;
    private Slider instantiatedSlider;
    private TextMeshProUGUI valueText;
    private List<Button> instantiatedButtons = new List<Button>();
    private int selectedOptionIndex = -1;

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
        ResetInput();
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

        // Clear previous UI elements
        ClearInstantiatedElements();

        // Set up appropriate input based on question type
        if (currentQuestionData.task.task_type == "VALUE_PART")
        {
            SetupSlider();
        }
        else if (currentQuestionData.task.task_type == "MIN_X")
        {
            SetupButtons();
        }
        else
        {
            Debug.LogWarning($"Unknown task type: {currentQuestionData.task.task_type}");
            // Default to slider if unknown
            SetupSlider();
        }
    }

    void SetupSlider()
    {
        // Instantiate slider
        instantiatedSlider = Instantiate(sliderPrefab, optionsPanel);
        Transform valueTransform = instantiatedSlider.transform.Find("Value");
        if (valueTransform != null)
            valueText = valueTransform.GetComponent<TextMeshProUGUI>();
        else
            Debug.LogWarning("Value text child not found on slider prefab");

        instantiatedSlider.onValueChanged.AddListener(UpdateValueText);

        // Configure slider
        instantiatedSlider.minValue = 0;
        instantiatedSlider.maxValue = 100;
        instantiatedSlider.value = 50; // start at middle to avoid anchoring

        UpdateValueText(instantiatedSlider.value);
        instantiatedSlider.gameObject.SetActive(true);
    }

    void SetupButtons()
    {
        // For MIN_X questions, use x_labels as options if options array is not provided
        string[] options = currentQuestionData.task.options;
        if ((options == null || options.Length == 0) && currentQuestionData.x_labels != null)
        {
            options = currentQuestionData.x_labels;
            Debug.Log($"Using x_labels as options, count: {options.Length}");
        }

        if (options == null || options.Length == 0)
        {
            Debug.LogError("No options found for MIN_X question");
            return;
        }

        // Create a container for grid layout
        GameObject gridContainer = new GameObject("ButtonGrid");
        gridContainer.transform.SetParent(optionsPanel, false);
        RectTransform gridRect = gridContainer.AddComponent<RectTransform>();

        // Add GridLayoutGroup
        GridLayoutGroup gridLayout = gridContainer.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = buttonSize;
        gridLayout.spacing = new Vector2(buttonSpacing, buttonSpacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = buttonsPerRow;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // Set grid size based on number of options
        int optionCount = options.Length;
        int rows = Mathf.CeilToInt((float)optionCount / buttonsPerRow);
        float gridWidth = (buttonSize.x * buttonsPerRow) + (buttonSpacing * (buttonsPerRow - 1));
        float gridHeight = (buttonSize.y * rows) + (buttonSpacing * (rows - 1));
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);

        // Instantiate buttons
        for (int i = 0; i < optionCount; i++)
        {
            Button btn = Instantiate(buttonPrefab, gridContainer.transform);
            int index = i; // Capture for lambda

            // Set button text
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = options[i];
            }

            // Add click listener
            btn.onClick.AddListener(() => OnButtonClicked(index));

            instantiatedButtons.Add(btn);
        }
    }

    void OnButtonClicked(int index)
    {
        selectedOptionIndex = index;

        // Get the actual option text
        string selectedText = "";
        if (currentQuestionData.task.options != null && index < currentQuestionData.task.options.Length)
        {
            selectedText = currentQuestionData.task.options[index];
        }
        else if (currentQuestionData.x_labels != null && index < currentQuestionData.x_labels.Length)
        {
            selectedText = currentQuestionData.x_labels[index];
        }

        Debug.Log($"Selected option {index}: {selectedText}");

        // Visual feedback
        for (int i = 0; i < instantiatedButtons.Count; i++)
        {
            ColorBlock colors = instantiatedButtons[i].colors;
            if (i == index)
            {
                // Yellow color for selected button
                colors.normalColor = new Color(1f, 0.92f, 0.016f); // Yellow
                colors.highlightedColor = new Color(1f, 0.92f, 0.016f);
                colors.pressedColor = new Color(0.8f, 0.74f, 0.01f); // Darker yellow
                colors.selectedColor = new Color(1f, 0.92f, 0.016f);
            }
            else
            {
                // Grey color for unselected buttons
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f); // Grey
                colors.highlightedColor = new Color(0.6f, 0.6f, 0.6f);
                colors.pressedColor = new Color(0.4f, 0.4f, 0.4f);
                colors.selectedColor = new Color(0.5f, 0.5f, 0.5f);
            }
            instantiatedButtons[i].colors = colors;
        }
    }

    void ClearInstantiatedElements()
    {
        // Clear slider
        if (instantiatedSlider != null)
        {
            Destroy(instantiatedSlider.gameObject);
            instantiatedSlider = null;
            valueText = null;
        }

        // Clear buttons and their container
        if (instantiatedButtons.Count > 0)
        {
            // Destroy the grid container which will also destroy all buttons
            if (instantiatedButtons[0] != null && instantiatedButtons[0].transform.parent != null)
            {
                Destroy(instantiatedButtons[0].transform.parent.gameObject);
            }
        }
        instantiatedButtons.Clear();
        selectedOptionIndex = -1;
    }

    void ResetInput()
    {
        if (instantiatedSlider != null)
        {
            instantiatedSlider.value = 50;
            UpdateValueText(instantiatedSlider.value);
        }
        selectedOptionIndex = -1;
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
        return -1f;
    }

    public int GetSelectedOptionIndex()
    {
        return selectedOptionIndex;
    }

    public string GetSelectedOptionText()
    {
        if (selectedOptionIndex >= 0)
        {
            if (currentQuestionData.task.options != null && selectedOptionIndex < currentQuestionData.task.options.Length)
            {
                return currentQuestionData.task.options[selectedOptionIndex];
            }
            else if (currentQuestionData.x_labels != null && selectedOptionIndex < currentQuestionData.x_labels.Length)
            {
                return currentQuestionData.x_labels[selectedOptionIndex];
            }
        }
        return "";
    }

    public string GetCurrentTaskType()
    {
        if (currentQuestionData != null && currentQuestionData.task != null)
        {
            return currentQuestionData.task.task_type;
        }
        return "";
    }

    public string GetCorrectAnswer()
    {
        if (currentQuestionData != null && currentQuestionData.task != null)
        {
            return currentQuestionData.task.answer;
        }
        return "";
    }
}