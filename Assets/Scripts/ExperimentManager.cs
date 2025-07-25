using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class ExperimentManager : MonoBehaviour
{
    public static ExperimentManager Instance { get; private set; }

    public enum Condition { CTRL, BASE, TIME, FEED, FULL }

    [Header("UI Parents")]
    [SerializeField] private GameObject flatStimulusCanvas;
    [SerializeField] private GameObject HUD;
    [SerializeField] private GameObject environment;

    [Header("HUD Sub-Elements")]
    [SerializeField] private GameObject xpContainer;
    [SerializeField] private GameObject streakMultiplier;
    [SerializeField] private GameObject timeContainer;

    [Header("Controllers")]
    [SerializeField] private ImageViewerController imageViewer;
    [SerializeField] private QuestionDisplayManager questionManager;

    private readonly Condition[][] latinSquare = new Condition[][]
    {
        new[]{ Condition.CTRL, Condition.BASE, Condition.TIME, Condition.FEED, Condition.FULL },
        new[]{ Condition.BASE, Condition.TIME, Condition.FEED, Condition.FULL, Condition.CTRL },
        new[]{ Condition.TIME, Condition.FEED, Condition.FULL, Condition.CTRL, Condition.BASE },
        new[]{ Condition.FEED, Condition.FULL, Condition.CTRL, Condition.BASE, Condition.TIME },
        new[]{ Condition.FULL, Condition.CTRL, Condition.BASE, Condition.TIME, Condition.FEED }
    };

    private List<Condition> conditionSequence;
    private int participantID;
    private int currentConditionIndex;
    private Condition currentCondition;

    [Serializable]
    private class Trial
    {
        public string imageName;
        public int taskIndex;
        public string encodingType;
        public string taskLevel;
        public QuestionData metadata;
    }

    private List<Trial> currentTrials;
    private HashSet<string> usedKeys = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        participantID = PlayerPrefs.GetInt("ParticipantID", 1);
        currentConditionIndex = PlayerPrefs.GetInt("CurrentCondition", 0);
        conditionSequence = latinSquare[participantID - 1].ToList();
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Pilot")
            BeginCondition();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Pilot")
            BeginCondition();
    }

    private void BeginCondition()
    {
        if (currentConditionIndex >= conditionSequence.Count)
        {
            EndExperiment();
            return;
        }

        // Use coroutine to ensure proper initialization order
        StartCoroutine(InitializeCondition());
    }

    private System.Collections.IEnumerator InitializeCondition()
    {
        // Wait one frame to ensure scene is fully loaded
        yield return null;

        // Find and assign references
        ResolveReferences();

        currentCondition = conditionSequence[currentConditionIndex];

        // Apply condition settings FIRST
        ApplyConditionSettings(currentCondition);

        // Generate trials after settings are applied
        currentTrials = GenerateTrials();

        // Set the image pairs after condition settings are applied
        PrepareImageViewer();

        Debug.Log($"[ExperimentManager] Starting {currentCondition} block ({currentConditionIndex + 1}/{conditionSequence.Count}) with {currentTrials.Count} trials");
    }

    private void ResolveReferences()
    {
        // Try to find references if not already assigned
        if (imageViewer == null) imageViewer = FindObjectOfType<ImageViewerController>();
        if (questionManager == null) questionManager = FindObjectOfType<QuestionDisplayManager>();

        // Find GameObjects by name
        if (flatStimulusCanvas == null) flatStimulusCanvas = GameObject.Find("FlatStimulusCanvas");
        if (HUD == null) HUD = GameObject.Find("HUD");
        if (environment == null) environment = GameObject.Find("Environment");

        // Find HUD sub-elements
        if (HUD != null)
        {
            Transform hudTransform = HUD.transform;
            if (xpContainer == null)
            {
                Transform xpTransform = hudTransform.Find("XPContainer");
                if (xpTransform != null) xpContainer = xpTransform.gameObject;
            }
            if (streakMultiplier == null)
            {
                Transform streakTransform = hudTransform.Find("StreakMultiplier");
                if (streakTransform != null) streakMultiplier = streakTransform.gameObject;
            }
            if (timeContainer == null)
            {
                Transform timeTransform = hudTransform.Find("TimeContainer");
                if (timeTransform != null) timeContainer = timeTransform.gameObject;
            }
        }
    }

    private void ApplyConditionSettings(Condition cond)
    {
        // Ensure all HUD sub-elements are inactive BEFORE deactivating parent
        if (xpContainer != null) xpContainer.SetActive(false);
        if (streakMultiplier != null) streakMultiplier.SetActive(false);
        if (timeContainer != null) timeContainer.SetActive(false);

        // Reset all parents to inactive
        if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(false);
        if (HUD != null) HUD.SetActive(false);
        if (environment != null) environment.SetActive(false);

        // Apply condition-specific settings
        switch (cond)
        {
            case Condition.CTRL:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                // No HUD, no environment
                break;

            case Condition.BASE:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(true);
                // No HUD
                break;

            case Condition.TIME:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(true);
                if (HUD != null) HUD.SetActive(true);
                if (xpContainer != null) xpContainer.SetActive(true);
                if (timeContainer != null) timeContainer.SetActive(true);
                // streakMultiplier stays inactive
                break;

            case Condition.FEED:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(true);
                if (HUD != null) HUD.SetActive(true);
                if (xpContainer != null) xpContainer.SetActive(true);
                // timeContainer and streakMultiplier stay inactive
                break;

            case Condition.FULL:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(true);
                if (HUD != null) HUD.SetActive(true);
                if (xpContainer != null) xpContainer.SetActive(true);
                if (streakMultiplier != null) streakMultiplier.SetActive(true);
                if (timeContainer != null) timeContainer.SetActive(true);
                break;
        }
    }

    private List<Trial> GenerateTrials()
    {
        var allTrials = new List<Trial>();
        string metaPath = Path.Combine(Application.streamingAssetsPath, "Metadata");

        // Load all metadata files
        foreach (var file in Directory.GetFiles(metaPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var data = JsonUtility.FromJson<QuestionData>(json);
                if (data == null) continue;

                string encodingType = data.encoding.ToLower();

                // Create trials for each task in the metadata
                for (int i = 0; i < data.tasks.Length; i++)
                {
                    string key = $"{data.file}_{i}";

                    // Skip if already used in previous conditions
                    if (usedKeys.Contains(key)) continue;

                    // Use the task type from metadata directly
                    string taskLevel = data.tasks[i].type.ToLower(); // "low" or "high"

                    allTrials.Add(new Trial
                    {
                        imageName = data.file,
                        taskIndex = i,
                        encodingType = encodingType,
                        taskLevel = taskLevel,
                        metadata = data
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse JSON file {file}: {e.Message}");
            }
        }

        // Select trials for this condition: 3 low + 3 high for each encoding type
        var selectedTrials = new List<Trial>();

        foreach (var encoding in new[] { "bar", "pie", "stack" })
        {
            var encodingTrials = allTrials.Where(t => t.encodingType == encoding).ToList();
            var lowTrials = encodingTrials.Where(t => t.taskLevel == "low").ToList();
            var highTrials = encodingTrials.Where(t => t.taskLevel == "high").ToList();

            // Select 3 low-level tasks
            selectedTrials.AddRange(SelectRandom(lowTrials, 3));

            // Select 3 high-level tasks
            selectedTrials.AddRange(SelectRandom(highTrials, 3));
        }

        // Mark selected trials as used
        foreach (var trial in selectedTrials)
        {
            usedKeys.Add($"{trial.imageName}_{trial.taskIndex}");
        }

        // Shuffle the final selection
        Shuffle(selectedTrials);

        Debug.Log($"[ExperimentManager] Generated {selectedTrials.Count} trials for condition {currentCondition}");
        return selectedTrials;
    }

    private List<Trial> SelectRandom(List<Trial> source, int count)
    {
        var temp = new List<Trial>(source);
        var result = new List<Trial>();

        int actualCount = Math.Min(count, temp.Count);

        for (int i = 0; i < actualCount; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }

        if (result.Count < count)
        {
            Debug.LogWarning($"[ExperimentManager] Could only select {result.Count} trials, requested {count}");
        }

        return result;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void PrepareImageViewer()
    {
        if (imageViewer == null)
        {
            Debug.LogError("[ExperimentManager] ImageViewerController not found!");
            return;
        }

        // Create the list of image-question pairs
        var pairs = new List<ImageViewerController.ImageQuestionPair>();

        foreach (var trial in currentTrials)
        {
            // Load texture
            string imagePath = Path.Combine(Application.streamingAssetsPath, "Stimulus", trial.imageName);

            if (!File.Exists(imagePath))
            {
                Debug.LogError($"[ExperimentManager] Image file not found: {imagePath}");
                continue;
            }

            byte[] imageData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);

            if (!texture.LoadImage(imageData))
            {
                Debug.LogError($"[ExperimentManager] Failed to load image: {imagePath}");
                continue;
            }

            texture.name = Path.GetFileNameWithoutExtension(trial.imageName);

            // Create the pair
            pairs.Add(new ImageViewerController.ImageQuestionPair
            {
                texture = texture,
                metadata = trial.metadata,
                taskIndex = trial.taskIndex,
                originalFileName = trial.imageName
            });
        }

        // Provide these pairs to the ImageViewerController
        imageViewer.SetImageQuestionPairs(pairs);

        Debug.Log($"[ExperimentManager] Prepared {pairs.Count} image-question pairs for ImageViewerController");
    }

    public void OnBlockComplete()
    {
        // Called when all trials in current condition are complete
        currentConditionIndex++;
        PlayerPrefs.SetInt("CurrentCondition", currentConditionIndex);
        PlayerPrefs.Save();

        if (currentConditionIndex >= conditionSequence.Count)
        {
            EndExperiment();
        }
        else
        {
            // Load break scene
            SceneManager.LoadScene("Break");
        }
    }

    private void EndExperiment()
    {
        Debug.Log("[ExperimentManager] Experiment completed!");
        PlayerPrefs.SetInt("CurrentCondition", 0); // Reset for next participant
        PlayerPrefs.Save();

        SceneManager.LoadScene("Setup");
    }

    public Condition GetCurrentCondition() => currentCondition;

    public int GetParticipantID() => participantID;

    public bool IsComponentActiveForCondition(string componentName)
    {
        switch (currentCondition)
        {
            case Condition.CTRL:
            case Condition.BASE:
                return false;

            case Condition.TIME:
                return componentName == "XPManager" || componentName == "QuestionTimer";

            case Condition.FEED:
                return componentName == "XPManager";

            case Condition.FULL:
                return true;

            default:
                return false;
        }
    }
}