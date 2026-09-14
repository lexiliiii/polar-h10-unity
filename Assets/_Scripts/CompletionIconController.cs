using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Heat;

public class CompletionIconController : MonoBehaviour
{
    public string activityName;
    public Sprite openCircleSprite;
    public Sprite closedCircleSprite;

    [Header("Optional: Standalone Icon (overrides BoxButton icon)")]
    public Image iconImage;

    [Header("Global Completion Mode")]
    public bool checkAllActivities = false;

    private BoxButtonManager boxButton;

    void Awake()
    {
        boxButton = GetComponent<BoxButtonManager>();
    }

    void Start()
    {
        UpdateIcon();
    }

    public void UpdateIcon()
    {
        Sprite targetSprite;

        if (checkAllActivities)
        {
            targetSprite = ActivitySelectionManager.Instance.GetCompletedCount() == ActivitySelectionManager.Instance.GetTotalCount()
                ? closedCircleSprite
                : openCircleSprite;
        }
        else
        {
            targetSprite = ActivitySelectionManager.Instance.IsActivityCompleted(activityName)
                ? closedCircleSprite
                : openCircleSprite;
        }

        if (iconImage != null)
        {
            iconImage.sprite = targetSprite;
        }
        else if (boxButton != null)
        {
            boxButton.buttonIcon = targetSprite;
            boxButton.UpdateUI();
        }
        else
        {
            Debug.LogWarning("No icon target found for: " + (checkAllActivities ? "All Activities" : activityName));
        }
    }
}