using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Vector3 offset = Vector3.zero;     // offset is a bit jank rn, but it works

    private Transform _t;

    void Start() {
        _t = transform;
    }

    void Update() {
        Vector3 relativePos = _t.position - (target.position + offset);

        Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
        _t.rotation = Quaternion.Lerp(_t.rotation, rotation, Time.deltaTime * rotationSpeed);
    }
}
