using UnityEngine;
using TMPro;

public class XPManager : MonoBehaviour
{
    [Header("XP Settings")]
    [SerializeField] private int xpPerSecond = 1;
    [SerializeField] private int xpPerCorrectAnswer = 10;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI xpDisplay;

    [Header("Mushroom Counter")]
    [SerializeField] private MushroomCounter mushroomCounter;

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