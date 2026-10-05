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
        // Also callable from Unity batch mode with -executeMethod.
        [MenuItem("Tools/Polar H10/Configure 3D Dashboard")]
        public static void Configure()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var dashboard = UnityEngine.Object.FindObjectOfType<PolarRmsDashboard>();
            var body = dashboard.GetComponent<PolarBodyVisualization>();
            if (!body) body = dashboard.gameObject.AddComponent<PolarBodyVisualization>();
            body.humanModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Humans/WorkerTPose.fbx");
            if (!body.humanModel) throw new Exception("Existing WorkerTPose model could not be imported.");
            body.bodyMaterial = MaterialAsset("PolarBody", "Universal Render Pipeline/Lit", new Color(.32f,.42f,.49f));
            body.markerMaterial = MaterialAsset("PolarMarker", "Universal Render Pipeline/Unlit", Color.white);
            if (!GameObject.Find("Polar key light")) {
                var go = new GameObject("Polar key light");
                var light = go.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
                go.transform.rotation = Quaternion.Euler(35, -35, 0);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f,.55f,.55f);
            EditorSceneManager.SaveScene(scene);
            // Keep legacy scene entries available but exclude them from this focused application.
            var scenes = EditorBuildSettings.scenes;
            foreach (var entry in scenes) entry.enabled = entry.path == ScenePath;
            EditorBuildSettings.scenes = scenes;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("Polar 3D dashboard configured with existing WorkerTPose model.");
        }
        private static Material MaterialAsset(string name, string shaderName, Color color)
        {
            string path = "Assets/PolarH10/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            var shader = Shader.Find(shaderName);
            if (!shader) throw new Exception("Required existing URP shader not found: " + shaderName);
            material = new Material(shader); material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
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
