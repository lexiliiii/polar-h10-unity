using UnityEngine;
using UnityEngine.Video;

// Video player that cycles between clips
public class VideoSwitcher : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoClip[] videoClips;
    public GameObject videoScreen;

    private int currentVideoIndex = 0;

    void Start()
    {
        if (videoPlayer == null || videoScreen == null)
        {
            Debug.LogError("Video Player or Video Screen is not assigned!");
            return;
        }

        if (videoClips.Length > 0)
        {
            videoPlayer.prepareCompleted += AdjustAspectRatio;
            PlayVideo(0); // Start with the first video
        }
    }

    void Update()
    {
        // Play/Pause with A Button
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            TogglePlayPause();
        }

        // Next Video with B Button
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            PlayNextVideo();
        }

        // Previous Video with X Button
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            PlayPreviousVideo();
        }

        // Stop Video with Y Button
        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            StopVideo();
        }
    }

    private void PlayVideo(int index)
    {
        if (videoClips.Length == 0) return;

        videoPlayer.Stop();
        videoPlayer.clip = videoClips[index];
        videoPlayer.Prepare();
        videoPlayer.Play();

        Debug.Log($"Playing video: {videoClips[index].name}");
    }

    private void PlayNextVideo()
    {
        if (videoClips.Length == 0) return;

        currentVideoIndex = (currentVideoIndex + 1) % videoClips.Length;
        PlayVideo(currentVideoIndex);
    }

    private void PlayPreviousVideo()
    {
        if (videoClips.Length == 0) return;

        currentVideoIndex = (currentVideoIndex - 1 + videoClips.Length) % videoClips.Length;
        PlayVideo(currentVideoIndex);
    }

    private void TogglePlayPause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            Debug.Log("Video Paused");
        }
        else
        {
            videoPlayer.Play();
            Debug.Log("Video Playing");
        }
    }

    private void StopVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
            Debug.Log("Video Stopped");
        }
    }

    private void AdjustAspectRatio(VideoPlayer vp)
    {
        // Get the video's width and height
        float videoWidth = vp.texture.width;
        float videoHeight = vp.texture.height;

        if (videoWidth == 0 || videoHeight == 0)
        {
            Debug.LogWarning("Unable to get video dimensions. Using default scaling.");
            return;
        }

        // Calculate the aspect ratio
        float aspectRatio = videoWidth / videoHeight;

        // Adjust the video screen's scale to preserve aspect ratio
        videoScreen.transform.localScale = new Vector3(aspectRatio, 1, 1);

        Debug.Log($"Video Aspect Ratio Adjusted: {aspectRatio}");
    }
}