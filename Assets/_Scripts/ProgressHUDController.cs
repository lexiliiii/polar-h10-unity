using UnityEngine;
using TMPro;

public class ProgressHUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI uiText;

    private void Start()
    {
        if (uiText == null)
        {
            Debug.LogError("ProgressHUDController: TextMeshProUGUI reference not set.");
            return;
        }

        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnStageChanged += OnStageChanged;
            OnStageChanged(ProgressManager.Instance.CurrentStage.ToString());
        }
    }

    private void OnStageChanged(string stageName)
    {
        if (!System.Enum.TryParse(stageName, out ProgressManager.Stage stage))
        {
            uiText.text = "";
            return;
        }

        // Play Quest Item animation if found
        ReplayQuestItemAnimation();

        switch (stage)
        {
            case ProgressManager.Stage.StartWalkthrough:
                uiText.text = "Welcome to ViRLE! When you are ready, please select the \"start walkthrough\" button to visit the virtual construction site.";
                break;
            case ProgressManager.Stage.ExteriorExploration:
                uiText.text = "Explore the construction site. When ready, open the options menu by pressing 'Y' and click \"Complete Activity.\"";
                break;
            case ProgressManager.Stage.ActivitySelect:
                uiText.text = "Select an activity to observe in the Construction Activity Library. You must visit all 5 activites to advance.";
                break;
            case ProgressManager.Stage.Activity:
                uiText.text = "Explore the environment and observe the avatar performing the task, noting any ergonomic risks that may appear. When ready, open the options menu by pressing 'Y' and click \"Complete Activity.\"";
                break;
            case ProgressManager.Stage.RobotCarpentry:
                uiText.text = "Navigate to the Wearable Robot Library. After examining each exoskeleton, apply to the avatar the one that best suits the CARPENTRY activity.";
                break;
            case ProgressManager.Stage.RobotCarpentryActivitySelect:
                uiText.text = "Revisit the CARPENTRY activity with the exoskeleton applied.";
                break;
            case ProgressManager.Stage.RobotConstructionLabor:
                uiText.text = "Navigate to the Wearable Robot Library. After examining each exoskeleton, apply to the avatar the one that best suits the CONSTRUCTION LABOR activity.";
                break;
            case ProgressManager.Stage.RobotConstructionLaborActivitySelect:
                uiText.text = "Revisit the CONSTRUCTION LABOR activity with the exoskeleton applied.";
                break;
            case ProgressManager.Stage.RobotDrywall:
                uiText.text = "Navigate to the Wearable Robot Library. After examining each exoskeleton, apply to the avatar the one that best suits the DRYWALL activity.";
                break;
            case ProgressManager.Stage.RobotDrywallActivitySelect:
                uiText.text = "Revisit the DRYWALL activity with the exoskeleton applied.";
                break;
            case ProgressManager.Stage.RobotElectrician:
                uiText.text = "Navigate to the Wearable Robot Library. After examining each exoskeleton, apply to the avatar the one that best suits the ELECTRICIAN activity.";
                break;
            case ProgressManager.Stage.RobotElectricianActivitySelect:
                uiText.text = "Revisit the ELECTRICIAN activity with the exoskeleton applied.";
                break;
            case ProgressManager.Stage.RobotMasonry:
                uiText.text = "Navigate to the Wearable Robot Library. After examining each exoskeleton, apply to the avatar the one that best suits the MASONRY activity.";
                break;
            case ProgressManager.Stage.RobotMasonryActivitySelect:
                uiText.text = "Revisit the MASONRY activity with the exoskeleton applied.";
                break;
            case ProgressManager.Stage.RobotActivity:
                uiText.text = "Observe the avatar performing the same task again. Notice how the green indicators highlight improved posture and reduced strain. When ready, open the options menu by pressing 'Y' and click \"Complete Activity.\"";
                break;
            case ProgressManager.Stage.Finish:
                uiText.text = "You have completed this module. Please return the headset to your instructor.";
                break;
            default:
                uiText.text = "";
                break;
        }
    }

    private void ReplayQuestItemAnimation()
    {
        GameObject questItem = GameObject.FindWithTag("Instruction");
        if (questItem != null)
        {
            Animator animator = questItem.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("In", 0, 0f);
                Debug.Log("[ProgressHUDController] QuestItem animation replayed.");
            }
        }
        else
        {
            Debug.LogWarning("[ProgressHUDController] No GameObject tagged 'Instruction' found.");
        }
    }

    private void OnDestroy()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnStageChanged -= OnStageChanged;
        }
    }
}
