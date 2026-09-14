using UnityEngine;
using Michsky.UI.Heat;

public class SelectorBasedButtonControl : MonoBehaviour
{
    [SerializeField] private HorizontalSelector horSelector;

    [Header("Buttons per Selector Item")]
    [SerializeField] private GameObject[] buttons; // Each index corresponds to selector index

    void Start()
    {
        horSelector.onValueChanged.AddListener(UpdateButtons);
        UpdateButtons(horSelector.index); // Ensure correct button shown at start
    }

    void UpdateButtons(int selectedIndex)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].SetActive(i == selectedIndex);
        }
    }
}