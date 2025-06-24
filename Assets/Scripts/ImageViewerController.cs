using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ImageViewerController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage displayImage;
    [SerializeField] private TextMeshProUGUI imageCounter;

    [Header("Panels")]
    [SerializeField] private GameObject openingPanel;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private Button startButton;

    [Header("External UI Elements")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private GameObject sliderPanel;

    [Header("Settings")]
    [SerializeField] private string imageFolderPath = "Data/SampleImages";

    private List<Texture2D> loadedImages = new List<Texture2D>();
    private int currentImageIndex = 0;
    private GameObject imageGameObject;
    private GameObject counterGameObject;

    void Awake()
    {
        Debug.Log("[ImageViewerController] AWAKE - Setting up button listener");

        // Set up button listener in Awake to ensure it's early enough
        if (startButton != null)
        {
            // Method 1: Using UnityAction
            UnityAction buttonAction = () =>
            {
                Debug.Log("[ImageViewerController] Button clicked via UnityAction!");
                StartStudy();
            };
            startButton.onClick.AddListener(buttonAction);

            // Method 2: Also add via Inspector-friendly method
            // This creates a persistent listener that shows in Inspector
            if (startButton.onClick.GetPersistentEventCount() == 0)
            {
                Debug.Log("[ImageViewerController] Adding persistent listener");
                // Note: This only works in Editor, but helps for debugging
#if UNITY_EDITOR
                UnityEditor.Events.UnityEventTools.AddPersistentListener(startButton.onClick, StartStudy);
#endif
            }

            Debug.Log($"[ImageViewerController] Button listeners after Awake: {startButton.onClick.GetPersistentEventCount()}");
        }
    }

    void Start()
    {
        Debug.Log("[ImageViewerController] ========== START INITIALIZATION ==========");

        // Get references to the GameObjects we need to show/hide
        if (displayImage != null)
        {
            imageGameObject = displayImage.gameObject;
            Debug.Log($"[ImageViewerController] Image GameObject found: {imageGameObject.name}");
        }

        if (imageCounter != null)
        {
            counterGameObject = imageCounter.gameObject;
            Debug.Log($"[ImageViewerController] Counter GameObject found: {counterGameObject.name}");
        }

        // Find QuestionPanel and SliderPanel if not assigned
        if (questionPanel == null)
        {
            GameObject foundPanel = GameObject.Find("QuestionPanel");
            if (foundPanel != null)
            {
                questionPanel = foundPanel;
                Debug.Log("[ImageViewerController] QuestionPanel found by name");
            }
        }

        if (sliderPanel == null)
        {
            GameObject foundPanel = GameObject.Find("SliderPanel");
            if (foundPanel != null)
            {
                sliderPanel = foundPanel;
                Debug.Log("[ImageViewerController] SliderPanel found by name");
            }
        }

        // Double-check button setup
        if (startButton != null)
        {
            Debug.Log($"[ImageViewerController] Checking button in Start...");

            // Add XR-specific check
            var imageCanvas = GameObject.Find("ImageCanvas");
            if (imageCanvas != null)
            {
                // Make sure we have TrackedDeviceGraphicRaycaster for XR
                var trackedRaycaster = imageCanvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                if (trackedRaycaster == null)
                {
                    Debug.LogWarning("[ImageViewerController] No TrackedDeviceGraphicRaycaster found! Adding one...");
                    imageCanvas.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                }
            }

            // Test the button directly
            startButton.onClick.Invoke();
            Debug.Log("[ImageViewerController] Tested button.onClick.Invoke() - if StartStudy was called, button is working");
        }

        // Initial visibility setup
        SetupInitialVisibility();

        // Load images
        LoadImagesFromFolder();

        Debug.Log("[ImageViewerController] ========== END INITIALIZATION ==========");
    }

    void SetupInitialVisibility()
    {
        Debug.Log("[ImageViewerController] Setting up initial visibility...");

        if (openingPanel != null) openingPanel.SetActive(true);
        if (endingPanel != null) endingPanel.SetActive(false);
        if (imageGameObject != null) imageGameObject.SetActive(false);
        if (counterGameObject != null) counterGameObject.SetActive(false);
        if (questionPanel != null) questionPanel.SetActive(false);
        if (sliderPanel != null) sliderPanel.SetActive(false);
    }

    public void StartStudy()
    {
        Debug.Log("[ImageViewerController] !!!!! StartStudy() CALLED !!!!!");
        Debug.Log($"[ImageViewerController] Loaded images count: {loadedImages.Count}");

        if (openingPanel != null)
        {
            openingPanel.SetActive(false);
            Debug.Log("[ImageViewerController] Opening panel HIDDEN");
        }

        if (imageGameObject != null)
        {
            imageGameObject.SetActive(true);
            Debug.Log("[ImageViewerController] Image SHOWN");
        }
        if (counterGameObject != null)
        {
            counterGameObject.SetActive(true);
            Debug.Log("[ImageViewerController] Counter SHOWN");
        }
        if (questionPanel != null)
        {
            questionPanel.SetActive(true);
            Debug.Log("[ImageViewerController] Question panel SHOWN");
        }
        if (sliderPanel != null)
        {
            sliderPanel.SetActive(true);
            Debug.Log("[ImageViewerController] Slider panel SHOWN");
        }

        if (loadedImages.Count > 0)
        {
            Debug.Log("[ImageViewerController] Displaying first image...");
            DisplayCurrentImage();
        }
    }

    void Update()
    {
        // Test with keyboard
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[ImageViewerController] SPACE key pressed - calling StartStudy");
            StartStudy();
        }
    }

    // Alternative approach - Public method for button OnClick in Inspector
    public void OnStartButtonClicked()
    {
        Debug.Log("[ImageViewerController] OnStartButtonClicked called from Inspector!");
        StartStudy();
    }

    void LoadImagesFromFolder()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, imageFolderPath);
        Debug.Log($"[ImageViewerController] Looking for images in: {fullPath}");

        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"[ImageViewerController] Directory not found: {fullPath}");
            return;
        }

        string[] supportedExtensions = { "*.png", "*.jpg", "*.jpeg" };
        List<string> imagePaths = new List<string>();

        foreach (string extension in supportedExtensions)
        {
            string[] paths = Directory.GetFiles(fullPath, extension);
            imagePaths.AddRange(paths);
        }

        imagePaths.Sort();
        Debug.Log($"[ImageViewerController] Found {imagePaths.Count} image files");

        foreach (string path in imagePaths)
        {
            StartCoroutine(LoadImageCoroutine(path));
        }
    }

    IEnumerator LoadImageCoroutine(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);

        if (texture.LoadImage(fileData))
        {
            texture.name = Path.GetFileNameWithoutExtension(path);
            loadedImages.Add(texture);
        }

        yield return null;
    }

    void DisplayCurrentImage()
    {
        if (loadedImages.Count == 0) return;

        displayImage.texture = loadedImages[currentImageIndex];

        if (displayImage.texture != null)
        {
            float aspectRatio = (float)displayImage.texture.width / displayImage.texture.height;
            RectTransform rectTransform = displayImage.GetComponent<RectTransform>();
            float currentHeight = rectTransform.sizeDelta.y;
            rectTransform.sizeDelta = new Vector2(currentHeight * aspectRatio, currentHeight);
        }

        string fileName = loadedImages[currentImageIndex].name;
        imageCounter.text = $"{fileName} ({currentImageIndex + 1}/{loadedImages.Count})";
    }

    public void NextImage()
    {
        Debug.Log("[ImageViewerController] NextImage called");

        if (loadedImages.Count == 0) return;

        currentImageIndex++;

        if (currentImageIndex >= loadedImages.Count)
        {
            EndStudy();
        }
        else
        {
            DisplayCurrentImage();
        }
    }

    void EndStudy()
    {
        Debug.Log("[ImageViewerController] Ending study");

        if (imageGameObject != null) imageGameObject.SetActive(false);
        if (counterGameObject != null) counterGameObject.SetActive(false);
        if (questionPanel != null) questionPanel.SetActive(false);
        if (sliderPanel != null) sliderPanel.SetActive(false);

        if (endingPanel != null) endingPanel.SetActive(true);
    }

    public void PreviousImage()
    {
        if (loadedImages.Count == 0) return;

        currentImageIndex--;
        if (currentImageIndex < 0)
            currentImageIndex = loadedImages.Count - 1;

        DisplayCurrentImage();
    }

    void OnDestroy()
    {
        foreach (var texture in loadedImages)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
        loadedImages.Clear();
    }
}