using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RawImage))]
public class ImageClickHandler : MonoBehaviour
{
    [SerializeField] private float sliderError = 5f;

    [Header("References")]
    public StudyDataLogger dataLogger;
    public ImageViewerController imageViewer;
    public QuestionDisplayManager questionManager;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f);

    [Header("Trigger Input")]
    [SerializeField] private InputActionReference leftTriggerAction;
    [SerializeField] private InputActionReference rightTriggerAction;

    private RawImage rawImage;
    private bool triggerWasPressed = false;
    private float activationTime = 0f;
    private const float STARTUP_DELAY = 0.5f;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.color = normalColor;
        }

        // Auto-find references
        if (dataLogger == null)
            dataLogger = FindObjectOfType<StudyDataLogger>();
        if (imageViewer == null)
            imageViewer = FindObjectOfType<ImageViewerController>();
        if (questionManager == null)
            questionManager = FindObjectOfType<QuestionDisplayManager>();

        // Enable input actions
        if (leftTriggerAction != null)
            leftTriggerAction.action.Enable();
        if (rightTriggerAction != null)
            rightTriggerAction.action.Enable();
    }

    void OnEnable()
    {
        activationTime = Time.time;
        triggerWasPressed = true;
    }

    void Update()
    {
        if (imageViewer == null || !gameObject.activeInHierarchy)
            return;

        if (Time.time - activationTime < STARTUP_DELAY)
            return;

        float leftTrigger = leftTriggerAction != null ? leftTriggerAction.action.ReadValue<float>() : 0f;
        float rightTrigger = rightTriggerAction != null ? rightTriggerAction.action.ReadValue<float>() : 0f;

        bool triggerPressed = leftTrigger > 0.5f || rightTrigger > 0.5f;

        if (triggerPressed && !triggerWasPressed)
        {
            HandleClick();
        }

        triggerWasPressed = triggerPressed;
    }

    private void HandleClick()
    {
        // Practice mode check for feedback state
        bool isPracticeMode = ExperimentManager.Instance != null && ExperimentManager.Instance.IsPracticeMode();
        if (isPracticeMode && questionManager != null && questionManager.IsShowingFeedback())
        {
            // Hide feedback and continue
            questionManager.HideFeedbackAndContinue();

            bool isLast = imageViewer.GetCurrentImageIndex() >= imageViewer.GetLoadedImagesCount() - 1;

            // Advance image
            if (imageViewer != null)
            {
                imageViewer.NextImage();
                if (dataLogger != null && !isLast)
                {
                    dataLogger.StartImageTimer();
                }
            }

            // Advance question
            if (!isLast && questionManager != null)
            {
                questionManager.LoadNextQuestion();
            }
            return;
        }

        // Get input value based on question type
        float sliderValue = -1f;
        string selectedOption = "";
        string taskType = "";
        string taskFamily = "";
        string panelType = "";
        string correctAnswer = "";

        if (questionManager != null)
        {
            taskType = questionManager.GetCurrentTaskType();
            taskFamily = questionManager.GetCurrentTaskFamily();
            panelType = questionManager.GetCurrentPanelType();
            correctAnswer = questionManager.GetCorrectAnswer();

            // Check if it's a slider question
            if (panelType == "slider")
            {
                sliderValue = questionManager.GetCurrentSliderValue();
            }
            // Check if it's a button question
            else if (panelType == "button")
            {
                int optionIndex = questionManager.GetSelectedOptionIndex();
                if (optionIndex != -1)
                {
                    selectedOption = questionManager.GetSelectedOptionText();
                }
            }
        }

        // Record data for current image before advancing
        if (dataLogger != null && imageViewer != null)
        {
            dataLogger.RecordImageData(
                imageViewer.GetCurrentImageIndex(),
                imageViewer.GetCurrentImageName(),
                sliderValue,
                selectedOption,
                taskType,
                correctAnswer
            );
        }

        // Practice mode: show feedback instead of advancing
        if (isPracticeMode && questionManager != null)
        {
            string userAnswer = "";
            if (panelType == "slider")
            {
                userAnswer = Mathf.RoundToInt(sliderValue).ToString();
            }
            else if (panelType == "button" && !string.IsNullOrEmpty(selectedOption))
            {
                userAnswer = selectedOption;
            }

            questionManager.ShowFeedback(userAnswer);
            return;
        }

        // Check if answer is correct and handle streak/XP
        bool isCorrect = false;

        if (panelType == "slider" && questionManager.IsSliderQuestion())
        {
            // For slider questions, parse the correct answer as float and check
            float correctValue;
            if (float.TryParse(correctAnswer, out correctValue))
            {
                // Allow some tolerance for float comparison
                isCorrect = Mathf.Abs(sliderValue - correctValue) < sliderError;
            }
        }
        else if (panelType == "button" && !string.IsNullOrEmpty(selectedOption))
        {
            // For multiple choice questions, check if selected option matches correct answer
            isCorrect = selectedOption == correctAnswer;
        }

        // Handle streak multiplier and award XP
        if (isCorrect)
        {
            // Notify streak multiplier of correct answer (this will check if within countdown)
            if (StreakMultiplier.Instance != null)
            {
                StreakMultiplier.Instance.OnCorrectAnswer();
            }

            // Award XP with current multiplier
            if (XPManager.Instance != null)
            {
                XPManager.Instance.AddXPForCorrectAnswer();
            }
        }
        else
        {
            // Reset streak on wrong answer
            if (StreakMultiplier.Instance != null)
            {
                StreakMultiplier.Instance.OnWrongAnswer();
            }
        }

        // Check if this is the last image before advancing
        bool isLastImage = false;
        if (imageViewer != null)
        {
            isLastImage = imageViewer.GetCurrentImageIndex() >= imageViewer.GetLoadedImagesCount() - 1;
        }

        // Advance image
        if (imageViewer != null)
        {
            imageViewer.NextImage();
            // Start timer for next image (only if not the last one)
            if (dataLogger != null && !isLastImage)
            {
                dataLogger.StartImageTimer();
            }
        }
        else
        {
            Debug.LogError("[ImageClickHandler] Cannot advance - ImageViewerController is null!");
        }

        // Only advance question if we haven't ended the study
        if (!isLastImage && questionManager != null)
        {
            questionManager.LoadNextQuestion();
        }
    }

    void OnDestroy()
    {
        // Disable input actions
        if (leftTriggerAction != null)
            leftTriggerAction.action.Disable();
        if (rightTriggerAction != null)
            rightTriggerAction.action.Disable();
    }
}