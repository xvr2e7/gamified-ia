using UnityEngine;
using TMPro;

public class XPManager : MonoBehaviour
{
    [Header("XP Settings")]
    [SerializeField] private int xpPerSecond = 1;
    [SerializeField] private int xpPerCorrectAnswer = 10;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI xpDisplay;

    private int currentXP = 0;
    private float timer = 0f;
    private bool isCountingXP = false;

    private static XPManager instance;
    public static XPManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<XPManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        // Ensure only one instance exists
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateXPDisplay();
    }

    void Update()
    {
        // Only accumulate XP if counting is enabled
        if (isCountingXP)
        {
            timer += Time.deltaTime;
            if (timer >= 1f)
            {
                timer -= 1f;
                AddXP(xpPerSecond);
            }
        }
    }

    public void StartCounting()
    {
        isCountingXP = true;
        timer = 0f; // Reset timer to ensure clean start
        currentXP = 0; // Reset XP for new study session
        UpdateXPDisplay();
    }

    public void StopCounting()
    {
        isCountingXP = false;
    }

    public void AddXP(int amount)
    {
        currentXP += amount;
        UpdateXPDisplay();
    }

    public void AddXPForCorrectAnswer()
    {
        AddXP(xpPerCorrectAnswer);
    }

    private void UpdateXPDisplay()
    {
        if (xpDisplay != null)
        {
            xpDisplay.text = $"XP: {currentXP}";
        }
    }

    public int GetCurrentXP()
    {
        return currentXP;
    }

    public bool IsCountingXP()
    {
        return isCountingXP;
    }
}