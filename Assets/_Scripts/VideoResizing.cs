using UnityEngine;
using UnityEngine.Video;

public class VideoResizing : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private Transform videoPlayerTransform;
    private Transform screenTransform; // for extracting screen dimensions
    private float maxWidth;
    private float maxHeight;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayerTransform = GetComponent<Transform>();
        screenTransform = GetComponentInParent<Transform>();

        // Get the TV frame size
        maxWidth = screenTransform.localScale.z;
        maxHeight = screenTransform.localScale.y;

        // Attach event listener to adjust the video aspect ratio after loading
        videoPlayer.prepareCompleted += Resize;

        videoPlayer.Prepare();
    }

    // Resizing so that the video fits within the bounds of the TV frame
    void Resize(VideoPlayer vp)
    {
        float videoWidth = vp.width;
        float videoHeight = vp.height;

        if (videoWidth == 0 || videoHeight == 0)
        {
            Debug.LogError("Invalid video dimensions.");
            return;
        }

        float videoAspect = videoWidth / videoHeight; // 16:9 = 1.777
        float screenAspect = screenTransform.localScale.z / screenTransform.localScale.y;

        if (videoAspect > screenAspect)
        {
            // Fit width (Z), adjust height (Y) to maintain 16:9
            float newHeight = screenTransform.localScale.z / videoAspect;
            videoPlayerTransform.localScale = new Vector3(1, newHeight / screenTransform.localScale.y, 1);
        }
        else
        {
            // Fit height (Y), adjust width (Z)
            float newWidth = screenTransform.localScale.y * videoAspect;
            videoPlayerTransform.localScale = new Vector3(1, 1, newWidth / screenTransform.localScale.z);
        }
    }
}
