using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationPause : MonoBehaviour
{
    [SerializeField] private bool isPaused = false;
    private Animator animator;
    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    // Update is called once per frame
    void Update()
    {
        if (isPaused)
        {
            animator.speed = 0;
        }
        else
        {
            animator.speed = 1;
        }
    }
}
