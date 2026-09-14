using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BOOTSTRAP for testing functionality of the REBA feedback
/// </summary>
public class REBAFeedback : MonoBehaviour
{
    [SerializeField] List<REBASection> feedbackSections;
    [SerializeField] REBAInputUI inputUI;

    private int currIdx = 0;

    void Start() {
        NextSection();
    }

    public void NextSection() {
        if (currIdx >= feedbackSections.Count) { return; }

        inputUI.UpdateUI(feedbackSections[currIdx]);
        currIdx++;
    }
}
