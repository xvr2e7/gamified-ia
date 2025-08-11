using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ButtonsControl : MonoBehaviour
{
    [Header("Trackpad Input Actions")]
    [SerializeField] private InputActionReference leftTrackpadClick;
    [SerializeField] private InputActionReference leftTrackpadPosition;
    [SerializeField] private InputActionReference rightTrackpadClick;
    [SerializeField] private InputActionReference rightTrackpadPosition;

    [Header("Trackpad Settings")]
    [SerializeField] private float horizontalThreshold = 0.3f; // How far left/right to register

    private InputAction _leftClickClone;
    private InputAction _leftPositionClone;
    private InputAction _rightClickClone;
    private InputAction _rightPositionClone;

    private string[] options;
    private Button buttonPrefab;
    private Button instantiatedButton;

    public int CurrentIndex { get; private set; } = 0;
    public string CurrentText => (options != null && options.Length > 0)
        ? options[CurrentIndex]
        : string.Empty;

    private void Awake()
    {
        // Create independent copies
        if (leftTrackpadClick != null)
        {
            _leftClickClone = leftTrackpadClick.action.Clone();
            _leftPositionClone = leftTrackpadPosition.action.Clone();
        }
        if (rightTrackpadClick != null)
        {
            _rightClickClone = rightTrackpadClick.action.Clone();
            _rightPositionClone = rightTrackpadPosition.action.Clone();
        }
    }

    void OnEnable()
    {
        if (_leftClickClone != null && _leftPositionClone != null)
        {
            _leftClickClone.Enable();
            _leftPositionClone.Enable();
            _leftClickClone.performed += OnLeftTrackpadClick;
        }
        if (_rightClickClone != null && _rightPositionClone != null)
        {
            _rightClickClone.Enable();
            _rightPositionClone.Enable();
            _rightClickClone.performed += OnRightTrackpadClick;
        }
    }

    void OnDisable()
    {
        if (_leftClickClone != null)
        {
            _leftClickClone.performed -= OnLeftTrackpadClick;
            _leftClickClone.Disable();
        }
        if (_leftPositionClone != null)
        {
            _leftPositionClone.Disable();
        }
        if (_rightClickClone != null)
        {
            _rightClickClone.performed -= OnRightTrackpadClick;
            _rightClickClone.Disable();
        }
        if (_rightPositionClone != null)
        {
            _rightPositionClone.Disable();
        }
    }

    private void OnLeftTrackpadClick(InputAction.CallbackContext ctx)
    {
        if (_leftPositionClone != null)
        {
            Vector2 trackpadPos = _leftPositionClone.ReadValue<Vector2>();
            HandleTrackpadPress(trackpadPos);
        }
    }

    private void OnRightTrackpadClick(InputAction.CallbackContext ctx)
    {
        if (_rightPositionClone != null)
        {
            Vector2 trackpadPos = _rightPositionClone.ReadValue<Vector2>();
            HandleTrackpadPress(trackpadPos);
        }
    }

    private void HandleTrackpadPress(Vector2 trackpadPosition)
    {
        // Check if press is on left or right side of trackpad
        if (trackpadPosition.x < -horizontalThreshold)
        {
            Prev(); // Left side pressed
        }
        else if (trackpadPosition.x > horizontalThreshold)
        {
            Next(); // Right side pressed
        }
    }

    public void Initialize(string[] optionsList, Button prefab)
    {
        options = optionsList;
        buttonPrefab = prefab;
        CurrentIndex = 0;
        InstantiateSingle();
    }

    private void InstantiateSingle()
    {
        if (instantiatedButton != null)
            Destroy(instantiatedButton.gameObject);

        instantiatedButton = Instantiate(buttonPrefab, transform);

        var txt = instantiatedButton.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
            txt.text = options[CurrentIndex];

        var leftBtn = instantiatedButton.transform.Find("LeftArrow")?.GetComponent<Button>();
        var rightBtn = instantiatedButton.transform.Find("RightArrow")?.GetComponent<Button>();

        if (leftBtn != null)
        {
            leftBtn.onClick.RemoveAllListeners();
            leftBtn.onClick.AddListener(Prev);
        }
        if (rightBtn != null)
        {
            rightBtn.onClick.RemoveAllListeners();
            rightBtn.onClick.AddListener(Next);
        }
    }

    public void Next()
    {
        if (options == null || options.Length == 0) return;
        CurrentIndex = (CurrentIndex + 1) % options.Length;
        InstantiateSingle();
    }

    public void Prev()
    {
        if (options == null || options.Length == 0) return;
        CurrentIndex = (CurrentIndex - 1 + options.Length) % options.Length;
        InstantiateSingle();
    }

    public void Clear()
    {
        if (instantiatedButton != null)
            Destroy(instantiatedButton.gameObject);
        options = null;
    }
}