using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class ImageClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ImageViewerController imageViewer;
    private QuestionDisplayManager questionManager;
    private BoxCollider boxCollider;
    private bool isHovered = false;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    private RawImage rawImage;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        rawImage = GetComponent<RawImage>();

        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        if (rawImage != null)
        {
            rawImage.raycastTarget = true;
        }
    }

    void Start()
    {
        imageViewer = GetComponentInParent<ImageViewerController>();
        if (imageViewer == null)
        {
            imageViewer = FindObjectOfType<ImageViewerController>();
        }

        questionManager = FindObjectOfType<QuestionDisplayManager>();
    }

    // UI Event System callbacks for Vive pointer
    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (rawImage != null)
        {
            rawImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (rawImage != null)
        {
            rawImage.color = normalColor;
        }
    }

    void OnMouseEnter()
    {
        if (!isHovered)
        {
            isHovered = true;
            if (rawImage != null)
            {
                rawImage.color = hoverColor;
            }
        }
    }

    void OnMouseExit()
    {
        if (isHovered)
        {
            isHovered = false;
            if (rawImage != null)
            {
                rawImage.color = normalColor;
            }
        }
    }

    private void HandleClick()
    {
        // Advance image
        if (imageViewer != null)
        {
            imageViewer.NextImage();
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