using System;
using PupilLabs;
using UnityEngine;

/// <summary>Main-thread subscription to Neon. Old samples never become fallback head gaze.</summary>
public sealed class NeonXRStream : IDisposable
{
    public GazeDataProvider Provider { get; private set; }
    public double LastSampleTime { get; private set; } = double.NegativeInfinity;
    public long SampleCount { get; private set; }
    public Ray LocalRay { get; private set; }
    public EyeState EyeState { get; private set; }
    public Eyelid Eyelid { get; private set; }
    public bool EyeStateAvailable { get; private set; }
    public bool EyelidAvailable { get; private set; }

    public void Refresh(GazeDataProvider explicitProvider = null)
    {
        var next = explicitProvider != null ? explicitProvider :
            (ServiceLocator.Instance != null ? ServiceLocator.Instance.GazeDataProvider : null);
        if (ReferenceEquals(next, Provider)) return;
        Dispose();
        Provider = next;
        if (Provider != null) Provider.gazeDataReady.AddListener(Receive);
    }

    private void Receive(GazeDataProvider data)
    {
        if (data == null || !data.isActiveAndEnabled) return;
        Ray ray = data.GazeRay;
        if (!Finite(ray.origin) || !Finite(ray.direction) || ray.direction.sqrMagnitude < 0.000001f)
        {
            LastSampleTime = double.NegativeInfinity;
            return;
        }
        LocalRay = ray;
        EyeStateAvailable = data.EyeStateAvailable;
        EyelidAvailable = data.EyelidAvailable;
        EyeState = EyeStateAvailable ? data.EyeState : default;
        Eyelid = EyelidAvailable ? data.Eyelid : default;
        LastSampleTime = Time.realtimeSinceStartupAsDouble;
        SampleCount++;
    }

    public bool IsFresh(float timeout) => Provider != null && Provider.isActiveAndEnabled &&
        Time.realtimeSinceStartupAsDouble - LastSampleTime <= Mathf.Max(0.01f, timeout);

    public bool TryGetWorldRay(Camera camera, float timeout, out Ray ray)
    {
        ray = default;
        if (camera == null || !IsFresh(timeout)) return false;
        ray = new Ray(camera.transform.TransformPoint(LocalRay.origin),
            camera.transform.TransformDirection(LocalRay.direction).normalized);
        return true;
    }

    private static bool Finite(Vector3 v) =>
        !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
        !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
        !float.IsNaN(v.z) && !float.IsInfinity(v.z);

    public void Dispose()
    {
        if (Provider != null) Provider.gazeDataReady.RemoveListener(Receive);
        Provider = null;
        LastSampleTime = double.NegativeInfinity;
        EyeStateAvailable = EyelidAvailable = false;
    }
}
