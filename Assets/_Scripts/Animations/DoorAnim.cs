using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Opens the Quarters door
/// </summary>
public class DoorAnim : MonoBehaviour
{
    [SerializeField] private Transform pivot;

    [YarnCommand("door_anim")]
    public void Play() {
        RotateOverTime(pivot, Vector3.up, -159f, 2f);
    }

    private void RotateOverTime(Transform target, Vector3 axis, float angle, float duration) {
        StartCoroutine(RotateCoroutine(target, axis, angle, duration));
    }

    private IEnumerator RotateCoroutine(Transform target, Vector3 axis, float angle, float duration) {
        Quaternion startRotation = target.rotation;
        Quaternion endRotation = startRotation * Quaternion.AngleAxis(angle, axis.normalized);
        float elapsed = 0f;

        while (elapsed < duration) {
            float t = elapsed / duration;
            target.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.rotation = endRotation; // Snap to final rotation
    }
}
