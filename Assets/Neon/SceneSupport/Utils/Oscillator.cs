using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oscillator : MonoBehaviour {

    private Vector3 startPos;

    [SerializeField] private float yAmp, yMult, rotSpeed;

    void Awake() {
        startPos = transform.position;
    }

    void Update() {
        transform.position = new(startPos.x,
                                 startPos.y + Mathf.Sin(Time.deltaTime * yMult) * yAmp,
                                 startPos.z);
        transform.Rotate(Time.deltaTime * rotSpeed * Vector3.one);
    }
}
