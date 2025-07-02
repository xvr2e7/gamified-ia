using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class GridControlledButtons : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftGripAction;
    [SerializeField] private InputActionReference rightGripAction;

    private InputAction _leftGripClone;
    private InputAction _rightGripClone;

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
        _leftGripClone = leftGripAction.action.Clone();
        _rightGripClone = rightGripAction.action.Clone();
    }

    void OnEnable()
    {
        if (_leftGripClone != null)
        {
            _leftGripClone.Enable();
            _leftGripClone.started += OnLeftGrip;
        }
        if (_rightGripClone != null)
        {
            _rightGripClone.Enable();
            _rightGripClone.started += OnRightGrip;
        }
    }

    void OnDisable()
    {
        if (_leftGripClone != null)
        {
            _leftGripClone.started -= OnLeftGrip;
            _leftGripClone.Disable();
        }
        if (_rightGripClone != null)
        {
            _rightGripClone.started -= OnRightGrip;
            _rightGripClone.Disable();
        }
    }

    private void OnLeftGrip(InputAction.CallbackContext ctx)
    {
        if (ctx.ReadValue<float>() > 0.5f)
            Prev();
    }

    private void OnRightGrip(InputAction.CallbackContext ctx)
    {
        if (ctx.ReadValue<float>() > 0.5f)
            Next();
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
