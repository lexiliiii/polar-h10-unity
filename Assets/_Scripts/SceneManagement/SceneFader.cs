using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Yarn.Unity;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private Image img;
    [SerializeField] private AnimationCurve curve;
    //[SerializeField] private float fadeTime = 1f;

    //public void EnterScene() {
    //    StartCoroutine(FadeIn());
    //}
    //void Start() {
    //    StartCoroutine(FadeIn());
    //}

    //public void FadeTo(int index) {
    //    StartCoroutine(FadeOut(index));
    //}

    [YarnCommand("fade_in")]
    public IEnumerator FadeIn(int fadeTime) {
        float t = fadeTime;

        while (t > 0) {
            float a = curve.Evaluate(t / fadeTime);
            img.color = new Color(0f, 0f, 0f, a);
             t -= Time.deltaTime;
            yield return 0;         // wait a frame and then continue
        }
    }

    IEnumerator FadeOut(int index, int fadeTime) {
        float t = 0f;    // time

        while (t < fadeTime) {
            float a = curve.Evaluate(t);
            img.color = new Color(0f, 0f, 0f, a);
            t += Time.deltaTime;
            yield return 0;         // wait a frame and then continue
        }
        SceneManager.LoadScene(index);
    }
}
