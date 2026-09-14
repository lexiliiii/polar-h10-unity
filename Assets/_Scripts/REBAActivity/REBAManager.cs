using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manager for the REBA activity
/// </summary>
public class REBAManager : MonoBehaviour
{
    public event System.Action OnAssessmentEnd;

    [SerializeField] private REBALocators locators;
    [SerializeField] private REBAInputUI inputUI;
    [SerializeField] private REBAScoringManager scoringManager;
    [Space]
    [SerializeField] private List<REBASection> sections;
    [Space]
    [SerializeField] private Canvas angleVisualizer;
    [Space]
    [SerializeField] private bool enableOnStart = true;
    
    private Canvas _canvas;
    private AngleImageController _angleImgController;
    private REBASection _currentSection;
    private List<Transform> _currLocators;      // [0] = main axis, [1] = angle axis

    private bool _isAnimating = true;
    public bool IsAnimating => _isAnimating;

    private void Start() {
        if (enableOnStart) StartREBAAssessment();      // TODO: test delete later

        inputUI.OnConfirmInput += HandleUserInput;
    }

    void Update() {
        if (!IsAnimating)
            DrawAngleOnScreen(_currLocators[0], _currLocators[1]);

        if (Input.GetKeyDown(KeyCode.Space)) {
            NextSection();
        }
    }

    public void StartREBAAssessment() {
        NextSection();
        _isAnimating = false;       // pause the animation
    }

    public void NextSection() {

        // update the current index, or start with 0
        int currIndex;
        if (_currentSection == null) { currIndex = 0; }
        else { currIndex = sections.IndexOf(_currentSection) + 1; }

        if (currIndex < sections.Count) {
            _currentSection = sections[currIndex];

            _currLocators = locators.GetCurrentLocators(_currentSection.SectionIndex);      // update the current locators

            DisplayAngleVisualizer();
            DisplayInputUI();
        } else {
            OnAssessmentEnd?.Invoke();
        }
    }

    /// <summary>
    /// Display the angle pie chart that will show the angles on a ui canvas
    /// </summary>
    private void DisplayAngleVisualizer() {
        // if this is the first time this is called instantiate
        if (_canvas == null) {
            _canvas = Instantiate(angleVisualizer, _currLocators[0]);
            _angleImgController = _canvas.GetComponent<AngleImageController>();
        } else {    // jank canvas reposition because world space is pain
            // try something new
            _canvas.transform.SetParent(_currLocators[0], false);
            _canvas.transform.position = _currLocators[0].position;
        }
        var rectTransform = _canvas.GetComponent<RectTransform>();

        float distance = Vector3.Distance(_currLocators[0].position, _currLocators[1].position);

        //Vector3 midpoint = (_currLocators[0].position + _currLocators[1].position) / 2f;
        //_canvas.transform.position = midpoint;
        _canvas.transform.position = _currLocators[0].position;
        //_canvas.transform.rotation = _currLocators[0].rotation;
        rectTransform.sizeDelta = new Vector2(distance * 4, distance * 4);
        
    }

    public void DisplayInputUI() {
        inputUI.gameObject.SetActive(true);
        var newImage = _currentSection.DisplayImage;
        var newText = _currentSection.DisplayText;
        inputUI.UpdateUI(_currentSection);
        inputUI.gameObject.SetActive(true);
    }

    public void HandleUserInput(int input) {
        // get the number from the user input and set
        // the current SO score variable to it

        if (scoringManager) {
            REBABodyPart partType = _currentSection.partType;
            scoringManager.SetScore(partType, input);
        }
        NextSection();
    }

    private void DrawAngleOnScreen(Transform mainAxis, Transform locator) {
        Vector3 mainDirection;
        if (locator.position.y > mainAxis.position.y) {
            mainDirection = mainAxis.up;
            _angleImgController.RadialChangeTop();
        } else {
            mainDirection = -mainAxis.up;
            _angleImgController.RadialChangeBottom();
        }
        Vector3 angleVector = locator.position - mainAxis.position;

        float angle = Vector3.Angle(angleVector, mainDirection);
        var angleAsPercent = angle / 360;

        if (_angleImgController != null) {
            _angleImgController.SetAngle(angleAsPercent);
        }

        locators.UpdateLabel(angle);        // update the label with angle text
    }

    public void ToggleExoskeletons() => locators.ToggleExoskeletons();
}
