using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// There's an animation event on the animation playing (REBA worker)
/// This is the class + function that will stop the animation at that point
/// </summary>
public class StopAnimation : MonoBehaviour
{
    [SerializeField] private REBAManager rebaManager;
    [SerializeField] private Animator _animator;

    public void StopAnim() {
        if (!rebaManager.IsAnimating) {
            _animator.speed = 0;
        }
    }
}
