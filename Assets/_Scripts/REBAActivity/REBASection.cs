using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Holds the data for each score tracking step for the REBA area
/// (Ex. you would create a SO for the neck score + any bonuns questions needed to make the assesment)
/// 
/// 
/// honestly rn imma simplify it so the user just has to input one score
/// and do the calcs on their own until we need to upscale later
/// </summary>
[CreateAssetMenu(menuName ="REBA/REBASection")]
public class REBASection : ScriptableObject
{
    [SerializeField] private Sprite displayImage;
    [SerializeField] private string headerText;
    [SerializeField] private List<int> toggleOptions;       // REBA score options
    [SerializeField] private string sectionIndex;
    private int _overallScore;

    public int OverallScore() => _overallScore;
    public Sprite DisplayImage => displayImage;
    public string DisplayText => headerText;
    public List<int> ToggleOptions => toggleOptions;
    public string SectionIndex => sectionIndex;

    public REBABodyPart partType;
    public string questionText;       // added for REBA feedback

    public void UpdateScore(int score) {  _overallScore = score; }
}
