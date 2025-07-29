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

    [Header("Audio")]
    [SerializeField] private AudioClip ambienceClip;
    private AudioSource ambienceSource;

    [Header("Settings")]
    [SerializeField] private string imageFolderPath = "Stimulus";
    [SerializeField] private string metadataFolderPath = "Metadata";

    [Header("Trigger Input")]
    [SerializeField] private InputActionReference leftTriggerAction;
    [SerializeField] private InputActionReference rightTriggerAction;

    [SerializeField] private QuestionDisplayManager questionManager;

    public class ImageQuestionPair
    {
        public Texture2D texture;
        public QuestionData metadata;
        public int taskIndex;
        public string originalFileName;
    }

    private List<ImageQuestionPair> imageQuestionPairs = new List<ImageQuestionPair>();
    private int currentPairIndex = 0;
    private StudyDataLogger dataLogger;
    private Image openingPanelImage;
    private bool triggerWasPressed = false;
    private bool experimentControlled = false;

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

        // Ensure panels are hidden initially
        if (sliderPanel != null) sliderPanel.SetActive(false);
        if (buttonPanel != null) buttonPanel.SetActive(false);
    }

    private void Start()
    {
        // Set up ambience
        ambienceSource = gameObject.AddComponent<AudioSource>();
        ambienceSource.clip = ambienceClip;
        ambienceSource.loop = true;
        ambienceSource.volume = 0.3f;
        ambienceSource.playOnAwake = false;
        ambienceSource.spatialBlend = 0f;

        // Set initial panel states
        openingPanel.SetActive(true);
        endingPanel.SetActive(false);
        displayImage.gameObject.SetActive(false);
        imageCounter.gameObject.SetActive(false);
        questionPanel.SetActive(false);

        // Check if ExperimentManager exists
        if (ExperimentManager.Instance != null)
        {
            experimentControlled = true;
            // Don't load images - wait for ExperimentManager to provide them
        }
        else
        {
            // Only load images if not controlled by ExperimentManager (for standalone testing)
            LoadImagesAndMetadata();
        }

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


    private void ClearExistingPairs()
    {
        if (imageQuestionPairs != null)
        {
            // Clean up existing textures
            var uniqueTextures = new HashSet<Texture2D>();
            foreach (var pair in imageQuestionPairs)
            {
                if (pair.texture != null)
                    uniqueTextures.Add(pair.texture);
            }
            foreach (var texture in uniqueTextures)
            {
                if (texture != null) Destroy(texture);
            }
            imageQuestionPairs.Clear();
        }
    }

    private void StartStudy()
    {
        if (imageQuestionPairs.Count == 0)
        {
            Debug.LogError("No image-question pairs loaded. Cannot start study.");
            return;
        }

        if (ambienceSource != null) ambienceSource.Play();

        // Hide opening panel
        openingPanel.SetActive(false);

        // Show study UI
        displayImage.gameObject.SetActive(true);
        // imageCounter.gameObject.SetActive(true);
        questionPanel.SetActive(true);

        // NOW activate HUD elements for the current condition
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.ActivateHUDForStudy();

            // Only initialize XPManager if it should be active for current condition
            if (ExperimentManager.Instance.IsComponentActiveForCondition("XPManager"))
            {
                if (XPManager.Instance != null && XPManager.Instance.gameObject.activeInHierarchy)
                {
                    XPManager.Instance.StartCounting();
                }
            }

            // Only initialize StreakMultiplier if it should be active for current condition
            if (ExperimentManager.Instance.IsComponentActiveForCondition("StreakMultiplier"))
            {
                if (StreakMultiplier.Instance != null && StreakMultiplier.Instance.gameObject.activeInHierarchy)
                {
                    StreakMultiplier.Instance.ResetStreak();
                }
            }
        }
        else
        {
            // Standalone mode - check if objects are active
            if (XPManager.Instance != null && XPManager.Instance.gameObject.activeInHierarchy)
            {
                XPManager.Instance.StartCounting();
            }

            if (StreakMultiplier.Instance != null && StreakMultiplier.Instance.gameObject.activeInHierarchy)
            {
                StreakMultiplier.Instance.ResetStreak();
            }
        }

        // Start question system
        if (questionManager != null)
        {
            questionManager.enabled = true;
            bool practice = ExperimentManager.Instance?.IsPracticeMode() ?? false;
            questionManager.SetPracticeMode(practice);
            questionManager.InitializeWithPairs(imageQuestionPairs, currentPairIndex);
        }

        DisplayCurrentImage();

        if (dataLogger != null)
            dataLogger.StartImageTimer();
    }

    private void LoadImagesAndMetadata()
    {
        // This method is now only called for standalone testing
        // Load all metadata files first
        string metadataPath = Path.Combine(Application.streamingAssetsPath, metadataFolderPath);

        var metadataFiles = Directory.GetFiles(metadataPath, "*.json");
        var loadedMetadata = new List<QuestionData>();

        foreach (var file in metadataFiles)
        {
            string jsonContent = File.ReadAllText(file);
            try
            {
                // Replace numeric answers with string format before parsing
                string processedJson = System.Text.RegularExpressions.Regex.Replace(
                    jsonContent,
                    @"""answer""\s*:\s*(\d+(?:\.\d+)?)",
                    @"""answer"": ""$1"""
                );

                QuestionData data = JsonUtility.FromJson<QuestionData>(processedJson);
                if (data != null)
                {
                    loadedMetadata.Add(data);
                    Debug.Log($"Loaded metadata from {Path.GetFileName(file)} with {data.tasks.Length} tasks");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse JSON file {file}: {e.Message}");
            }
        }

        // Create pairs of images with questions
        foreach (var metadata in loadedMetadata)
        {
            // Load the image
            string imagePath = Path.Combine(Application.streamingAssetsPath, imageFolderPath, metadata.file);

            byte[] imageData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(imageData))
            {
                Debug.LogWarning($"Failed to load image: {imagePath}");
                continue;
            }

            texture.name = Path.GetFileNameWithoutExtension(metadata.file);

            // Create a pair for each task in the metadata
            for (int i = 0; i < metadata.tasks.Length; i++)
            {
                imageQuestionPairs.Add(new ImageQuestionPair
                {
                    texture = texture,
                    metadata = metadata,
                    taskIndex = i,
                    originalFileName = metadata.file
                });

                Debug.Log($"Task {i} for {metadata.file}: Question='{metadata.tasks[i].question}', Answer='{metadata.tasks[i].answer}', Panel='{metadata.tasks[i].panel}'");
            }
        }

        // Randomize the order
        ShuffleList(imageQuestionPairs);

        Debug.Log($"[ImageViewerController] Loaded {imageQuestionPairs.Count} image-question pairs from {loadedMetadata.Count} metadata files");
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

        // Deactivate HUD through ExperimentManager
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.DeactivateHUDForStudy();
        }

        // Hide study UI
        displayImage.gameObject.SetActive(false);
        imageCounter.gameObject.SetActive(false);
        questionPanel.SetActive(false);
        sliderPanel.SetActive(false);
        buttonPanel.SetActive(false);

        questionManager.enabled = false;

        // Show ending panel
        endingPanel.SetActive(true);

        if (ambienceSource != null) ambienceSource.Stop();

        // Notify experiment manager
        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.OnBlockComplete();
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void DisplayCurrentImage()
    {
        if (imageQuestionPairs.Count == 0 || currentPairIndex >= imageQuestionPairs.Count) return;

        var currentPair = imageQuestionPairs[currentPairIndex];
        displayImage.texture = currentPair.texture;
        // imageCounter.text = $"{currentPairIndex + 1} / {imageQuestionPairs.Count}";

        if (dataLogger != null && dataLogger.trackingManager != null)
        {
            dataLogger.trackingManager.StartTrackingForImage(currentPairIndex);
        }
    }

    private void OnDestroy()
    {
        // Disable input actions
        if (leftTriggerAction != null)
            leftTriggerAction.action.Disable();
        if (rightTriggerAction != null)
            rightTriggerAction.action.Disable();

        // Clean up textures
        ClearExistingPairs();
    }

    public void SetImageQuestionPairs(List<ImageQuestionPair> pairs)
    {
        // Clear existing pairs
        ClearExistingPairs();

        // Set new pairs
        imageQuestionPairs = pairs ?? new List<ImageQuestionPair>();
        currentPairIndex = 0;

        Debug.Log($"[ImageViewerController] Set {imageQuestionPairs.Count} image-question pairs");

        // Update counter to show correct total
        // if (imageCounter != null && imageQuestionPairs.Count > 0)
        // {
        //     imageCounter.text = $"1 / {imageQuestionPairs.Count}";
        // }
    }

    public void NextImage()
    {
        if (imageQuestionPairs.Count == 0) return;

        currentPairIndex++;
        if (currentPairIndex >= imageQuestionPairs.Count)
        {
            EndStudy();
            return;
        }

        DisplayCurrentImage();
    }

    public void PreviousImage()
    {
        if (imageQuestionPairs.Count == 0) return;
        currentPairIndex = (currentPairIndex - 1 + imageQuestionPairs.Count) % imageQuestionPairs.Count;
        DisplayCurrentImage();
    }


    public string GetCurrentImageName()
    {
        if (imageQuestionPairs.Count == 0 || currentPairIndex >= imageQuestionPairs.Count)
            return "";
        return imageQuestionPairs[currentPairIndex].originalFileName;
    }

    public int GetCurrentImageIndex() => currentPairIndex;

    public int GetLoadedImagesCount()
    {
        return imageQuestionPairs.Count;
    }

    public ImageQuestionPair GetCurrentPair()
    {
        if (imageQuestionPairs.Count == 0 || currentPairIndex >= imageQuestionPairs.Count)
            return null;
        return imageQuestionPairs[currentPairIndex];
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

}