using System;
using System.Linq;
using PupilLabs;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NeonXRProjectSetup
{
    private const string PrefabPath = "Packages/com.pupil-labs.neon-xr.core/Runtime/Prefabs/NeonXR.prefab";
    private const string TestScene = "Assets/_Scenes/EyeTrackingTestRoom.unity";

    [MenuItem("Tools/Neon/XR/Configure Current Scene")]
    public static void ConfigureCurrentScene()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode before configuring Neon.");
        EnsureAddressables();
        var scene = SceneManager.GetActiveScene();
        var providers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<NeonGazeDataProvider>(true)).ToArray();
        if (providers.Length > 1) throw new InvalidOperationException("Scene contains multiple Neon providers. Keep one NeonXR prefab.");
        GameObject root;
        if (providers.Length == 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) throw new InvalidOperationException("Neon XR Core package is not resolved.");
            root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(root, "Add Neon XR Core");
        }
        else root = providers[0].GetComponentInParent<ServiceLocator>().gameObject;

        var provider = root.GetComponentInChildren<NeonGazeDataProvider>(true);
        var serialized = new SerializedObject(provider);
        serialized.FindProperty("simulationEnabled").boolValue = false;
        serialized.ApplyModifiedProperties();
        // Keep network lifetime and Android multicast lock together across scene loads.
        if (!root.GetComponent<NeonAndroidNetworkBootstrap>()) Undo.AddComponent<NeonAndroidNetworkBootstrap>(root);
        var monitor = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<NeonXRConnectionMonitor>(true)).FirstOrDefault();
        if (!monitor) monitor = Undo.AddComponent<NeonXRConnectionMonitor>(root);
        var console = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DebugConsole>(true)).FirstOrDefault();
        var monitorSettings = new SerializedObject(monitor);
        monitorSettings.FindProperty("debugConsole").objectReferenceValue = console;
        monitorSettings.ApplyModifiedProperties();
        if (!scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<NeonGazeDot>(true)))
            Undo.AddComponent<NeonGazeDot>(root);
        // The custom red dot handles data expiry. The stock visualizer retains its last ray forever.
        foreach (var visualizer in root.GetComponentsInChildren<GazeDataVisualizer>(true))
        {
            Undo.RecordObject(visualizer, "Use fresh Neon gaze visualization");
            visualizer.RayVisible = false;
            visualizer.DoRaycast = false;
            visualizer.RaycastPointerVisible = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(visualizer);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Neon XR configured. Build Addressables for the deployment target; use calibrated config.json and calibration.bin in persistentDataPath.");
    }

    private static void EnsureAddressables()
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (!settings.FindGroup("NeonXR Group")) MenuHelpers.ImportGroups();
        foreach (string address in new[] { "pl.config", "pl.config.defaults", "pl.calibration" })
        {
            var entry = settings.groups.Where(g => g != null).SelectMany(g => g.entries).SingleOrDefault(e => e.address == address);
            if (entry == null || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(entry.guid)))
                throw new InvalidOperationException("Missing Neon Addressable: " + address);
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Neon/XR/Build Addressables")]
    public static void BuildAddressables()
    {
        EnsureAddressables();
        AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
        Debug.Log("NEON_ADDRESSABLES_BUILD_PASSED: " + EditorUserBuildSettings.activeBuildTarget);
    }

    // Batch entry point; deliberately saves only the supplied Neon test scene.
    public static void PrepareTestScene()
    {
        EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);
        ConfigureCurrentScene();
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new Exception("Cannot save Neon test scene.");
        Debug.Log("NEON_SCENE_SETUP_PASSED");
    }
}
