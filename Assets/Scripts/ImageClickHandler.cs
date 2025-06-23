using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class ImageClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ImageViewerController imageViewer;
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

        // Set up collider for 3D interaction as backup
        if (boxCollider != null)
        {
            // Make sure collider matches the RectTransform size
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                boxCollider.size = new Vector3(rect.rect.width, rect.rect.height, 1f);
                boxCollider.center = Vector3.zero;
            }
        }
    }

    // UI Event System callbacks for Vive pointer
    public void OnPointerClick(PointerEventData eventData)
    {
        NextImage();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (rawImage != null && hoverColor != null)
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

    // 3D Collider-based interaction as fallback
    void OnMouseDown()
    {
        NextImage();
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

    private void NextImage()
    {
        if (imageViewer != null)
        {
            imageViewer.NextImage();
        }
        else
        {
            Debug.LogError("[ViveImageClickHandler] Cannot advance - ImageViewerController is null!");
        }
    }

    // Keyboard shortcut for testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextImage();
        }

        // Debug ray visualization
        if (isHovered && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[ViveImageClickHandler] Currently hovered: {isHovered}");
        }
    }
}