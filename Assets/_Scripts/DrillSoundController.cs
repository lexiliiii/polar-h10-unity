using UnityEngine;

public class DrillSoundController : MonoBehaviour
{
    [Header("Reference to animated drill tip (Transform)")]
    public Transform drillTip;

    [Header("Wall detection settings")]
    public float activationDistance = 0.2f;
    public LayerMask wallLayer;

    public AudioSource audioSource;

    private float lastStopTime = -Mathf.Infinity;
    public float bufferDuration = 1f;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Optional: ensure audio is set up correctly
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    void LateUpdate()
    {
        if (drillTip != null)
        {
            transform.position = drillTip.position;
            transform.rotation = drillTip.rotation;

            bool wallNearby = Physics.CheckSphere(drillTip.position, activationDistance, wallLayer);

            if (wallNearby)
            {
                if (!audioSource.isPlaying && Time.time - lastStopTime >= bufferDuration)
                {
                    audioSource.Play();
                }
            }
            else
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                    lastStopTime = Time.time;
                }
            }
        }
    }
}