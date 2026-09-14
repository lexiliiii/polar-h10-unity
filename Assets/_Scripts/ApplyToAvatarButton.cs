using UnityEngine;
using Michsky.UI.Heat;

public class ApplyToAvatarButton : MonoBehaviour
{
    [Header("ButtonManager Wrappers")]
    public ButtonManager applyButtonManager;
    public ButtonManager appliedButtonManager;

    private void Start()
    {
        if (applyButtonManager == null || appliedButtonManager == null)
            Debug.LogWarning("ButtonManager references not set!");

        var manager = RobotSelectionManager.Instance;
        if (manager != null)
        {
            manager.onSuitApplied.AddListener(_ => UpdateButtonUI());
            manager.onSuitPreviewed.AddListener(_ => UpdateButtonUI());
        }

        UpdateButtonUI();
    }

    public void OnApplyClicked()
    {
        RobotSelectionManager.Instance.ApplySuit();
        UpdateButtonUI();
    }

    private void UpdateButtonUI()
    {
        var manager = RobotSelectionManager.Instance;
        if (manager == null || manager.previewSet == null)
        {
            applyButtonManager.gameObject.SetActive(true);
            appliedButtonManager.gameObject.SetActive(false);
            return;
        }

        bool isApplied = manager.previewSet == manager.equippedSet;

        applyButtonManager.gameObject.SetActive(!isApplied);
        appliedButtonManager.gameObject.SetActive(isApplied);
    }
}