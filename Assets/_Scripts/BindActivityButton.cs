using UnityEngine;
using Michsky.UI.Heat;

public class BindActivityButton : MonoBehaviour
{
    public BoxButtonManager activityButton;
    public ButtonManager confirmButton;
    public ButtonManager completeButton;
    public string activitySceneName;
    public bool isWalkthrough = false;

    void Start()
    {
        if (activityButton != null)
        {
            string localSceneName = activitySceneName; // capture per-instance value

            activityButton.onClick.RemoveAllListeners();
            activityButton.onClick.AddListener(() =>
            {
                Debug.Log($"Clicked Button: {gameObject.name}, Scene: {localSceneName}");

                if (isWalkthrough)
                {
                    ActivitySelectionManager.Instance.StartWalkthrough();
                }
                else
                {
                    bool valid = ActivitySelectionManager.Instance.SetSelectedActivity(localSceneName);
                    if (!valid)
                    {
                        Debug.LogWarning("Invalid activity selected for this stage: " + localSceneName);
                    }
                }
            });
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
                ActivitySelectionManager.Instance.ConfirmActivity());
        }

        if (completeButton != null)
        {
            completeButton.onClick.RemoveAllListeners();
            completeButton.onClick.AddListener(() =>
                ActivitySelectionManager.Instance.CompleteActivity());
        }

        Debug.Log($"{gameObject.name} initialized with scene: {activitySceneName}");
    }
}