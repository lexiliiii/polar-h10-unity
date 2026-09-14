using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Script attatched to the Label Prefab
/// Sets the text, and line renderer locations
/// You need to attatch the Object Connect Point
/// </summary>
public class Label : MonoBehaviour
{
    [SerializeField] private Transform labelCanvas;
    [SerializeField] private TextMeshProUGUI angleText;

    [SerializeField] private Transform textConnectPoint;
    [SerializeField] private Transform objectConnectPoint;
    [SerializeField] private LineRenderer lineRenderer;

    void Start() {
        labelCanvas.localPosition = new Vector3(labelCanvas.localPosition.x, labelCanvas.localPosition.y, objectConnectPoint.localPosition.z);
        lineRenderer.SetPosition(0, textConnectPoint.position);
    }

    void Update() {
        lineRenderer.SetPosition(1, objectConnectPoint.position);   // constantly updating since this might move
    }

    public void SetAngle(int angle) {
        angleText.text = angle.ToString();
    }
}
