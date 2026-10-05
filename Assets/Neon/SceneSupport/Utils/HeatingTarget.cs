using System.Collections;
using UnityEngine;

public class HeatingTarget : MonoBehaviour {

    private enum State { Idle = 0, Heating = 1 };
    private State state;

    [Header("Logic & References")]
    [SerializeField] private MeshRenderer[] mRenderers;
    [SerializeField] private float heatUpDuration;
    [SerializeField] private float coolOffDuration;

    [Header("Visuals & Audio")]
    [SerializeField] private Gradient gradientColor;
    [SerializeField] private AnimationCurve gradientEmmision;
    [SerializeField] private Gradient gradientEmmisionColor;
    [SerializeField] private float pingTime;

    private float OverheatTarget => state switch { State.Heating => 1, _ => 0 };

    private float overheatPercent, pingTimer;

    void Awake() {
        UpdateVisuals();
    }

    void Update() {
        if (pingTimer > 0) {
            pingTimer = Mathf.MoveTowards(pingTimer, 0, Time.deltaTime);
            if (pingTimer <= 0) {
                state = State.Idle;
                ResumeAsyncCountdown();
            }
        }
    }

    public void PingObject() {
        if (pingTimer <= 0) {
            state = State.Heating;
            ResumeAsyncCountdown();
        }
        pingTimer = pingTime;
    }

    private IEnumerator IUpdateCountdown() {
        float speed;
        while (overheatPercent.Distance(OverheatTarget) > Mathf.Epsilon) {
            speed = state switch { State.Heating => heatUpDuration, _ => coolOffDuration };
            overheatPercent = Mathf.MoveTowards(overheatPercent, OverheatTarget, Time.deltaTime * 1.0f.SafeDivide(speed));
            UpdateVisuals();
            yield return null;
        }
    }

    private void UpdateVisuals() {
        MaterialPropertyBlock mpb = new();
        foreach (MeshRenderer mr in mRenderers) {
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", gradientColor.Evaluate(overheatPercent));
            Vector4 emmisionColor = gradientEmmisionColor.Evaluate(overheatPercent);
            emmisionColor *= gradientEmmision.Evaluate(overheatPercent);
            emmisionColor.w = 1;
            mpb.SetColor("_EmissionColor", emmisionColor);
            
            Debug.Log(overheatPercent);
            mr.SetPropertyBlock(mpb);
        }
    }

    private void ResumeAsyncCountdown() {
        StopAllCoroutines();
        StartCoroutine(IUpdateCountdown());
    }
}
