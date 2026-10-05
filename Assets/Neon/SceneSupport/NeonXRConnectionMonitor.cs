using UnityEngine;

public sealed class NeonXRConnectionMonitor : MonoBehaviour
{
    [SerializeField] private DebugConsole debugConsole;
    [SerializeField] private float staleAfterSeconds = 2f;
    private readonly NeonXRStream stream = new NeonXRStream();
    private double nextUpdate;
    private double rateStart;
    private long previousCount;
    private float rate;
    public string Status { get; private set; } = "Neon XR: waiting for Core provider";

    private void OnEnable()
    {
        stream.Refresh();
        rateStart = Time.realtimeSinceStartupAsDouble;
        previousCount = stream.SampleCount;
        rate = 0;
    }

    private void Update()
    {
        stream.Refresh();
        double now = Time.realtimeSinceStartupAsDouble;
        if (now < nextUpdate) return;
        nextUpdate = now + 0.25;
        if (now - rateStart >= 1)
        {
            rate = (float)((stream.SampleCount - previousCount) / (now - rateStart));
            rateStart = now;
            previousCount = stream.SampleCount;
        }
        Status = stream.Provider == null ? "Neon XR: waiting for Core provider" :
            stream.IsFresh(staleAfterSeconds) ? $"Neon XR: GAZE DATA RECEIVED · {rate:F0} events/s" :
            "Neon XR: no fresh gaze. Check Companion, LAN and calibration assets.";
        if (debugConsole != null) debugConsole.SetNeonStatus(Status);
    }

    private void OnDisable() => stream.Dispose();
}
