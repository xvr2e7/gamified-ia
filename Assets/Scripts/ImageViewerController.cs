using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ImageViewerController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage displayImage;
    [SerializeField] private TextMeshProUGUI imageCounter;
    [SerializeField] private Button startButton;

    [Header("Panels")]
    [SerializeField] private GameObject openingPanel;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private GameObject sliderPanel;

    [Header("Settings")]
    [SerializeField] private string imageFolderPath = "Data/SampleImages";

    private List<Texture2D> loadedImages = new List<Texture2D>();
    private int currentImageIndex = 0;
    private StudyDataLogger dataLogger;

    private void Awake()
    {
        // Hook Start button
        if (startButton != null)
            startButton.onClick.AddListener(StartStudy);

        dataLogger = gameObject.AddComponent<StudyDataLogger>();
    }

    private void Start()
    {
        // Initial UI state
        openingPanel.SetActive(true);
        endingPanel.SetActive(false);
        displayImage.gameObject.SetActive(false);
        imageCounter.gameObject.SetActive(false);
        questionPanel.SetActive(false);
        sliderPanel.SetActive(false);

        LoadImages();
    }

    public void StartStudy()
    {
        // Show study UI
        openingPanel.SetActive(false);
        displayImage.gameObject.SetActive(true);
        imageCounter.gameObject.SetActive(true);
        questionPanel.SetActive(true);
        sliderPanel.SetActive(true);

        if (loadedImages.Count > 0)
        {
            currentImageIndex = 0;
            DisplayCurrentImage();
            dataLogger.StartImageTimer();
        }
    }

    private void LoadImages()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, imageFolderPath);
        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"[ImageViewerController] Directory not found: {fullPath}");
            return;
        }

        var files = new List<string>();
        foreach (var ext in new[] { "*.png", "*.jpg", "*.jpeg" })
            files.AddRange(Directory.GetFiles(fullPath, ext));

        files.Sort();
        foreach (var path in files)
            StartCoroutine(LoadImageCoroutine(path));
    }

    private IEnumerator LoadImageCoroutine(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        if (tex.LoadImage(data))
        {
            tex.name = Path.GetFileNameWithoutExtension(path);
            loadedImages.Add(tex);
        }
        yield return null;
    }

    private void DisplayCurrentImage()
    {
        var tex = loadedImages[currentImageIndex];
        displayImage.texture = tex;

        // Keep aspect ratio
        var rt = displayImage.rectTransform;
        float h = rt.sizeDelta.y;
        rt.sizeDelta = new Vector2(h * tex.width / tex.height, h);

        imageCounter.text = $"{tex.name} ({currentImageIndex + 1}/{loadedImages.Count})";
    }

    public void NextImage()
    {
        if (++currentImageIndex >= loadedImages.Count)
        {
            EndStudy();
            return;
        }

        DisplayCurrentImage();
        dataLogger.StartImageTimer();
    }

    public void PreviousImage()
    {
        if (loadedImages.Count == 0) return;
        currentImageIndex = (currentImageIndex - 1 + loadedImages.Count) % loadedImages.Count;
        DisplayCurrentImage();
    }

    private void EndStudy()
    {
        dataLogger.SaveToFile();

        displayImage.gameObject.SetActive(false);
        imageCounter.gameObject.SetActive(false);
        questionPanel.SetActive(false);
        sliderPanel.SetActive(false);
        endingPanel.SetActive(true);
    }

    public string GetCurrentImageName()
    {
        if (loadedImages.Count == 0 || currentImageIndex >= loadedImages.Count)
            return "";
        return loadedImages[currentImageIndex].name;
    }

    public int GetCurrentImageIndex()
    {
        return currentImageIndex;
    }

    private void Update()
    {
        // When the opening panel is active, allow Space to start the study
        if (openingPanel != null && openingPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Space))
        {
            StartStudy();
        }
    }

    private void OnDestroy()
    {
        foreach (var t in loadedImages)
            if (t) Destroy(t);
        loadedImages.Clear();
    }
}
