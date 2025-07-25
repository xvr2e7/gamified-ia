using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StreakMultiplier : MonoBehaviour
{
    [Header("Streak Settings")]
    [SerializeField] private int maxMultiplier = 10;
    [SerializeField] private float streakBonusPerLevel = 1f;

    [Header("Visual Elements")]
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private Image borderImage;
    [SerializeField] private GameObject multiplierContainer;

    [Header("Color Progression")]
    [SerializeField]
    private Color[] streakColors = new Color[]
    {
        new Color(0.5f, 0.5f, 0.5f, 0.8f), // x1 - Gray
        new Color(0.2f, 0.8f, 0.2f, 0.8f), // x2 - Green
        new Color(0.3f, 0.7f, 0.9f, 0.8f), // x3 - Light Blue
        new Color(0.9f, 0.7f, 0.2f, 0.8f), // x4 - Gold
        new Color(0.9f, 0.4f, 0.1f, 0.8f), // x5 - Orange
        new Color(0.8f, 0.1f, 0.1f, 0.8f), // x6+ - Red
    };

    [Header("Animation")]
    [SerializeField] private float pulseIntensity = 0.15f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int currentStreak = 1;
    private Vector3 originalScale;
    private Coroutine pulseCoroutine;

    // Events
    public System.Action<int> OnStreakChanged;
    public System.Action OnStreakReset;

    private static StreakMultiplier instance;
    public static StreakMultiplier Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<StreakMultiplier>();
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

        // Store original scale
        if (multiplierContainer != null)
            originalScale = multiplierContainer.transform.localScale;
        else if (transform != null)
            originalScale = transform.localScale;
    }

    void Start()
    {
        // Subscribe to timer events
        if (QuestionTimer.Instance != null)
        {
            QuestionTimer.Instance.OnTimeOut += ResetStreak;
        }

        InitializeDisplay();
    }

    void OnDestroy()
    {
        // Unsubscribe from timer events
        if (QuestionTimer.Instance != null)
        {
            QuestionTimer.Instance.OnTimeOut -= ResetStreak;
        }
    }

    private void InitializeDisplay()
    {
        UpdateDisplay();
    }

    public void OnCorrectAnswer()
    {
        // Only increase streak if timer is still active (answered within countdown)
        if (QuestionTimer.Instance != null && QuestionTimer.Instance.IsTimerActive && !QuestionTimer.Instance.HasTimedOut)
        {
            IncreaseStreak();
        }
        else
        {
            // Reset if answered after timeout
            ResetStreak();
        }
    }

    public void OnWrongAnswer()
    {
        ResetStreak();
    }

    private void IncreaseStreak()
    {
        if (currentStreak < maxMultiplier)
        {
            currentStreak++;
            UpdateDisplay();
            PlayStreakIncreaseEffect();
            OnStreakChanged?.Invoke(currentStreak);
        }
    }

    public void ResetStreak()
    {
        if (currentStreak > 1)
        {
            currentStreak = 1;
            UpdateDisplay();
            OnStreakReset?.Invoke();
        }
    }

    private void UpdateDisplay()
    {
        // Update multiplier text
        if (multiplierText != null)
        {
            multiplierText.text = $"x{currentStreak}";
        }

        // Update border color based on streak level
        if (borderImage != null && streakColors.Length > 0)
        {
            int colorIndex = Mathf.Min(currentStreak - 1, streakColors.Length - 1);
            borderImage.color = streakColors[colorIndex];
        }
    }

    private void PlayStreakIncreaseEffect()
    {
        // Safety check
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[StreakMultiplier] Don't play effect - GameObject is inactive");
            return;
        }

        // Stop any existing pulse
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        // Start new pulse effect
        pulseCoroutine = StartCoroutine(PulseEffect());
    }

    private IEnumerator PulseEffect()
    {
        Transform targetTransform = multiplierContainer != null ? multiplierContainer.transform : transform;

        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / pulseDuration;
            float pulseValue = pulseCurve.Evaluate(normalizedTime);
            float scale = 1f + (pulseValue * pulseIntensity);

            targetTransform.localScale = originalScale * scale;

            yield return null;
        }

        // Reset to original scale
        targetTransform.localScale = originalScale;
    }

    // Public getters
    public int CurrentStreak => currentStreak;
    public float GetXPMultiplier() => currentStreak * streakBonusPerLevel;
}