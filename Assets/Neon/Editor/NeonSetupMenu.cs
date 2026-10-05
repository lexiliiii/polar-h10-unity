using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Module3.Neon.Editor
{
    public static class NeonSetupMenu
    {
        // Retained because EyeTrackingTestRoom still uses the official Neon XR package.
        [MenuItem("Tools/Neon/Open XR Core Test Scene")]
        public static void OpenXrCoreTestScene()
        {
            const string scenePath = "Assets/_Scenes/EyeTrackingTestRoom.unity";
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }
}
