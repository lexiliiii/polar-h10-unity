using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ActivitySelectionManager : MonoBehaviour
{
    public static ActivitySelectionManager Instance { get; private set; }

    private string selectedSceneName;

    // Activity completion state
    private Dictionary<string, bool> activityCompletion = new Dictionary<string, bool>()
    {
        { "Carpentry", false },
        { "Construction_Labor", false },
        { "Drywall", false },
        { "Electrician", false },
        { "Exterior", false },
        { "Masonry", false }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void StartWalkthrough()
    {
        ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.ExteriorExploration);
        SceneManager.LoadScene("Exterior");
    }

    // Called when an activity button is clicked
    public bool SetSelectedActivity(string sceneName)
    {
        var stage = ProgressManager.Instance?.CurrentStage ?? ProgressManager.Stage.StartWalkthrough;

        bool valid = true;
        switch (stage)
        {
            case ProgressManager.Stage.RobotCarpentryActivitySelect:
                valid = (sceneName == "Carpentry");
                break;
            case ProgressManager.Stage.RobotConstructionLaborActivitySelect:
                valid = (sceneName == "Construction_Labor");
                break;
            case ProgressManager.Stage.RobotDrywallActivitySelect:
                valid = (sceneName == "Drywall");
                break;
            case ProgressManager.Stage.RobotElectricianActivitySelect:
                valid = (sceneName == "Electrician");
                break;
            case ProgressManager.Stage.RobotMasonryActivitySelect:
                valid = (sceneName == "Masonry");
                break;
        }

        selectedSceneName = sceneName;
        Debug.Log(valid
            ? "[SetSelectedActivity] Valid activity selected: " + sceneName
            : "[SetSelectedActivity] Invalid activity selected for this stage: " + sceneName);

        return valid;
    }

    // Called by the confirm button in the modal
    public void ConfirmActivity()
    {
        if (!string.IsNullOrEmpty(selectedSceneName))
        {
            /*var stage = ProgressManager.Instance.CurrentStage;

            if (!ProgressManager.Instance.HasCompletedActivity(selectedSceneName))
            {
                ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.Activity);
            }
            else if (stage == ProgressManager.Stage.RobotCarpentryActivitySelect && selectedSceneName == "Carpentry" ||
                     stage == ProgressManager.Stage.RobotConstructionLaborActivitySelect && selectedSceneName == "Construction_Labor" ||
                     stage == ProgressManager.Stage.RobotDrywallActivitySelect && selectedSceneName == "Drywall" ||
                     stage == ProgressManager.Stage.RobotElectricianActivitySelect && selectedSceneName == "Electrician" ||
                     stage == ProgressManager.Stage.RobotMasonryActivitySelect && selectedSceneName == "Masonry")
            {
                ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.RobotActivity);
            }

            SceneManager.LoadScene(selectedSceneName);*/
        }
        else
        {
            Debug.LogWarning("No activity selected before confirmation.");
        }
    }

    public void CompleteActivity()
    {
        string activityName = SceneManager.GetActiveScene().name;

        if (!activityCompletion.ContainsKey(activityName))
        {
            Debug.LogWarning("Unknown activity: " + activityName);
            return;
        }

        var stage = ProgressManager.Instance.CurrentStage;

        // WRL stage handling
        if (stage == ProgressManager.Stage.RobotActivity)
        {
            activityCompletion[activityName] = true;

            if (activityName == "Carpentry")
                ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.RobotConstructionLabor, force: true);
            else if (activityName == "Construction_Labor")
                ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.RobotDrywall, force: true);
            else if (activityName == "Drywall")
                ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.RobotElectrician, force: true);
            else if (activityName == "Electrician")
                ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.RobotMasonry, force: true);
            else if (activityName == "Masonry")
                ProgressManager.Instance.AdvanceToStage(ProgressManager.Stage.Finish);
            else
                Debug.LogWarning("Unrecognized activity in WRL stage: " + activityName);

            SceneManager.LoadScene("Start");
            return;
        }

        // Pre-WRL handling
        bool isWRLStage = stage >= ProgressManager.Stage.RobotCarpentry;
        bool isCorrectWRLActivity =
            (stage == ProgressManager.Stage.RobotCarpentryActivitySelect && activityName == "Carpentry") ||
            (stage == ProgressManager.Stage.RobotConstructionLaborActivitySelect && activityName == "Construction_Labor") ||
            (stage == ProgressManager.Stage.RobotDrywallActivitySelect && activityName == "Drywall") ||
            (stage == ProgressManager.Stage.RobotElectricianActivitySelect && activityName == "Electrician") ||
            (stage == ProgressManager.Stage.RobotMasonryActivitySelect && activityName == "Masonry");

        if (isWRLStage && !isCorrectWRLActivity)
        {
            Debug.LogWarning("[ActivitySelectionManager] Incorrect WRL activity completion attempted.");
        }
        else
        {
            activityCompletion[activityName] = true;
            Debug.Log(activityName + " marked as completed.");
            ProgressManager.Instance?.RegisterActivityCompleted(activityName);
        }

        SceneManager.LoadScene("Start");
    }

    public bool IsActivityCompleted(string activityName)
    {
        return activityCompletion.ContainsKey(activityName) && activityCompletion[activityName];
    }

    public int GetCompletedCount()
    {
        int count = 0;
        foreach (var entry in activityCompletion)
        {
            if (entry.Key != "Exterior" && entry.Value)
            {
                count++;
            }
        }
        return count;
    }

    public int GetTotalCount()
    {
        return activityCompletion.Count - 1;
    }

    public void ResetActivityCompletions()
    {
        var keys = new List<string>(activityCompletion.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != "Exterior") // Skip exterior
                activityCompletion[keys[i]] = false;
        }

        // Update all completion icons
        var icons = FindObjectsOfType<CompletionIconController>();
        foreach (var icon in icons)
        {
            icon.UpdateIcon();
        }
    }
}