using UnityEngine;
using TMPro;
using System.Collections;

public class MushroomCounter : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private Transform mushroomIcon;

    [Header("Effects")]
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private AudioClip collectSound;

    [Header("Animation Settings")]
    [SerializeField] private float bounceScale = 1.5f;
    [SerializeField] private float bounceDuration = 0.5f;

    private int mushroomCount = 0;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        UpdateDisplay();
    }

    public void UpdateMushroomCount(int newXP)
    {
        int newCount = newXP / 25;

        if (newCount > mushroomCount)
        {
            mushroomCount = newCount;
            StartCoroutine(AnimateCollection());
        }
    }

    private IEnumerator AnimateCollection()
    {
        // Update text
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
        if (mushroomIcon != null)
        {
            Vector3 originalScale = mushroomIcon.localScale;
            float elapsed = 0;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceDuration;

                // Bounce curve
                float scale = 1f + (bounceScale - 1f) * Mathf.Sin(t * Mathf.PI);
                mushroomIcon.localScale = originalScale * scale;

                mushroomIcon.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2) * 10f);

                yield return null;
            }

            mushroomIcon.localScale = originalScale;
            mushroomIcon.rotation = Quaternion.identity;
        }
    }

    private void UpdateDisplay()
    {
        if (counterText != null)
        {
            counterText.text = $"x {mushroomCount}";
        }
    }

    public int GetMushroomCount()
    {
        return mushroomCount;
    }
}