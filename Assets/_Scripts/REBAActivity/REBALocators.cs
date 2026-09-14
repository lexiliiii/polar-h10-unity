using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data container for locators transform data and axis points
/// located on the worker prefab.
/// 
/// Controls the Screen UI for the REBA activity
/// (things the player does not interact with)
/// </summary>
public class REBALocators : MonoBehaviour
{
    // All locators need to be in the order: mainAxis transform, locator transform
    [SerializeField] private ExoskeletonREBAController exoskeletonController;
    [SerializeField] private List<Transform> neckLocators;
    [SerializeField] private GameObject neckLabel;
    [Space]
    [SerializeField] private List<Transform> trunkLocators;
    [SerializeField] private GameObject trunkLabel;
    [Space]
    [SerializeField] private List<Transform> legsLocators;
    [SerializeField] private GameObject legsLabel;
    [Space]
    [SerializeField] private List<Transform> upperArmLocators;
    [SerializeField] private GameObject upperArmLabel;
    [Space]
    [SerializeField] private List<Transform> lowerArmLocators;
    [SerializeField] private GameObject lowerArmLabel;
    [Space]
    [SerializeField] private List<Transform> wristLocators;
    [SerializeField] private GameObject wristLabel;

    private Label _currLabel;

    /// <summary>
    /// Based on the current REBA section, return the proper locator and activite the label
    /// </summary>
    public List<Transform> GetCurrentLocators(string index) {
        if (_currLabel != null) { _currLabel.gameObject.SetActive(false); }

        switch (index) {
            case "neck":
                neckLabel.SetActive(true);
                _currLabel = neckLabel.GetComponent<Label>();
                return neckLocators;
            case "trunk":
                trunkLabel.SetActive(true);
                _currLabel = trunkLabel.GetComponent<Label>(); 
                return trunkLocators;
            case "legs":
                legsLabel.SetActive(true);
                _currLabel = legsLabel.GetComponent<Label>();
                return legsLocators;
            case "upper arm":
                upperArmLabel.SetActive(true);
                _currLabel = upperArmLabel.GetComponent<Label>();
                return upperArmLocators;
            case "lower arm":
                lowerArmLabel.SetActive(true);
                _currLabel = lowerArmLabel.GetComponent<Label>();
                return lowerArmLocators;
            case "wrist":
                wristLabel.SetActive(true);
                _currLabel = wristLabel.GetComponent<Label>();
                return wristLocators;
        }

        Debug.Log("Got current locators: was null");
        return null;    // something went wrong if u got here
    }

    /// <summary>
    /// Called in the REBA manager to update the angle text
    /// </summary>
    public void UpdateLabel(float angle) {
        _currLabel.SetAngle((int) angle);
    }

    public void ToggleExoskeletons() => exoskeletonController.ToggleExoskeletons();
}
