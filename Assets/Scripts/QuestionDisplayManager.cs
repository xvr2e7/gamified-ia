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
    public GameObject feedbackPanel;

    [Header("Feedback UI")]
    public TextMeshProUGUI feedbackText;

    [Header("Prefabs")]
    public Slider sliderPrefab;
    public Button buttonPrefab;

    private QuestionData currentQuestionData;
    private TaskData currentTask;
    private List<ImageViewerController.ImageQuestionPair> allPairs;
    private int currentPairIndex = 0;

    private Slider instantiatedSlider;
    private TextMeshProUGUI valueText;

    private bool isPracticeMode = false;
    private bool isSimpleFeedbackMode = false;
    private bool showingFeedback = false;

    void Awake()
    {
        // Hide panels before anything renders
        sliderPanel.SetActive(false);
        buttonPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    public void SetPracticeMode(bool enabled)
    {
        isPracticeMode = enabled;
        showingFeedback = false;
    }

    public bool IsShowingFeedback()
    {
        return showingFeedback;
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
        else if (currentTask.panel == "button" || currentTask.panel == "buttons")
            SetupCarousel();
        else
            Debug.LogError($"Unknown panel type: {currentTask.panel}");
    }

    void SetupSlider()
    {
        // Activate slider panel; deactivate others
        sliderPanel.SetActive(true);
        buttonPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

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
        // Activate carousel; deactivate others
        buttonPanel.SetActive(true);
        sliderPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        // Use categories array for button options
        string[] opts = currentQuestionData.categories;

        var carousel = buttonPanel.GetComponent<ButtonsControl>();
        if (carousel != null)
        {
            carousel.Initialize(opts, buttonPrefab);
        }
        else
        {
            Debug.LogError("GridControlledButtons component not found on buttonPanel");
        }
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
        var carousel = buttonPanel?.GetComponent<ButtonsControl>();
        if (carousel != null)
            carousel.Clear();
    }

    void UpdateValueText(float val)
    {
        if (valueText != null)
            valueText.text = $"{val:0}";
    }

    public void ShowFeedback(string userAnswer)
    {
        if (isSimpleFeedbackMode)
        {
            ShowSimpleFeedback(userAnswer);
            return;
        }

        if (!isPracticeMode || feedbackPanel == null || feedbackText == null)
            return;

        showingFeedback = true;

        // Hide option panels
        sliderPanel.SetActive(false);
        buttonPanel.SetActive(false);

        // Show feedback panel
        feedbackPanel.SetActive(true);

        // Check if answer is correct
        string correctAnswer = GetCorrectAnswer();
        bool isCorrect = false;

        if (currentTask?.panel == "slider")
        {
            // Parse both user answer and correct answer as floats
            if (float.TryParse(userAnswer, out float userValue) &&
                float.TryParse(correctAnswer, out float correctValue))
            {
                // Check if within ±5 tolerance
                isCorrect = Mathf.Abs(userValue - correctValue) <= 5f;

                // Format feedback text
                int roundedCorrect = Mathf.RoundToInt(correctValue);
                int roundedUser = Mathf.RoundToInt(userValue);

                if (isCorrect)
                {
                    feedbackText.text = $"Correct! The correct answer is: {roundedCorrect}. Your answer was {roundedUser}.";
                    feedbackText.color = Color.green;
                }
                else
                {
                    feedbackText.text = $"Incorrect. The correct answer is: {roundedCorrect}.";
                    feedbackText.color = Color.red;
                }
            }
            else
            {
                // Fallback if parsing fails
                feedbackText.text = $"Incorrect. The correct answer is: {correctAnswer}.";
                feedbackText.color = Color.red;
            }
        }
        else
        {
            // Button questions - exact match required
            isCorrect = userAnswer.Equals(correctAnswer, System.StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                feedbackText.text = "Correct!";
                feedbackText.color = Color.green;
            }
            else
            {
                feedbackText.text = $"Incorrect. The correct answer is: {correctAnswer}";
                feedbackText.color = Color.red;
            }
        }
    }

    public void HideFeedbackAndContinue()
    {
        showingFeedback = false;
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
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

    public void SetSimpleFeedbackMode(bool enabled)
    {
        isSimpleFeedbackMode = enabled;
        showingFeedback = false;
    }

    public void ShowSimpleFeedback(string userAnswer)
    {
        if (!isSimpleFeedbackMode || feedbackPanel == null || feedbackText == null)
            return;

        showingFeedback = true;

        // Hide option panels
        sliderPanel.SetActive(false);
        buttonPanel.SetActive(false);

        // Show feedback panel
        feedbackPanel.SetActive(true);

        // Check if answer is correct
        string correctAnswer = GetCorrectAnswer();
        bool isCorrect = false;

        if (currentTask?.panel == "slider")
        {
            // Parse both user answer and correct answer as floats
            if (float.TryParse(userAnswer, out float userValue) &&
                float.TryParse(correctAnswer, out float correctValue))
            {
                // Check if within ±5 tolerance
                isCorrect = Mathf.Abs(userValue - correctValue) <= 5f;
            }
        }
        else
        {
            // Button questions - exact match required
            isCorrect = userAnswer.Equals(correctAnswer, System.StringComparison.OrdinalIgnoreCase);
        }

        // Show only "Correct!" or "Incorrect!" - no correct answer revealed
        if (isCorrect)
        {
            feedbackText.text = "Correct!";
            feedbackText.color = Color.green;
        }
        else
        {
            feedbackText.text = "Incorrect!";
            feedbackText.color = Color.red;
        }
    }

    // --- Public getters ---

    public float GetCurrentSliderValue()
        => instantiatedSlider != null ? instantiatedSlider.value : -1f;

    public int GetSelectedOptionIndex()
    {
        var c = buttonPanel?.GetComponent<ButtonsControl>();
        return c != null ? c.CurrentIndex : -1;
    }

    public string GetSelectedOptionText()
    {
        var c = buttonPanel?.GetComponent<ButtonsControl>();
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