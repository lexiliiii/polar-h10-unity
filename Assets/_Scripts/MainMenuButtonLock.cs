using UnityEngine;
using Michsky.UI.Heat;

public class MainMenuButtonLock : MonoBehaviour
{
    public BoxButtonManager activityLibraryButton;
    public BoxButtonManager robotLibraryButton;
    public GameObject activityLibraryDisabledOverlay;
    public GameObject robotLibraryDisabledOverlay;

    private void Start()
    {
        // Initial state check
        UpdateButtonStates(ProgressManager.Instance.CurrentStage.ToString());

        // Subscribe to stage change
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnStageChanged += UpdateButtonStates;
        }
    }

    private void OnDestroy()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.OnStageChanged -= UpdateButtonStates;
        }
    }

    private void UpdateButtonStates(string stageName)
    {
        if (!System.Enum.TryParse(stageName, out ProgressManager.Stage stage))
            return;

        activityLibraryButton.enabled = stage >= ProgressManager.Stage.ActivitySelect;
        activityLibraryDisabledOverlay.SetActive(stage < ProgressManager.Stage.ActivitySelect);
        robotLibraryButton.enabled = stage >= ProgressManager.Stage.RobotCarpentry;
        robotLibraryDisabledOverlay.SetActive(stage < ProgressManager.Stage.RobotCarpentry);

        activityLibraryButton.UpdateUI();
        robotLibraryButton.UpdateUI();
    }
}