using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace Module3.PolarH10
{
    /// <summary>
    /// Presents only the coaching engine's current learner message. In XR the
    /// world-space canvas follows the eye camera; desktop uses a screen overlay.
    /// This component does not evaluate readings or extend a message's lifetime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PolarLearnerPrompt : MonoBehaviour
    {
        [Tooltip("Optional eye camera. Otherwise the active stereo camera or MainCamera is used.")]
        public Camera learnerCamera;
        [Tooltip("Optional font. Defaults to this project's TextMesh Pro default font.")]
        public TMP_FontAsset font;
        [Tooltip("For testing a world-space prompt without a connected headset.")]
        public bool forceWorldSpace;
        [Min(0.5f)] public float distance = 1.5f;
        public float verticalOffset = -0.3f;

        public string CurrentText { get; private set; } = string.Empty;
        public bool IsVisible => view != null && view.enabled && !string.IsNullOrEmpty(CurrentText);

        private Canvas view;
        private RectTransform viewRect, card;
        private CanvasScaler scaler;
        private TextMeshProUGUI label;

        public void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { Hide(); return; }
            EnsureView();
            CurrentText = message;
            label.text = message;
            UpdatePlacement();
            view.enabled = isActiveAndEnabled;
        }

        public void Hide()
        {
            CurrentText = string.Empty;
            if (label != null) label.text = string.Empty;
            if (view != null) view.enabled = false;
        }

        private void OnDisable() => Hide();

        private void LateUpdate()
        {
            if (IsVisible) UpdatePlacement();
        }

        private void EnsureView()
        {
            if (view != null) return;
            var canvasObject = new GameObject("Polar learner coaching", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            view = canvasObject.GetComponent<Canvas>();
            view.enabled = false;
            view.overrideSorting = true;
            view.sortingOrder = 100;
            viewRect = (RectTransform)canvasObject.transform;
            scaler = canvasObject.GetComponent<CanvasScaler>();
            var group = canvasObject.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var cardObject = new GameObject("Coaching card", typeof(RectTransform), typeof(Image));
            card = (RectTransform)cardObject.transform;
            card.SetParent(viewRect, false);
            card.sizeDelta = new Vector2(980f, 190f);
            var background = cardObject.GetComponent<Image>();
            background.color = new Color(.04f, .09f, .14f, .96f);
            background.raycastTarget = false;

            var textObject = new GameObject("Learner message", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(card, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(28f, 18f);
            textRect.offsetMax = new Vector2(-28f, -18f);
            label = textObject.GetComponent<TextMeshProUGUI>();
            label.font = font != null ? font : TMP_Settings.defaultFontAsset;
            if (label.font == null) label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            label.fontSize = 32f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 32f;
            label.enableWordWrapping = true;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.richText = false;
            label.raycastTarget = false;
            label.text = string.Empty;
        }

        private Camera FindLearnerCamera()
        {
            if (learnerCamera != null && learnerCamera.isActiveAndEnabled) return learnerCamera;
            if (XRSettings.isDeviceActive)
                foreach (var camera in Camera.allCameras)
                    if (camera.isActiveAndEnabled && camera.stereoEnabled) return camera;
            return Camera.main;
        }

        private void UpdatePlacement()
        {
            var camera = FindLearnerCamera();
            bool worldSpace = camera != null && (forceWorldSpace || XRSettings.isDeviceActive || camera.stereoEnabled);
            if (worldSpace)
            {
                view.renderMode = RenderMode.WorldSpace;
                view.worldCamera = camera;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                viewRect.sizeDelta = new Vector2(980f, 190f);
                // Keep the card in front of the near clipping plane and below gaze.
                float z = Mathf.Max(distance, camera.nearClipPlane + .2f);
                viewRect.position = camera.transform.TransformPoint(new Vector3(0f, verticalOffset, z));
                viewRect.rotation = camera.transform.rotation;
                viewRect.localScale = Vector3.one * .001f;
                card.anchorMin = card.anchorMax = new Vector2(.5f, .5f);
                card.pivot = new Vector2(.5f, .5f);
                card.anchoredPosition = Vector2.zero;
                int layer = VisibleLayer(camera);
                view.gameObject.layer = card.gameObject.layer = label.gameObject.layer = layer;
            }
            else
            {
                view.renderMode = RenderMode.ScreenSpaceOverlay;
                view.worldCamera = null;
                viewRect.localScale = Vector3.one;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
                card.anchorMin = card.anchorMax = new Vector2(.5f, 0f);
                card.pivot = new Vector2(.5f, 0f);
                card.anchoredPosition = new Vector2(0f, 24f);
            }
        }

        private static int VisibleLayer(Camera camera)
        {
            const int uiLayer = 5;
            if ((camera.cullingMask & (1 << uiLayer)) != 0) return uiLayer;
            for (int layer = 0; layer < 32; layer++)
                if ((camera.cullingMask & (1 << layer)) != 0) return layer;
            return uiLayer;
        }
    }
}
