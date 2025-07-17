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
    private const float STARTUP_DELAY = 0.5f; // Half second delay after activation

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
        // Record when this component became active
        activationTime = Time.time;
        triggerWasPressed = true; // Assume trigger is pressed on enable to prevent immediate action
    }

    void Update()
    {
        // Check if we should process input
        if (imageViewer == null || !gameObject.activeInHierarchy)
            return;

        // Don't process input for a short time after activation
        if (Time.time - activationTime < STARTUP_DELAY)
            return;

        // Read trigger values
        float leftTrigger = leftTriggerAction != null ? leftTriggerAction.action.ReadValue<float>() : 0f;
        float rightTrigger = rightTriggerAction != null ? rightTriggerAction.action.ReadValue<float>() : 0f;

        // Check if either trigger is pressed (threshold of 0.5)
        bool triggerPressed = leftTrigger > 0.5f || rightTrigger > 0.5f;

        // Detect trigger press (not hold) - only fires once per press
        if (triggerPressed && !triggerWasPressed)
        {
            HandleClick();
        }

        triggerWasPressed = triggerPressed;
    }

    private void HandleClick()
    {
        // Get input value based on question type
        float sliderValue = -1f;
        string selectedOption = "";
        string taskType = "";
        string correctAnswer = "";

        if (questionManager != null)
        {
            taskType = questionManager.GetCurrentTaskType();
            correctAnswer = questionManager.GetCorrectAnswer();

            // Check if it's a slider question
            if (taskType == "VALUE_PART")
            {
                sliderValue = questionManager.GetCurrentSliderValue();
            }
            // Check if it's a button question
            else if (taskType == "MIN_X")
            {
                int optionIndex = questionManager.GetSelectedOptionIndex();
                if (optionIndex != -1)
                {
                    selectedOption = questionManager.GetSelectedOptionText();
                }
                // Keep sliderValue as -1 for MIN_X questions
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

        // Check if answer is correct and handle streak/XP
        bool isCorrect = false;

        if (taskType == "VALUE_PART" && !string.IsNullOrEmpty(correctAnswer))
        {
            // For slider questions, check if the slider value matches the correct answer
            float correctValue;
            if (float.TryParse(correctAnswer, out correctValue))
            {
                // Allow some tolerance for float comparison
                isCorrect = Mathf.Abs(sliderValue - correctValue) < sliderError;
            }
        }
        else if (taskType == "MIN_X" && !string.IsNullOrEmpty(selectedOption))
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