using UnityEngine;

public class AdvanceStageButton : MonoBehaviour
{
    public ProgressManager.Stage stageToAdvanceTo;

    public void AdvanceStage()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.AdvanceToStage(stageToAdvanceTo);
        }
        else
        {
            Debug.LogWarning("ProgressManager instance not found.");
        }
    }
}