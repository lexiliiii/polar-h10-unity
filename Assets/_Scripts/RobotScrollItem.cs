using UnityEngine;
using Michsky.UI.Heat;

public class RobotScrollItem : MonoBehaviour
{
    public ExosuitMaterialSet materialSet;
    public HorizontalSelector horSelector;
    public int myIndex;

    void Start()
    {
        horSelector.onValueChanged.AddListener(OnSelectorChanged);

        // Force callback on initial selection
        if (horSelector.index == myIndex)
        {
            RobotSelectionManager.Instance.PreviewSuit(materialSet);
            Debug.Log($"[Init Preview] {materialSet.suitType}");
        }
    }

    void OnSelectorChanged(int index)
    {
        if (index == myIndex)
        {
            RobotSelectionManager.Instance.PreviewSuit(materialSet);
            Debug.Log("Preview: " + materialSet.suitType);
        }
    }
}