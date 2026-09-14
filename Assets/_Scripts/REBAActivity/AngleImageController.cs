using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used to Set the Angle Amount for the Angle Visualizer when the prefab is spawned
/// by the REBAManager.cs
/// </summary>
public class AngleImageController : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] TextMeshPro text;

    public void SetAngle(float angle) {
        image.fillAmount = angle;
    }

    public void SetText(string newText) {
        text.text = newText;
    }
    
    public void RadialChangeBottom() {
        image.fillOrigin = (int) Image.Origin360.Bottom;
        image.fillClockwise = true;
    }

    public void RadialChangeTop()
    {
        image.fillOrigin = (int) Image.Origin360.Top;
        image.fillClockwise = false;
    }
}
