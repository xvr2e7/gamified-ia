using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class BreakManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button continueButton;

    [Header("Break Settings")]
    [SerializeField] private float minimumBreakTime = 10f;
    [SerializeField] private float recommendedBreakTime = 120f;

    private int currentCondition;
    private int participantID;
    private float breakTimer = 0f;

    void Start()
    {
        // Get current progress
        participantID = PlayerPrefs.GetInt("ParticipantID", 1);
        currentCondition = PlayerPrefs.GetInt("CurrentCondition", 0);

        // Check if experiment is complete
        if (currentCondition >= 5)
        {
            ShowExperimentComplete();
            return;
        }

        // Show break info
        ShowBreakInfo();

        // Setup continue button
        if (continueButton != null)
        {
            continueButton.interactable = false;
            continueButton.onClick.AddListener(ContinueExperiment);
        }

        // Start break timer
        StartCoroutine(BreakTimer());
    }

    void ShowBreakInfo()
    {
        string[] conditionNames = { "CTRL", "BASE", "TIME", "FEED", "FULL" };
        int total = conditionNames.Length;

        statusText.text += "Please take a short break.\n\n";

        // Next condition: valid only while currentCondition < total
        string next = currentCondition < total
            ? conditionNames[currentCondition]
            : "Complete";
        statusText.text += $"Next condition: {next}";
    }


    void ShowExperimentComplete()
    {
        if (statusText != null)
        {
            statusText.text = "Experiment Complete!";
        }

        if (timerText != null)
        {
            timerText.text = "";
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    IEnumerator BreakTimer()
    {
        while (breakTimer < recommendedBreakTime)
        {
            breakTimer += Time.deltaTime;

            // Update timer display
            if (timerText != null)
            {
                int secondsRemaining = Mathf.CeilToInt(recommendedBreakTime - breakTimer);
                timerText.text = $"Break time: {secondsRemaining}s";
            }

            // Enable continue button after minimum time
            if (breakTimer >= minimumBreakTime && continueButton != null)
            {
                continueButton.interactable = true;
            }

            yield return null;
        }

        if (timerText != null)
        {
            timerText.text = "Click Continue to proceed.";
        }
    }

    void ContinueExperiment()
    {
        // Return to experiment scene
        SceneManager.LoadScene("Pilot");
    }
}