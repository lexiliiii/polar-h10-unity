using Oculus.Interaction.Samples;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class REBAInputUI : MonoBehaviour
{
    public delegate void ConfirmInput(int input);
    public event ConfirmInput OnConfirmInput;

    //[SerializeField] private DropDownGroup dropDownGroup;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private GameObject toggleDropdown;
    [SerializeField] private List<Toggle> toggleOptions;            // options for which number the user can select
    [SerializeField] private Image displayImage;
    [SerializeField] private TextMeshProUGUI text;                  // added this for reba feedback
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private List<TextMeshProUGUI> optionsTexts;    // TODO: messy
    [SerializeField] private List<Toggle> toggles;                  // TODO: LMAO i'm manually matching this up with above rn


    public void UpdateUI(REBASection section) {

        if (section.DisplayImage == null) { displayImage.enabled = false; }
        else { displayImage.enabled = true; displayImage.sprite = section.DisplayImage; }

        if (section.questionText == null) { text.enabled = false; }
        else { text.text = section.questionText; }

        titleText.text = section.DisplayText;

        if (section.ToggleOptions.Count <= 0 ) { toggleDropdown.SetActive(false);  }
        else { toggleDropdown.SetActive(true); UpdateDropdownChoices(section); }
    }

    private void UpdateDropdownChoices(REBASection section) {
        var newOptions = section.ToggleOptions;
        for (int i = 0; i < optionsTexts.Count; ++i) {
            if (i < newOptions.Count) {
                optionsTexts[i].text = newOptions[i].ToString();    // TODO: conversion might be inoptimal but wtv
                toggles[i].gameObject.SetActive(true);
            } else {
                toggles[i].gameObject.SetActive(false);
            }
        }
    }

    public void ConfirmUserInput() {
        if (Int32.TryParse(valueText.text, out int input)) {
            OnConfirmInput.Invoke(input);
        }
    }
}
