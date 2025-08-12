using UnityEngine;
using TMPro;

public class XPManager : MonoBehaviour
{
    [Header("XP Settings")]
    [SerializeField] private int xpPerCorrectAnswer = 10;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI xpDisplay;

    [Header("Mushroom Counter")]
    [SerializeField] private MushroomCounter mushroomCounter;

    private int currentXP = 0;
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

        // Find mushroom counter if not assigned
        if (mushroomCounter == null)
        {
            mushroomCounter = FindObjectOfType<MushroomCounter>();
        }
    }

    void Start()
    {
        UpdateXPDisplay();
    }

    public void StartCounting()
    {
        isCountingXP = true;
        currentXP = 0; // Reset XP for new study session
        UpdateXPDisplay();

        // Notify mushroom counter
        if (mushroomCounter != null)
        {
            mushroomCounter.UpdateMushroomCount(currentXP);
        }
    }

    public void StopCounting()
    {
        isCountingXP = false;
    }

    public void AddXP(int amount)
    {
        currentXP += amount;
        UpdateXPDisplay();

        // Update mushroom counter
        if (mushroomCounter != null)
        {
            mushroomCounter.UpdateMushroomCount(currentXP);
        }
    }

    public void AddXPForCorrectAnswer()
    {
        // Apply streak multiplier to correct answer XP
        float multiplier = StreakMultiplier.Instance != null ? StreakMultiplier.Instance.GetXPMultiplier() : 1f;
        int multipliedXP = Mathf.RoundToInt(xpPerCorrectAnswer * multiplier);
        AddXP(multipliedXP);
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