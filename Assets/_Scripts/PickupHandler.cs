using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupHandler : MonoBehaviour
{
    [SerializeField] private Transform handTransform;
    [SerializeField] private Transform targetObjectTransform;
    [SerializeField] private Transform attachTransform;
    [SerializeField] private float lerpSpeed = 5f;
    private bool isFollowing = false;
    private bool isMovingToTransform = false;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isMovingToTransform)
        {
            targetObjectTransform.position = Vector3.Lerp(
                targetObjectTransform.position,
                attachTransform.position,
                Time.deltaTime * lerpSpeed);

            targetObjectTransform.rotation = Quaternion.Slerp(
                targetObjectTransform.rotation,
                attachTransform.rotation,
                Time.deltaTime * lerpSpeed);

            if (Vector3.Distance(targetObjectTransform.position, attachTransform.position) < 0.001f)
            {
                isFollowing = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (isFollowing)
        {
            targetObjectTransform.position = handTransform.position;
        }
    }

    public void toggleFollowing()
    {
        isFollowing = !isFollowing;
    }

    public void attachObjectToTransform()
    {
        isMovingToTransform = true;
        isFollowing = false;
    }

    public void PlayWoodPlacingSound()
    {
        audioSource.Play();
    }
}
