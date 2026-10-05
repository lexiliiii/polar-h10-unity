using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Module3.Neon.Editor
{
    public static class NeonGazeOnlyMenu
    {
        [MenuItem("Tools/Neon/Add Gaze-Only UI Cursor to Current Scene")]
        public static void Add()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[NeonGaze] Exit Play mode before adding the gaze-only cursor so the scene can save it.");
                return;
            }
            var existing = Object.FindObjectOfType<NeonGazeOnlyCursor>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[NeonGaze] Gaze-only cursor already exists in this scene.", existing);
                return;
            }

            var root = new GameObject("Neon Gaze-Only Integration");
            Undo.RegisterCreatedObjectUndo(root, "Add Neon gaze-only cursor");
            root.AddComponent<NeonGazeOnlyReceiver>();
            root.AddComponent<NeonGazeBridgeProcess>();

            var canvasObject = new GameObject("Neon Gaze Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Add Neon gaze Canvas");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var dotObject = new GameObject("Red Gaze Cursor", typeof(RectTransform), typeof(Image));
            dotObject.transform.SetParent(canvasObject.transform, false);
            RectTransform dot = dotObject.GetComponent<RectTransform>();
            dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = new Vector2(56, 56);
            Image image = dotObject.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.color = new Color(1, 0.01f, 0.01f, 1);
            image.raycastTarget = false;
            dotObject.SetActive(false);

            var cursor = root.AddComponent<NeonGazeOnlyCursor>();
            cursor.Configure(canvasObject.GetComponent<RectTransform>(), dot);
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[NeonGaze] Added gaze-only receiver, bridge launcher, Canvas and red cursor.", root);
        }

        [MenuItem("Tools/Neon/Add Gaze-Only UI Cursor to Current Scene", true)]
        private static bool ValidateAdd() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
