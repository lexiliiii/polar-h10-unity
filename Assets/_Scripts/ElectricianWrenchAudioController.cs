using UnityEngine;

public class ElectricianWrenchAudioController : MonoBehaviour
{
    public float yThreshold = 1.29f;
    private AudioSource audioSource;
    private bool wasAboveThreshold = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        float localY = transform.localPosition.y;

        if (localY > yThreshold)
        {
            if (!wasAboveThreshold)
            {
                audioSource.Play();
                wasAboveThreshold = true;
            }
        }
        else
        {
            if (wasAboveThreshold)
            {
                audioSource.Stop();
                wasAboveThreshold = false;
            }
        }
    }
}