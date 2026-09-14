using UnityEngine;
using TMPro;

public class ActivityProgressDisplay : MonoBehaviour
{
    public TextMeshProUGUI progressText;

    void Start()
    {
        UpdateProgress();
    }

    public void UpdateProgress()
    {
        int totalActivities = ActivitySelectionManager.Instance.GetTotalCount();
        int completed = ActivitySelectionManager.Instance.GetCompletedCount();
        progressText.text = $"ACTIVITIES COMPLETED:  {completed} / {totalActivities}";
    }
}