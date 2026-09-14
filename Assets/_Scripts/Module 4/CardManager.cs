using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the logic for ethical case study cards Module 4
/// </summary>
public class CardManager : MonoBehaviour
{
    [SerializeField] private GameObject returnToCardsPanel;    // when the user is done with the scenario
    [SerializeField] private GameObject activityMenuWrapper;

    /// <summary>
    /// When the user clicks on a case card
    /// </summary>
    public void StartSimulation() {
        activityMenuWrapper.SetActive(false);
        returnToCardsPanel.SetActive(true);
    }
}
