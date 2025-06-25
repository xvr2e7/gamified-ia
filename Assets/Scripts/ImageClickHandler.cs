using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ImageClickHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    public StudyDataLogger dataLogger;
    public ImageViewerController imageViewer;
    public QuestionDisplayManager questionManager;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f);

    private RawImage rawImage;

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
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rawImage != null)
        {
            rawImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rawImage != null)
        {
            rawImage.color = normalColor;
        }
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

        // Advance image
        if (imageViewer != null)
        {
            imageViewer.NextImage();
            // Start timer for next image
            if (dataLogger != null)
            {
                dataLogger.StartImageTimer();
            }
        }
        else
        {
            Debug.LogError("[ImageClickHandler] Cannot advance - ImageViewerController is null!");
        }

        // Advance question and reset slider
        if (questionManager != null)
        {
            questionManager.LoadNextQuestion();
        }
        else
        {
            Debug.LogError("[ImageClickHandler] Cannot advance question - QuestionDisplayManager is null!");
        }
    }
}