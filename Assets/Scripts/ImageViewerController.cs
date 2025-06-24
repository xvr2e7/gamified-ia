using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

public class ImageViewerController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage displayImage;
    [SerializeField] private TextMeshProUGUI imageCounter;

    [Header("Settings")]
    [SerializeField] private string imageFolderPath = "Data/SampleImages";

    private List<Texture2D> loadedImages = new List<Texture2D>();
    private int currentImageIndex = 0;

    void Start()
    {
        Debug.Log("[ImageViewerController] Starting initialization");

        // Validate references
        if (displayImage == null)
        {
            Debug.LogError("[ImageViewerController] DisplayImage reference is missing!");
        }
        if (imageCounter == null)
        {
            Debug.LogError("[ImageViewerController] ImageCounter reference is missing!");
        }

        LoadImagesFromFolder();
        if (loadedImages.Count > 0)
        {
            DisplayCurrentImage();
        }
        else
        {
            Debug.LogWarning("No images found in the specified folder!");
        }
    }

    void LoadImagesFromFolder()
    {
        // Build the full path to StreamingAssets
        string fullPath = Path.Combine(Application.streamingAssetsPath, imageFolderPath);

        Debug.Log($"[ImageViewerController] Looking for images in: {fullPath}");

        // Check if directory exists
        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"[ImageViewerController] Directory not found: {fullPath}");
            return;
        }

        // Get all image files from the folder
        string[] supportedExtensions = { "*.png", "*.jpg", "*.jpeg" };
        List<string> imagePaths = new List<string>();

        foreach (string extension in supportedExtensions)
        {
            string[] paths = Directory.GetFiles(fullPath, extension);
            imagePaths.AddRange(paths);
        }

        // Sort the paths to ensure consistent ordering
        imagePaths.Sort();

        Debug.Log($"[ImageViewerController] Found {imagePaths.Count} image files");

        // Load each image
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
            Debug.Log($"[ImageViewerController] Loaded image: {texture.name}");

            // If this is the first image, display it immediately
            if (loadedImages.Count == 1)
            {
                DisplayCurrentImage();
            }
        }
        else
        {
            Debug.LogError($"[ImageViewerController] Failed to load image: {path}");
        }

        yield return null;
    }

    void DisplayCurrentImage()
    {
        if (loadedImages.Count == 0) return;

        // Display the image
        displayImage.texture = loadedImages[currentImageIndex];

        // Adjust the aspect ratio of the RawImage to match the texture
        if (displayImage.texture != null)
        {
            float aspectRatio = (float)displayImage.texture.width / displayImage.texture.height;
            RectTransform rectTransform = displayImage.GetComponent<RectTransform>();

            // Maintain height and adjust width
            float currentHeight = rectTransform.sizeDelta.y;
            rectTransform.sizeDelta = new Vector2(currentHeight * aspectRatio, currentHeight);
        }

        // Update the counter
        string fileName = loadedImages[currentImageIndex].name;
        imageCounter.text = $"{fileName} ({currentImageIndex + 1}/{loadedImages.Count})";
    }

    public void NextImage()
    {
        Debug.Log("[ImageViewerController] NextImage called");

        if (loadedImages.Count == 0)
        {
            Debug.LogWarning("[ImageViewerController] No images loaded!");
            return;
        }

        int previousIndex = currentImageIndex;
        currentImageIndex = (currentImageIndex + 1) % loadedImages.Count;
        Debug.Log($"[ImageViewerController] Advanced from image {previousIndex} to {currentImageIndex}");

        DisplayCurrentImage();
    }

    public void PreviousImage()
    {
        if (loadedImages.Count == 0) return;

        currentImageIndex--;
        if (currentImageIndex < 0)
            currentImageIndex = loadedImages.Count - 1;

        DisplayCurrentImage();
    }

    // Clean up textures when the object is destroyed
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