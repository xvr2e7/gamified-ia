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
            for (int i = 1; i <= 5; i++)
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

        // Show the sequence for this participant
        string[] sequences = new string[]
        {
            "CTRL → BASE → TIME → FEED → FULL",
            "BASE → TIME → FEED → FULL → CTRL",
            "TIME → FEED → FULL → CTRL → BASE",
            "FEED → FULL → CTRL → BASE → TIME",
            "FULL → CTRL → BASE → TIME → FEED"
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
        PlayerPrefs.Save();

        // Load experiment scene (Pilot Study for now)
        StartCoroutine(LoadSceneWithProperLighting());
    }

    IEnumerator LoadSceneWithProperLighting()
    {
        // Load the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Pilot", LoadSceneMode.Single);

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}