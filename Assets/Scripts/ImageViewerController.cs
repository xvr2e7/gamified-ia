using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ImageViewerController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

    [Header("Visual Feedback")]
    [SerializeField] private Color normalPanelColor = new Color(1f, 1f, 1f, 0.392f);
    [SerializeField] private Color hoverPanelColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

    [Header("Settings")]
    [SerializeField] private string imageFolderPath = "Data/SampleImages";

    [SerializeField] private QuestionDisplayManager questionManager;

    private List<Texture2D> loadedImages = new List<Texture2D>();
    private int currentImageIndex = 0;
    private StudyDataLogger dataLogger;
    private Image openingPanelImage;

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
        openingPanel.SetActive(true);
        endingPanel.SetActive(false);
        displayImage.gameObject.SetActive(false);
        imageCounter.gameObject.SetActive(false);
        questionPanel.SetActive(false);

        LoadImages();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (openingPanel != null && openingPanel.activeSelf && openingPanelImage != null)
            openingPanelImage.color = hoverPanelColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (openingPanel != null && openingPanel.activeSelf && openingPanelImage != null)
            openingPanelImage.color = normalPanelColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (openingPanel != null && openingPanel.activeSelf)
            StartStudy();
    }

    public void StartStudy()
    {
        openingPanel.SetActive(false);
        displayImage.gameObject.SetActive(true);
        imageCounter.gameObject.SetActive(true);
        questionPanel.SetActive(true);

        questionManager.InitializeQuestions();

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
        buttonPanel.SetActive(false);

        questionManager.enabled = false;

        endingPanel.SetActive(true);
    }

    public string GetCurrentImageName()
    {
        if (loadedImages.Count == 0 || currentImageIndex >= loadedImages.Count)
            return "";
        return loadedImages[currentImageIndex].name;
    }

    public int GetCurrentImageIndex() => currentImageIndex;

    // private void Update()
    // {
    //     if (openingPanel != null && openingPanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
    //         StartStudy();
    // }

    private void OnDestroy()
    {
        foreach (var t in loadedImages)
            if (t) Destroy(t);
        loadedImages.Clear();
    }
}
