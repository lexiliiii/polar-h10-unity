using UnityEngine;

namespace Module3.Neon
{
    /// <summary>Maps Neon scene-camera gaze pixels onto a Canvas without scene video.</summary>
    [RequireComponent(typeof(NeonGazeOnlyReceiver))]
    public sealed class NeonGazeOnlyCursor : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform cursor;
        [SerializeField, Min(0.05f)] private float staleAfterSeconds = 0.35f;
        [SerializeField] private bool verboseSampleLogging = true;

        private NeonGazeOnlyReceiver receiver;
        private NeonRawGazeSample latest;
        private double latestAt = double.NegativeInfinity;
        private double nextLog;

        private void OnEnable()
        {
            receiver = GetComponent<NeonGazeOnlyReceiver>();
            receiver.SampleReceived += Receive;
            SetVisible(false);
            Debug.Log("[NeonGaze] Cursor waiting for live gaze samples.", this);
        }

        private void Receive(NeonRawGazeSample sample)
        {
            latest = sample;
            latestAt = Time.realtimeSinceStartupAsDouble;
        }

        private void Update()
        {
            if (canvasRect == null || cursor == null || latest == null || !latest.worn ||
                Time.realtimeSinceStartupAsDouble - latestAt > staleAfterSeconds ||
                !TryConvert(latest.gazeX, latest.gazeY, latest.sourceWidth, latest.sourceHeight,
                    canvasRect.rect, out Vector2 anchored))
            {
                SetVisible(false);
                return;
            }

            cursor.anchoredPosition = anchored;
            SetVisible(true);
            if (verboseSampleLogging && Time.realtimeSinceStartupAsDouble >= nextLog)
            {
                nextLog = Time.realtimeSinceStartupAsDouble + 1;
                Debug.Log($"[NeonGaze] raw=({latest.gazeX:F1}, {latest.gazeY:F1}) / " +
                          $"{latest.sourceWidth}x{latest.sourceHeight}; canvas={anchored}", this);
            }
        }

        public static bool TryConvert(float gazeX, float gazeY, int sourceWidth, int sourceHeight,
            Rect canvas, out Vector2 anchored)
        {
            anchored = default;
            if (sourceWidth <= 0 || sourceHeight <= 0 || canvas.width <= 0 || canvas.height <= 0 ||
                float.IsNaN(gazeX) || float.IsInfinity(gazeX) ||
                float.IsNaN(gazeY) || float.IsInfinity(gazeY)) return false;
            float normalizedX = gazeX / sourceWidth;
            float normalizedTopY = gazeY / sourceHeight;
            if (normalizedX < 0 || normalizedX > 1 || normalizedTopY < 0 || normalizedTopY > 1)
                return false;
            // Neon pixels: top-left origin. Centered RectTransform: bottom-left Y direction.
            anchored = new Vector2((normalizedX - 0.5f) * canvas.width,
                (0.5f - normalizedTopY) * canvas.height);
            return true;
        }

        private void SetVisible(bool value)
        {
            if (cursor != null && cursor.gameObject.activeSelf != value) cursor.gameObject.SetActive(value);
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.SampleReceived -= Receive;
            latest = null;
            SetVisible(false);
        }

        public void Configure(RectTransform targetCanvas, RectTransform targetCursor)
        {
            canvasRect = targetCanvas;
            cursor = targetCursor;
        }
    }
}
