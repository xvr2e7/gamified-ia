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
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;

    private float currentTime;
    private bool isTimerActive = false;
    private bool isInUrgencyMode = false;
    private bool hasTimedOut = false;
    private MushroomCounter mushroomCounter;
    private AudioSource audioSource;
    private Coroutine timerCoroutine;

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

        mushroomCounter = FindObjectOfType<MushroomCounter>();
    }

    void Start()
    {
        InitializeVisuals();
    }

    void InitializeVisuals()
    {
        if (baseCircle != null)
            baseCircle.color = baseColor;

        if (progressRing != null)
        {
            progressRing.fillAmount = 0f;
            progressRing.color = healthyColor;
        }

        if (countdownText != null)
            countdownText.color = Color.white;

        if (urgencyOverlay != null)
            urgencyOverlay.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, 0f);

        if (sporeParticles != null)
            sporeParticles.Stop();
    }

    public void StartTimer()
    {
        // Safety check
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[QuestionTimer] Don't start timer - GameObject is inactive");
            return;
        }

        StopTimer(); // Stop any existing timer

        currentTime = questionTimeLimit;
        isTimerActive = true;
        isInUrgencyMode = false;
        hasTimedOut = false;

        // Reset visuals
        InitializeVisuals();

        // Start countdown
        timerCoroutine = StartCoroutine(CountdownCoroutine());

        OnTimerStart?.Invoke();
    }

    public void StopTimer()
    {
        isTimerActive = false;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        StopAllCoroutines();

        if (sporeParticles != null)
            sporeParticles.Stop();

        OnTimerStop?.Invoke();
    }

    private IEnumerator CountdownCoroutine()
    {
        while (currentTime > 0 && isTimerActive)
        {
            currentTime -= Time.deltaTime;

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

        if (isTimerActive)
        {
            TimeOut();
        }
    }

    private void UpdateProgressRing()
    {
        if (progressRing != null)
        {
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

        if (timeRatio > 0.5f)
        {
            float t = (1f - timeRatio) * 2f;
            currentColor = Color.Lerp(healthyColor, warningColor, t);
        }
        else if (timeRatio > 0.33f)
        {
            float t = (0.5f - timeRatio) * 3f;
            currentColor = Color.Lerp(warningColor, urgentColor, t);
        }
        else
        {
            float t = (0.33f - timeRatio) * 3f;
            currentColor = Color.Lerp(urgentColor, criticalColor, t);
        }

        if (progressRing != null)
            progressRing.color = currentColor;

        if (countdownText != null && currentTime <= criticalThreshold)
            countdownText.color = currentColor;
    }

    private void EnterUrgencyMode()
    {
        if (!gameObject.activeInHierarchy) return; // Safety check

        isInUrgencyMode = true;

        PlaySound(urgencySound);

        if (sporeParticles != null)
        {
            sporeParticles.Play();
        }

        StartCoroutine(UrgencyOverlayEffect());
        OnUrgencyMode?.Invoke();
    }

    private IEnumerator UrgencyOverlayEffect()
    {
        while (isInUrgencyMode && isTimerActive)
        {
            if (urgencyOverlay != null)
            {
                float pulse = Mathf.Sin(Time.time * 3f) * 0.1f;
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

        // Attempt to take damage and determine result
        bool damageWasTaken = false;
        bool hadMushroomsBeforeDamage = false;
        bool hasMushroomsAfterDamage = false;

        if (mushroomCounter != null)
        {
            hadMushroomsBeforeDamage = mushroomCounter.HasMushrooms();
            damageWasTaken = mushroomCounter.TakeDamage();
            hasMushroomsAfterDamage = mushroomCounter.HasMushrooms();
        }

        // Play appropriate sound based on outcome
        if (damageWasTaken)
        {
            if (hasMushroomsAfterDamage)
            {
                // Mushroom was lost but some remain - damage sound
                PlaySound(damageSound != null ? damageSound : timeoutSound);
            }
            else
            {
                // Last mushroom was lost - death sound
                PlaySound(deathSound != null ? deathSound : timeoutSound);
            }
        }
        else
        {
            // No mushrooms to lose - fallback to timeout sound
            PlaySound(timeoutSound);
        }

        // Start appropriate visual effect
        StartCoroutine(DamageOverlayEffect(damageWasTaken, hadMushroomsBeforeDamage && !hasMushroomsAfterDamage));
        OnTimeOut?.Invoke();
    }

    private IEnumerator DamageOverlayEffect(bool damageWasTaken, bool wasLastMushroom)
    {
        if (damageWasTaken)
        {
            if (wasLastMushroom)
            {
                // Last mushroom lost - more intense red overlay
                Color deathColor = new Color(0.9f, 0.05f, 0.05f, 0.7f);

                for (int i = 0; i < 3; i++)
                {
                    if (urgencyOverlay != null)
                        urgencyOverlay.color = deathColor;

                    yield return new WaitForSeconds(0.25f);

                    if (urgencyOverlay != null)
                        urgencyOverlay.color = new Color(deathColor.r, deathColor.g, deathColor.b, 0.1f);

                    yield return new WaitForSeconds(0.25f);
                }
            }
            else
            {
                // Regular damage - red overlay
                Color damageColor = new Color(0.8f, 0.1f, 0.1f, 0.6f);

                for (int i = 0; i < 2; i++)
                {
                    if (urgencyOverlay != null)
                        urgencyOverlay.color = damageColor;

                    yield return new WaitForSeconds(0.2f);

                    if (urgencyOverlay != null)
                        urgencyOverlay.color = new Color(damageColor.r, damageColor.g, damageColor.b, 0.1f);

                    yield return new WaitForSeconds(0.2f);
                }
            }
        }
        else
        {
            // Attempted damage but no mushrooms - orange warning
            Color warningColor = new Color(1f, 0.5f, 0f, 0.4f);

            for (int i = 0; i < 3; i++)
            {
                if (urgencyOverlay != null)
                    urgencyOverlay.color = warningColor;

                yield return new WaitForSeconds(0.1f);

                if (urgencyOverlay != null)
                    urgencyOverlay.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0.05f);

                yield return new WaitForSeconds(0.1f);
            }
        }

        // Standard timeout visual effects
        for (int i = 0; i < 3; i++)
        {
            if (progressRing != null)
                progressRing.color = Color.white;

            yield return new WaitForSeconds(0.15f);

            if (progressRing != null)
                progressRing.color = criticalColor;

            yield return new WaitForSeconds(0.15f);
        }

        if (countdownText != null)
        {
            countdownText.text = "TIME UP";
            countdownText.color = criticalColor;
        }

        if (progressRing != null)
            progressRing.color = criticalColor;

        if (urgencyOverlay != null)
            urgencyOverlay.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, 0f);
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