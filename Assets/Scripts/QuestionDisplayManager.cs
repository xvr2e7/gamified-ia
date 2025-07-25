using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private QuestionData currentQuestionData;
    private TaskData currentTask;
    private List<ImageViewerController.ImageQuestionPair> allPairs;
    private int currentPairIndex = 0;

    private Slider instantiatedSlider;
    private TextMeshProUGUI valueText;

    void Awake()
    {
        // Hide panels before anything renders
        sliderPanel.SetActive(false);
        buttonPanel.SetActive(false);
    }

    public void InitializeWithPairs(List<ImageViewerController.ImageQuestionPair> pairs, int startIndex)
    {
        allPairs = pairs;
        currentPairIndex = startIndex;

        if (allPairs != null && allPairs.Count > 0)
        {
            LoadAndDisplayQuestion();
        }

        // Check if QuestionTimer should be started based on current condition
        if (ExperimentManager.Instance != null)
        {
            if (ExperimentManager.Instance.IsComponentActiveForCondition("QuestionTimer"))
            {
                if (QuestionTimer.Instance != null && QuestionTimer.Instance.gameObject.activeInHierarchy)
                {
                    QuestionTimer.Instance.StartTimer();
                }
            }
        }
        else
        {
            // Standalone mode - check if timer is active
            if (QuestionTimer.Instance != null && QuestionTimer.Instance.gameObject.activeInHierarchy)
            {
                QuestionTimer.Instance.StartTimer();
            }
        }
    }

    void LoadAndDisplayQuestion()
    {
        if (allPairs == null || currentPairIndex >= allPairs.Count)
        {
            Debug.LogError("No pairs available or index out of range");
            return;
        }

        var currentPair = allPairs[currentPairIndex];
        currentQuestionData = currentPair.metadata;
        currentTask = currentQuestionData.tasks[currentPair.taskIndex];

        // Update question text
        questionText.text = currentTask.question;

        // Clear previous UI
        ClearInstantiatedElements();

        // Show the right panel based on the panel type
        if (currentTask.panel == "slider")
            SetupSlider();
        else if (currentTask.panel == "button")
            SetupCarousel();
        else
            Debug.LogError($"Unknown panel type: {currentTask.panel}");
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

        // Configure slider range (0-100 for percentages)
        instantiatedSlider.minValue = 0;
        instantiatedSlider.maxValue = 100;
        instantiatedSlider.wholeNumbers = true;

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

        // Use categories array for button options
        string[] opts = currentQuestionData.categories;

        // Initialize the existing GridControlledButtons on buttonPanel
        var carousel = buttonPanel.GetComponent<GridControlledButtons>();

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

    public void LoadNextQuestion()
    {
        if (allPairs == null || allPairs.Count == 0) return;

        currentPairIndex++;
        if (currentPairIndex < allPairs.Count)
        {
            LoadAndDisplayQuestion();

            if (QuestionTimer.Instance != null)
                QuestionTimer.Instance.StartTimer();
        }
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
        => currentTask?.type ?? "";

    public string GetCurrentTaskFamily()
        => currentTask?.family ?? "";

    public string GetCurrentPanelType()
        => currentTask?.panel ?? "";

    public string GetCorrectAnswer()
        => currentTask?.GetStringAnswer() ?? "";

    public bool IsSliderQuestion()
        => currentTask?.panel == "slider";
}