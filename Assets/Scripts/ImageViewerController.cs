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
    [SerializeField] private string imageFolderPath = "Assets/Data/SampleImages";

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
        // Get all image files from the folder
        string[] supportedExtensions = { "*.png", "*.jpg", "*.jpeg" };
        List<string> imagePaths = new List<string>();

        foreach (string extension in supportedExtensions)
        {
            string[] paths = Directory.GetFiles(imageFolderPath, extension);
            imagePaths.AddRange(paths);
        }

        // Sort the paths to ensure consistent ordering
        imagePaths.Sort();

        // Load each image
        foreach (string path in imagePaths)
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(fileData))
            {
                texture.name = Path.GetFileName(path);
                loadedImages.Add(texture);
                Debug.Log($"Loaded image: {texture.name}");
            }
        }

        Debug.Log($"Total images loaded: {loadedImages.Count}");
    }

    void DisplayCurrentImage()
    {
        if (loadedImages.Count == 0) return;

        // Display the image
        displayImage.texture = loadedImages[currentImageIndex];

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
}