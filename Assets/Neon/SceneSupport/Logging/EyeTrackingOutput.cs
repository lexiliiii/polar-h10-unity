using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PupilLabs;

public class EyeTrackingOutput : MonoBehaviour
{
    [SerializeField] private UserInputRecorder recorder;
    [SerializeField] private float outputDelay;

    void Start() {
        recorder.StartLogging();
        StartCoroutine(IDelayOutputLog());
    }

    private IEnumerator IDelayOutputLog() {
        float outputTime = Time.time + outputDelay;
        while (Time.time < outputTime) {
            yield return null;
        }
        recorder.StopLoggingAndSave();
    }
}
