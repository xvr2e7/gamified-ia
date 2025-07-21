using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ImageViewerController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage displayImage;
    [SerializeField] private TextMeshProUGUI imageCounter;
    [SerializeField] private TextMeshProUGUI startText;

    [Header("Panels")]
    [SerializeField] private GameObject openingPanel;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private GameObject sliderPanel;
    [SerializeField] private GameObject buttonPanel;

    [Header("HUD References")]
    [SerializeField] private GameObject hudGameObject;
    [SerializeField] private GameObject timerContainer;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalPanelColor = new Color(1f, 1f, 1f, 0.392f);

    [Header("Settings")]
    [SerializeField] private string imageFolderPath = "Data/SampleImages";

    [Header("Trigger Input")]
    [SerializeField] private InputActionReference leftTriggerAction;
    [SerializeField] private InputActionReference rightTriggerAction;

    [SerializeField] private QuestionDisplayManager questionManager;

    private List<Texture2D> loadedImages = new List<Texture2D>();
    private int currentImageIndex = 0;
    private StudyDataLogger dataLogger;
    private Image openingPanelImage;
    private bool triggerWasPressed = false;

    private void Awake()
    {
        dataLogger = gameObject.AddComponent<StudyDataLogger>();

        // Opening panel needs a raycast target
        if (openingPanel != null)
        {
            openingPanelImage = openingPanel.GetComponent<Image>() ?? openingPanel.AddComponent<Image>();
            openingPanelImage.color = normalPanelColor;
            openingPanelImage.raycastTarget = true;
        }

        if (sliderPanel != null) sliderPanel.SetActive(false);
        if (buttonPanel != null) buttonPanel.SetActive(false);
    }

    private void Start()
    {
        // Set initial panel states
        openingPanel.SetActive(true);
        endingPanel.SetActive(false);
        displayImage.gameObject.SetActive(false);
        imageCounter.gameObject.SetActive(false);
        questionPanel.SetActive(false);

        // Hide HUD and timer during opening panel
        if (hudGameObject != null)
        {
            hudGameObject.SetActive(false);
        }

        if (timerContainer != null)
        {
            timerContainer.SetActive(false);
        }

        LoadImages();

        // Enable input actions
        if (leftTriggerAction != null)
            leftTriggerAction.action.Enable();
        if (rightTriggerAction != null)
            rightTriggerAction.action.Enable();
    }

    void Update()
    {
        // Handle trigger input only for opening panel
        if (openingPanel != null && openingPanel.activeSelf)
        {
            float leftTrigger = leftTriggerAction != null ? leftTriggerAction.action.ReadValue<float>() : 0f;
            float rightTrigger = rightTriggerAction != null ? rightTriggerAction.action.ReadValue<float>() : 0f;

            bool triggerPressed = leftTrigger > 0.5f || rightTrigger > 0.5f;

            if (triggerPressed && !triggerWasPressed)
            {
                StartStudy();
            }

            triggerWasPressed = triggerPressed;
        }
    }

    private void StartStudy()
    {
        // Hide opening panel
        openingPanel.SetActive(false);

        // Show study UI
        displayImage.gameObject.SetActive(true);
        imageCounter.gameObject.SetActive(true);
        questionPanel.SetActive(true);

        // Show HUD and timer
        if (hudGameObject != null)
        {
            hudGameObject.SetActive(true);
        }

        if (timerContainer != null)
        {
            timerContainer.SetActive(true);
        }

        // Initialize HUD components
        if (XPManager.Instance != null)
        {
            XPManager.Instance.StartCounting();
        }

        if (StreakMultiplier.Instance != null)
        {
            StreakMultiplier.Instance.ResetStreak();
        }

        // Start question system and timer
        if (questionManager != null)
        {
            questionManager.enabled = true;
            questionManager.InitializeQuestions();
        }

        DisplayCurrentImage();

        if (dataLogger != null)
            dataLogger.StartImageTimer();
    }

    private void LoadImages()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, imageFolderPath);
        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"Image directory not found: {fullPath}");
            return;
        }

        string[] imageFiles = Directory.GetFiles(fullPath, "*.png");
        foreach (string file in imageFiles)
        {
            byte[] imageData = File.ReadAllBytes(file);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageData))
            {
                texture.name = Path.GetFileNameWithoutExtension(file);
                loadedImages.Add(texture);
            }
        }

        Debug.Log($"[ImageViewerController] Loaded {loadedImages.Count} images");
    }

    private void DisplayCurrentImage()
    {
        if (loadedImages.Count == 0) return;

        displayImage.texture = loadedImages[currentImageIndex];
        imageCounter.text = $"{currentImageIndex + 1} / {loadedImages.Count}";

        if (dataLogger != null && dataLogger.trackingManager != null)
        {
            dataLogger.trackingManager.StartTrackingForImage(currentImageIndex);
        }
    }

    public void NextImage()
    {
        if (loadedImages.Count == 0) return;

        currentImageIndex++;
        if (currentImageIndex >= loadedImages.Count)
        {
            EndStudy();
            return;
        }

        DisplayCurrentImage();
    }

    public void PreviousImage()
    {
        if (loadedImages.Count == 0) return;
        currentImageIndex = (currentImageIndex - 1 + loadedImages.Count) % loadedImages.Count;
        DisplayCurrentImage();
    }

    private void EndStudy()
    {
        // Stop tracking and save data
        if (dataLogger != null && dataLogger.trackingManager != null)
            dataLogger.trackingManager.StopTracking();

        dataLogger.SaveToFile();

        // Stop HUD components
        if (XPManager.Instance != null)
        {
            XPManager.Instance.StopCounting();
        }

        if (QuestionTimer.Instance != null)
        {
            QuestionTimer.Instance.StopTimer();
        }

        if (StreakMultiplier.Instance != null)
        {
            StreakMultiplier.Instance.ResetStreak();
        }

        // Hide study UI
        displayImage.gameObject.SetActive(false);
        imageCounter.gameObject.SetActive(false);
        questionPanel.SetActive(false);
        sliderPanel.SetActive(false);
        buttonPanel.SetActive(false);

        questionManager.enabled = false;

        // Hide HUD and timer
        if (hudGameObject != null)
        {
            hudGameObject.SetActive(false);
        }

        if (timerContainer != null)
        {
            timerContainer.SetActive(false);
        }

        // Show ending panel
        endingPanel.SetActive(true);
    }

    public string GetCurrentImageName()
    {
        if (loadedImages.Count == 0 || currentImageIndex >= loadedImages.Count)
            return "";
        return loadedImages[currentImageIndex].name;
    }

    public int GetCurrentImageIndex() => currentImageIndex;

    public int GetLoadedImagesCount()
    {
        return loadedImages.Count;
    }

    public bool IsOpeningPanelActive()
    {
        return openingPanel != null && openingPanel.activeSelf;
    }

    public bool IsEndingPanelActive()
    {
        return endingPanel != null && endingPanel.activeSelf;
    }

    public bool IsStudyActive()
    {
        return !IsOpeningPanelActive() && !IsEndingPanelActive();
    }

    private void OnDestroy()
    {
        // Disable input actions
        if (leftTriggerAction != null)
            leftTriggerAction.action.Disable();
        if (rightTriggerAction != null)
            rightTriggerAction.action.Disable();

        foreach (var t in loadedImages)
            if (t) Destroy(t);
        loadedImages.Clear();
    }
}