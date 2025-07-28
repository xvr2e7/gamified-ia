using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SetupManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown participantDropdown;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI sequenceText;

    private int selectedParticipantID = 1;

    void Start()
    {
        // Setup dropdown
        if (participantDropdown != null)
        {
            participantDropdown.ClearOptions();
            var options = new List<string>();
            for (int i = 1; i <= 6; i++)
            {
                options.Add($"Participant {i}");
            }
            participantDropdown.AddOptions(options);
            participantDropdown.onValueChanged.AddListener(OnParticipantChanged);
        }

        // Setup start button
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartExperiment);
        }

        // Show initial sequence
        OnParticipantChanged(0);
    }


    void OnParticipantChanged(int index)
    {
        selectedParticipantID = index + 1;

        string[] sequences = new string[]
        {
        "CTRL → BASE → BAFE → TIME → FEED → FULL",
        "BASE → BAFE → TIME → FEED → FULL → CTRL",
        "BAFE → TIME → FEED → FULL → CTRL → BASE",
        "TIME → FEED → FULL → CTRL → BASE → BAFE",
        "FEED → FULL → CTRL → BASE → BAFE → TIME",
        "FULL → CTRL → BASE → BAFE → TIME → FEED"
        };

        if (sequenceText != null)
        {
            sequenceText.text = $"{sequences[index]}";
        }
    }

    void StartExperiment()
    {
        // Save participant ID and reset condition counter
        PlayerPrefs.SetInt("ParticipantID", selectedParticipantID);
        PlayerPrefs.SetInt("CurrentCondition", 0);

        // Set flag to start in practice mode
        PlayerPrefs.SetInt("StartPractice", 1);
        PlayerPrefs.Save();

        // Load pilot scene (ExperimentManager will handle practice mode)
        StartCoroutine(LoadSceneWithProperLighting());
    }

    IEnumerator LoadSceneWithProperLighting()
    {
        // Load the pilot scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Pilot", LoadSceneMode.Single);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}