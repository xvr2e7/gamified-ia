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

    public enum Condition { PRACTICE, CTRL, BASE, BAFE, TIME, FEED, FULL }

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
        new[]{ Condition.CTRL, Condition.BASE, Condition.BAFE, Condition.TIME, Condition.FEED, Condition.FULL },
        new[]{ Condition.BASE, Condition.BAFE, Condition.TIME, Condition.FEED, Condition.FULL, Condition.CTRL },
        new[]{ Condition.BAFE, Condition.TIME, Condition.FEED, Condition.FULL, Condition.CTRL, Condition.BASE },
        new[]{ Condition.TIME, Condition.FEED, Condition.FULL, Condition.CTRL, Condition.BASE, Condition.BAFE },
        new[]{ Condition.FEED, Condition.FULL, Condition.CTRL, Condition.BASE, Condition.BAFE, Condition.TIME },
        new[]{ Condition.FULL, Condition.CTRL, Condition.BASE, Condition.BAFE, Condition.TIME, Condition.FEED }
    };

    private List<Condition> conditionSequence;
    private int participantID;
    private int currentConditionIndex;
    private Condition currentCondition;
    private bool isPracticeMode = false;

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

        // Check if we should start in practice mode
        isPracticeMode = PlayerPrefs.GetInt("StartPractice", 0) == 1;

        if (isPracticeMode)
        {
            // Clear the practice flag
            PlayerPrefs.SetInt("StartPractice", 0);
            PlayerPrefs.Save();
            currentCondition = Condition.PRACTICE;
            currentConditionIndex = -1; // Special index for practice
        }
        else
        {
            participantID = PlayerPrefs.GetInt("ParticipantID", 1);
            currentConditionIndex = PlayerPrefs.GetInt("CurrentCondition", 0);
            conditionSequence = latinSquare[participantID - 1].ToList();
        }
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
        if (!isPracticeMode && currentConditionIndex >= conditionSequence.Count)
        {
            EndExperiment();
            return;
        }

        StartCoroutine(InitializeCondition());
    }

    private System.Collections.IEnumerator InitializeCondition()
    {
        yield return null;

        ResolveReferences();

        if (!isPracticeMode)
        {
            currentCondition = conditionSequence[currentConditionIndex];
        }

        ApplyConditionSettings(currentCondition);
        currentTrials = GenerateTrials();
        PrepareImageViewer();

        string conditionName = isPracticeMode ? "PRACTICE" : currentCondition.ToString();
        Debug.Log($"[ExperimentManager] Starting {conditionName} block with {currentTrials.Count} trials");
    }

    private void ResolveReferences()
    {
        if (imageViewer == null) imageViewer = FindObjectOfType<ImageViewerController>();
        if (questionManager == null) questionManager = FindObjectOfType<QuestionDisplayManager>();

        if (flatStimulusCanvas == null) flatStimulusCanvas = GameObject.Find("FlatStimulusCanvas");
        if (HUD == null) HUD = GameObject.Find("HUD");
        if (environment == null) environment = GameObject.Find("Environment");

        if (HUD != null)
        {
            Transform hudTransform = HUD.transform;
            if (xpContainer == null) xpContainer = hudTransform.Find("XPContainer")?.gameObject;
            if (streakMultiplier == null) streakMultiplier = hudTransform.Find("StreakMultiplier")?.gameObject;
            if (timeContainer == null) timeContainer = hudTransform.Find("TimeContainer")?.gameObject;
        }
    }

    private void ApplyConditionSettings(Condition condition)
    {
        switch (condition)
        {
            case Condition.PRACTICE:
                // Practice mode - similar to CTRL but with feedback enabled
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(false);
                if (HUD != null) HUD.SetActive(false);

                // Enable feedback mode in question manager
                if (questionManager != null)
                    questionManager.SetPracticeMode(true);
                break;

            case Condition.CTRL:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(false);
                if (HUD != null) HUD.SetActive(false);
                break;

            case Condition.BASE:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(true);
                if (HUD != null) HUD.SetActive(false);
                break;

            case Condition.BAFE:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(true);
                if (HUD != null) HUD.SetActive(false);

                // Enable simple feedback mode
                if (questionManager != null)
                    questionManager.SetSimpleFeedbackMode(true);
                break;

            case Condition.TIME:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(true);
                if (HUD != null) HUD.SetActive(true);
                if (xpContainer != null) xpContainer.SetActive(false);
                if (streakMultiplier != null) streakMultiplier.SetActive(false);
                if (timeContainer != null) timeContainer.SetActive(true);
                break;

            case Condition.FEED:
                if (flatStimulusCanvas != null) flatStimulusCanvas.SetActive(true);
                if (environment != null) environment.SetActive(true);
                if (HUD != null) HUD.SetActive(true);
                if (xpContainer != null) xpContainer.SetActive(true);
                if (streakMultiplier != null) streakMultiplier.SetActive(false);
                if (timeContainer != null) timeContainer.SetActive(false);
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

        // Determine which folder to read from
        string folderName = isPracticeMode ? "Practice" : "Metadata";
        string metaPath = Path.Combine(Application.streamingAssetsPath, folderName);

        // For practice mode, only load the specific file
        if (isPracticeMode)
        {
            string practiceFile = Path.Combine(metaPath, "Metadata", "Item_vs_weight_stacked.json");
            if (File.Exists(practiceFile))
            {
                LoadTrialsFromFile(practiceFile, allTrials);
            }
            else
            {
                Debug.LogError($"[ExperimentManager] Practice file not found: {practiceFile}");
            }
        }
        else
        {
            // Regular mode - load all files
            foreach (var file in Directory.GetFiles(metaPath, "*.json"))
            {
                LoadTrialsFromFile(file, allTrials);
            }
        }

        // For practice mode, shuffle all trials and return
        if (isPracticeMode)
        {
            ShuffleList(allTrials);
            return allTrials;
        }

        // Regular experiment logic - Select 3 low + 3 high for each encoding type
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
        ShuffleList(selectedTrials);

        Debug.Log($"[ExperimentManager] Generated {selectedTrials.Count} trials for condition {currentCondition}");
        return selectedTrials;
    }

    private void LoadTrialsFromFile(string file, List<Trial> allTrials)
    {
        try
        {
            var json = File.ReadAllText(file);
            var data = JsonUtility.FromJson<QuestionData>(json);
            if (data == null) return;

            string encodingType = data.encoding.ToLower();

            for (int i = 0; i < data.tasks.Length; i++)
            {
                string key = $"{data.file}_{i}";

                // Skip if already used (except in practice mode)
                if (!isPracticeMode && usedKeys.Contains(key)) continue;

                string taskLevel = data.tasks[i].type.ToLower();

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
            Debug.LogError($"[ExperimentManager] Failed to load trial file {file}: {e.Message}");
        }
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

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void PrepareImageViewer()
    {
        if (imageViewer == null)
        {
            Debug.LogError("[ExperimentManager] ImageViewerController not found!");
            return;
        }

        var pairs = new List<ImageViewerController.ImageQuestionPair>();
        string imageFolder = isPracticeMode ? "Practice/Stimulus" : "Stimulus";

        foreach (var trial in currentTrials)
        {
            string imagePath = Path.Combine(Application.streamingAssetsPath, imageFolder, trial.imageName);

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

            pairs.Add(new ImageViewerController.ImageQuestionPair
            {
                texture = texture,
                metadata = trial.metadata,
                taskIndex = trial.taskIndex,
                originalFileName = trial.imageName
            });
        }

        imageViewer.SetImageQuestionPairs(pairs);
        Debug.Log($"[ExperimentManager] Prepared {pairs.Count} image-question pairs");
    }

    private void EndExperiment()
    {
        Debug.Log("[ExperimentManager] Experiment completed!");
        PlayerPrefs.SetInt("CurrentCondition", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Setup");
    }

    public void OnBlockComplete()
    {
        if (isPracticeMode)
        {
            // Practice complete, now start the real experiment
            isPracticeMode = false;
            participantID = PlayerPrefs.GetInt("ParticipantID", 1);
            currentConditionIndex = 0;
            conditionSequence = latinSquare[participantID - 1].ToList();
            PlayerPrefs.SetInt("CurrentCondition", 0);
            PlayerPrefs.Save();

            // Reload scene to start first condition
            SceneManager.LoadScene("Pilot");
            return;
        }

        // Regular block complete logic
        currentConditionIndex++;
        PlayerPrefs.SetInt("CurrentCondition", currentConditionIndex);
        PlayerPrefs.Save();

        if (currentConditionIndex >= conditionSequence.Count)
        {
            EndExperiment();
        }
        else
        {
            SceneManager.LoadScene("Break");
        }
    }

    public Condition GetCurrentCondition() => currentCondition;
    public int GetParticipantID() => participantID;
    public bool IsPracticeMode() => isPracticeMode;

    public bool IsComponentActiveForCondition(string componentName)
    {
        if (currentCondition == Condition.PRACTICE)
            return false;

        switch (currentCondition)
        {
            case Condition.CTRL:
            case Condition.BASE:
            case Condition.BAFE:
                return false;

            case Condition.TIME:
                return componentName == "QuestionTimer";

            case Condition.FEED:
                return componentName == "XPManager";

            case Condition.FULL:
                return true;

            default:
                return false;
        }
    }
}