using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Module3.PolarH10.Editor
{
    public static class PolarDashboardMenu
    {
        public const string ScenePath = "Assets/PolarH10/Scenes/PolarRmsDashboard.unity";
        [MenuItem("Tools/Polar H10/Open Dashboard Scene")]
        public static void Open()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Tools/Polar H10/Add Dashboard to Current Scene")]
        public static void Add()
        {
            var existing = UnityEngine.Object.FindObjectOfType<PolarRmsDashboard>();
            if(existing) { Selection.activeGameObject=existing.gameObject; return; }
            var go=new GameObject("Polar H10 RMS Dashboard");
            Undo.RegisterCreatedObjectUndo(go,"Add Polar dashboard");
            go.AddComponent<PolarRmsDashboard>(); Selection.activeGameObject=go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
        [MenuItem("Tools/Polar H10/Build Windows Dashboard")]
        public static void BuildWindows()
        {
            // Uses this project's settings, assets and scripts. No project copy or upgrade.
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{ScenePath}, locationPathName="Builds/PolarDashboard/PolarDashboard.exe",
                target=BuildTarget.StandaloneWindows64, options=BuildOptions.None
            });
            if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Polar dashboard build failed: "+report.summary.result);
            Debug.Log("Polar dashboard built: Builds/PolarDashboard/PolarDashboard.exe");
        }
    }
}
