using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MushroomCounter : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI counterText;

    [Header("Mushroom Sprites")]
    [SerializeField] private Sprite emptyMushroomSprite;
    [SerializeField] private Sprite filledMushroomSprite;

    [Header("Effects")]
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private AudioClip collectSound;

    [Header("Animation Settings")]
    [SerializeField] private float bounceScale = 1.5f;
    [SerializeField] private float bounceDuration = 0.5f;

    private int mushroomCount = 0;
    private int damageCount = 0;
    private AudioSource audioSource;
    private Image mushroomIconImage;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        mushroomIconImage = GetComponent<Image>();
        if (mushroomIconImage == null)
        {
            Transform iconChild = transform.Find("MushroomIcon");
            if (iconChild != null)
            {
                mushroomIconImage = iconChild.GetComponent<Image>();
            }
        }

        UpdateDisplay();
    }

    public void UpdateMushroomCount(int newXP)
    {
        int baseCount = newXP / 25;
        int newDisplayedCount = Mathf.Max(0, baseCount - damageCount);

        if (baseCount > mushroomCount)
        {
            mushroomCount = baseCount;
            StartCoroutine(AnimateCollection());
        }
        else if (newDisplayedCount != GetDisplayedMushroomCount())
        {
            UpdateDisplay();
        }
    }

    public bool TakeDamage()
    {
        int currentDisplayed = GetDisplayedMushroomCount();

        if (currentDisplayed > 0)
        {
            damageCount++;
            UpdateDisplay();
            return true; // Damage was taken
        }

        return false; // No mushrooms to lose
    }

    public int GetMushroomCount()
    {
        return mushroomCount;
    }

    public int GetDisplayedMushroomCount()
    {
        return Mathf.Max(0, mushroomCount - damageCount);
    }

    public bool HasMushrooms()
    {
        return GetDisplayedMushroomCount() > 0;
    }

    private IEnumerator AnimateCollection()
    {
        // Update display first
        UpdateDisplay();

        // Play particle effect
        if (collectParticles != null)
        {
            collectParticles.Play();
        }

        // Play sound
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        // Bounce animation on the icon
        Transform iconTransform = mushroomIconImage != null ?
            mushroomIconImage.transform : transform;

        if (iconTransform != null)
        {
            Vector3 originalScale = iconTransform.localScale;
            float elapsed = 0;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceDuration;

                // Bounce curve
                float scale = 1f + (bounceScale - 1f) * Mathf.Sin(t * Mathf.PI);
                iconTransform.localScale = originalScale * scale;

                iconTransform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2) * 10f);

                yield return null;
            }

            iconTransform.localScale = originalScale;
            iconTransform.rotation = Quaternion.identity;
        }
    }

    private void UpdateDisplay()
    {
        int displayedCount = GetDisplayedMushroomCount();

        // Update mushroom icon sprite based on displayed count
        if (mushroomIconImage != null)
        {
            if (displayedCount == 0)
            {
                mushroomIconImage.sprite = emptyMushroomSprite;
            }
            else
            {
                mushroomIconImage.sprite = filledMushroomSprite;
            }
        }

        // Counter text
        if (counterText != null)
        {
            if (displayedCount == 0)
            {
                counterText.gameObject.SetActive(false);
            }
            else
            {
                counterText.gameObject.SetActive(true);
                counterText.text = $"x {displayedCount}";
            }
        }
    }
}