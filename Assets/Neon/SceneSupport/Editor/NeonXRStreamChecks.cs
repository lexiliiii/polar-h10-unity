using System;
using PupilLabs;
using UnityEditor;
using UnityEngine;

// Runs in Edit mode without sockets or hardware; real device validation remains separate.
public sealed class NeonXRTestProvider : GazeDataProvider
{
    public Ray TestRay = new Ray(Vector3.zero, Vector3.forward);
    public override Ray GazeRay => TestRay;
    public override Vector3 RawGazeDir => TestRay.direction;
    public override Vector2 RawGazePoint => Vector2.zero;
    public override bool EyeStateAvailable => false;
    public override EyeState RawEyeState => default;
    public override bool EyelidAvailable => false;
    public override Eyelid RawEyelid => default;
}

public static class NeonXRStreamChecks
{
    [MenuItem("Tools/Neon/XR/Run Stream Checks")]
    public static void Run()
    {
        var go = new GameObject("Neon stream check");
        var cameraObject = new GameObject("Neon camera check");
        var stream = new NeonXRStream();
        try
        {
            var provider = go.AddComponent<NeonXRTestProvider>();
            provider.gazeDataReady = new PupilLabs.Serializable.GazeDataProviderEvent();
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(3, 2, 1), Quaternion.Euler(0, 90, 0));
            stream.Refresh(provider);
            Require(!stream.IsFresh(1), "No gaze before first event");
            provider.gazeDataReady.Invoke(provider);
            Require(stream.TryGetWorldRay(camera, 1, out Ray ray), "Accept gaze");
            Require(Vector3.Distance(ray.origin, camera.transform.position) < 0.001f &&
                Vector3.Dot(ray.direction, Vector3.right) > 0.999f, "Camera-local to world conversion");
            stream.Refresh(provider);
            provider.gazeDataReady.Invoke(provider);
            Require(stream.SampleCount == 2, "No duplicate subscription");
            System.Threading.Thread.Sleep(30);
            Require(!stream.IsFresh(0.01f), "Reject expired gaze");
            stream.Dispose();
            provider.gazeDataReady.Invoke(provider);
            Require(stream.SampleCount == 2 && !stream.IsFresh(1), "Unsubscribe and clear state");
            stream.Refresh(provider);
            provider.gazeDataReady.Invoke(provider);
            Require(stream.SampleCount == 3 && stream.IsFresh(1), "Resubscribe after disable");
            provider.TestRay = new Ray(Vector3.zero, Vector3.zero);
            provider.gazeDataReady.Invoke(provider);
            Require(!stream.IsFresh(1), "Reject zero direction");
            provider.TestRay = new Ray(new Vector3(float.NaN, 0, 0), Vector3.forward);
            provider.gazeDataReady.Invoke(provider);
            Require(!stream.IsFresh(1), "Reject non-finite ray");
            Debug.Log("NEON_STREAM_CHECKS_PASSED (9 assertions)");
        }
        finally
        {
            stream.Dispose();
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Neon check failed: " + message);
    }

    public static void ValidateAndPrepare()
    {
        Run();
        NeonXRProjectSetup.PrepareTestScene();
        NeonXRProjectSetup.BuildAddressables();
    }
}

