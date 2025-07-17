using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuestionTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float questionTimeLimit = 6f;
    [SerializeField] private float urgencyThreshold = 3f;
    [SerializeField] private float criticalThreshold = 2f;

    [Header("Visual Elements")]
    [SerializeField] private GameObject timerContainer;
    [SerializeField] private Image baseCircle;
    [SerializeField] private Image progressRing;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image urgencyOverlay;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem sporeParticles;

    [Header("Color Progression")]
    [SerializeField] private Color baseColor = new Color(0.15f, 0.12f, 0.08f, 0.9f);
    [SerializeField] private Color healthyColor = new Color(0.3f, 0.7f, 0.2f, 1f);
    [SerializeField] private Color warningColor = new Color(0.8f, 0.6f, 0.1f, 1f);
    [SerializeField] private Color urgentColor = new Color(0.9f, 0.4f, 0.1f, 1f);
    [SerializeField] private Color criticalColor = new Color(0.8f, 0.1f, 0.1f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioClip urgencySound;
    [SerializeField] private AudioClip timeoutSound;
    [SerializeField] private AudioClip tickSound;

    [Header("Animation")]
    [SerializeField] private float pulseIntensity = 0.1f;
    [SerializeField] private float pulseSpeed = 3f;

    private float currentTime;
    private bool isTimerActive = false;
    private bool isInUrgencyMode = false;
    private bool hasTimedOut = false;
    private AudioSource audioSource;
    private Coroutine timerCoroutine;
    private Coroutine pulseCoroutine;
    private Vector3 originalScale;

    // Events
    public System.Action OnTimerStart;
    public System.Action OnUrgencyMode;
    public System.Action OnTimeOut;
    public System.Action OnTimerStop;

    private static QuestionTimer instance;
    public static QuestionTimer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<QuestionTimer>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Store original scale
        if (timerContainer != null)
            originalScale = timerContainer.transform.localScale;
    }

    void Start()
    {
        InitializeTimer();
    }

    void InitializeTimer()
    {
        // Hide timer initially
        if (timerContainer != null)
            timerContainer.SetActive(false);

        // Initialize base circle
        if (baseCircle != null)
        {
            baseCircle.color = baseColor;
        }

        // Initialize progress ring
        if (progressRing != null)
        {
            progressRing.fillAmount = 0f;
            progressRing.color = healthyColor;
        }

        // Initialize text
        if (countdownText != null)
        {
            countdownText.color = Color.white;
        }

        // Initialize overlay
        if (urgencyOverlay != null)
        {
            urgencyOverlay.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, 0f);
        }

        // Stop particles
        if (sporeParticles != null)
            sporeParticles.Stop();
    }

    public void StartTimer()
    {
        if (isTimerActive)
            StopTimer();

        currentTime = questionTimeLimit;
        isTimerActive = true;
        hasTimedOut = false;
        isInUrgencyMode = false;

        // Show timer with slide-in effect
        if (timerContainer != null)
        {
            timerContainer.SetActive(true);
            StartCoroutine(SlideInTimer());
        }

        // Start timer coroutine
        timerCoroutine = StartCoroutine(CountdownCoroutine());

        OnTimerStart?.Invoke();
    }

    public void StopTimer()
    {
        isTimerActive = false;

        // Stop coroutines
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Stop any ongoing slide animations
        StopAllCoroutines();

        // Immediately hide timer instead of sliding out
        if (timerContainer != null)
        {
            timerContainer.SetActive(false);
            timerContainer.transform.localScale = originalScale; // Reset scale
        }

        // Stop particles
        if (sporeParticles != null)
            sporeParticles.Stop();

        OnTimerStop?.Invoke();
    }

    private IEnumerator CountdownCoroutine()
    {
        while (currentTime > 0 && isTimerActive)
        {
            currentTime -= Time.deltaTime;

            // Update visuals
            UpdateProgressRing();
            UpdateCountdownText();
            UpdateColors();

            // Check for urgency mode
            if (!isInUrgencyMode && currentTime <= urgencyThreshold)
            {
                EnterUrgencyMode();
            }

            // Critical phase tick sounds
            if (currentTime <= criticalThreshold && currentTime > 0)
            {
                if (Mathf.FloorToInt(currentTime) != Mathf.FloorToInt(currentTime + Time.deltaTime))
                {
                    PlaySound(tickSound);
                }
            }

            yield return null;
        }

        // Time's up
        if (isTimerActive)
        {
            TimeOut();
        }
    }

    private void UpdateProgressRing()
    {
        if (progressRing != null)
        {
            // Fill amount increases as time decreases (contamination spreads)
            float progress = 1f - (currentTime / questionTimeLimit);
            progressRing.fillAmount = progress;
        }
    }

    private void UpdateCountdownText()
    {
        if (countdownText != null)
        {
            int seconds = Mathf.CeilToInt(currentTime);
            countdownText.text = seconds.ToString();
        }
    }

    private void UpdateColors()
    {
        float timeRatio = currentTime / questionTimeLimit;
        Color currentColor;

        if (timeRatio > 0.5f) // First half: healthy to warning
        {
            float t = (1f - timeRatio) * 2f; // 0 to 1 over first half
            currentColor = Color.Lerp(healthyColor, warningColor, t);
        }
        else if (timeRatio > 0.33f) // Second third: warning to urgent
        {
            float t = (0.5f - timeRatio) * 3f; // 0 to 1 over second third
            currentColor = Color.Lerp(warningColor, urgentColor, t);
        }
        else // Final third: urgent to critical
        {
            float t = (0.33f - timeRatio) * 3f; // 0 to 1 over final third
            currentColor = Color.Lerp(urgentColor, criticalColor, t);
        }

        // Update ring color
        if (progressRing != null)
        {
            progressRing.color = currentColor;
        }

        // Update text color in critical phase
        if (countdownText != null && currentTime <= criticalThreshold)
        {
            countdownText.color = currentColor;
        }
    }

    private void EnterUrgencyMode()
    {
        isInUrgencyMode = true;

        // Play urgency sound
        PlaySound(urgencySound);

        // Start pulsing animation
        pulseCoroutine = StartCoroutine(PulseAnimation());

        // Start spore particles
        if (sporeParticles != null)
        {
            sporeParticles.Play();
        }

        // Start urgency overlay
        StartCoroutine(UrgencyOverlayEffect());

        OnUrgencyMode?.Invoke();
    }

    private IEnumerator PulseAnimation()
    {
        while (isInUrgencyMode && isTimerActive)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            float scale = 1f + pulse;

            if (timerContainer != null)
            {
                timerContainer.transform.localScale = originalScale * scale;
            }

            yield return null;
        }

        // Reset scale
        if (timerContainer != null)
        {
            timerContainer.transform.localScale = originalScale;
        }
    }

    private IEnumerator UrgencyOverlayEffect()
    {
        while (isInUrgencyMode && isTimerActive)
        {
            if (urgencyOverlay != null)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.1f;
                float alpha = Mathf.Clamp01(pulse + 0.05f);
                urgencyOverlay.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, alpha);
            }

            yield return null;
        }
    }

    private void TimeOut()
    {
        hasTimedOut = true;
        isTimerActive = false;

        // Play timeout sound
        PlaySound(timeoutSound);

        // Show timeout state
        StartCoroutine(TimeoutEffect());

        OnTimeOut?.Invoke();
    }

    private IEnumerator TimeoutEffect()
    {
        // Flash effect
        for (int i = 0; i < 3; i++)
        {
            if (progressRing != null)
                progressRing.color = Color.white;
            if (urgencyOverlay != null)
                urgencyOverlay.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, 0.4f);

            yield return new WaitForSeconds(0.15f);

            if (progressRing != null)
                progressRing.color = criticalColor;
            if (urgencyOverlay != null)
                urgencyOverlay.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, 0.1f);

            yield return new WaitForSeconds(0.15f);
        }

        // Final timeout state
        if (countdownText != null)
        {
            countdownText.text = "TIME UP";
            countdownText.color = criticalColor;
        }

        if (progressRing != null)
        {
            progressRing.color = criticalColor;
        }
    }

    private IEnumerator SlideInTimer()
    {
        Vector3 startPos = timerContainer.transform.localPosition + Vector3.up * 100f;
        Vector3 endPos = timerContainer.transform.localPosition;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth animation curve

            timerContainer.transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        timerContainer.transform.localPosition = endPos;
    }

    private IEnumerator SlideOutTimer()
    {
        Vector3 startPos = timerContainer.transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 100f;

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            timerContainer.transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        timerContainer.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Public getters
    public bool IsTimerActive => isTimerActive;
    public bool HasTimedOut => hasTimedOut;
    public float RemainingTime => currentTime;
    public float TimePercentage => currentTime / questionTimeLimit;
    public bool IsInUrgencyMode => isInUrgencyMode;

    // Configuration methods
    public void SetTimeLimit(float timeLimit)
    {
        questionTimeLimit = timeLimit;
    }

    public void SetUrgencyThreshold(float threshold)
    {
        urgencyThreshold = threshold;
    }
}