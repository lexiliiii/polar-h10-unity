using System;
using System.Collections.Generic;
using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    public event Action<string> OnStageChanged;

    public enum Stage
    {
        StartWalkthrough,
        ExteriorExploration,
        ActivitySelect,
        Activity,
        RobotCarpentry,
        RobotCarpentryActivitySelect,
        RobotConstructionLabor,
        RobotConstructionLaborActivitySelect,
        RobotDrywall,
        RobotDrywallActivitySelect,
        RobotElectrician,
        RobotElectricianActivitySelect,
        RobotMasonry,
        RobotMasonryActivitySelect,
        RobotActivity,
        Finish
    }

    private static readonly Dictionary<Stage, int> stageIndices = new Dictionary<Stage, int>
    {
        { Stage.StartWalkthrough, 0 },
        { Stage.ExteriorExploration, 1 },
        { Stage.ActivitySelect, 2 },
        { Stage.Activity, 3 },
        { Stage.RobotCarpentry, 4 },
        { Stage.RobotCarpentryActivitySelect, 5 },
        { Stage.RobotConstructionLabor, 6 },
        { Stage.RobotConstructionLaborActivitySelect, 7 },
        { Stage.RobotDrywall, 8 },
        { Stage.RobotDrywallActivitySelect, 9 },
        { Stage.RobotElectrician, 10 },
        { Stage.RobotElectricianActivitySelect, 11 },
        { Stage.RobotMasonry, 12 },
        { Stage.RobotMasonryActivitySelect, 13 },
        { Stage.RobotActivity, 14 },
        { Stage.Finish, 15 }
    };

    [SerializeField]
    private Stage currentStage = Stage.StartWalkthrough;
    public Stage CurrentStage => currentStage;

    private int completedActivitiesCount = 0;
    private HashSet<string> completedActivityScenes = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AdvanceToStage(Stage nextStage, bool force = false)
    {
        int currentIndex = stageIndices[currentStage];
        int nextIndex = stageIndices[nextStage];

        if (!force && nextIndex < currentIndex)
        {
            Debug.LogWarning($"[ProgressManager] Cannot regress from {currentStage} to {nextStage}");
            return;
        }

        // Reset activity completions at the start of the WRL section
        if (nextStage == Stage.RobotCarpentry && currentStage != Stage.RobotCarpentry)
        {
            ActivitySelectionManager.Instance?.ResetActivityCompletions();
            Debug.Log("[ProgressManager] Activity completions reset for WRL phase.");
        }

        currentStage = nextStage;
        Debug.Log($"[ProgressManager] Advanced to: {currentStage}");
        OnStageChanged?.Invoke(currentStage.ToString());
    }

    public void RegisterActivityCompleted(string sceneName)
    {
        if (sceneName == "Exterior")
        {
            // Prevent regress if already past ActivitySelection
            if (currentStage == Stage.ExteriorExploration)
            {
                AdvanceToStage(Stage.ActivitySelect, force: true);
            }
            else
            {
                Debug.Log($"[ProgressManager] Ignored repeat Exterior completion at stage: {currentStage}");
            }
            return;
        }

        if (completedActivityScenes.Contains(sceneName))
        {
            Debug.Log($"[ProgressManager] Activity '{sceneName}' already completed.");

            // Only redirect back to ActivitySelection if we're still in pre-robot stages
            if (stageIndices[currentStage] < stageIndices[Stage.RobotCarpentry])
            {
                AdvanceToStage(Stage.ActivitySelect, force: true);
            }
            else
            {
                Debug.Log($"[ProgressManager] Ignored fallback to ActivitySelection at stage: {currentStage}");
            }
            return;
        }

        completedActivityScenes.Add(sceneName);
        completedActivitiesCount++;
        Debug.Log($"[ProgressManager] Completed activity: {sceneName} ({completedActivitiesCount}/5)");

        if (completedActivitiesCount >= 5)
            AdvanceToStage(Stage.RobotCarpentry);
        else
            AdvanceToStage(Stage.ActivitySelect, force: true);
    }

    public bool HasCompletedActivity(string sceneName)
    {
        return completedActivityScenes.Contains(sceneName);
    }
}