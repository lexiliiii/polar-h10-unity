using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private void Start() {
        audioSource.volume = 0.0f;
    }

    //public IEnumerator Fade(bool fadeIn, AudioSource source, float duration, float targetVolume) {

    //}
}
