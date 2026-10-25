using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Module3.PolarH10
{
    [Serializable]
    public class PolarRegion
    {
        public string id;
        public string label;
        public Vector2 bodyPosition;
        [NonSerialized] public double rmsUv;
        [NonSerialized] public double rms1sUv, rms2sUv, filteredUv;
        [NonSerialized] public bool hasCalibrationRms, hasDecisionRms, hasFilteredSample, ecgBlanked, signalGap;
        [NonSerialized] public long signalSegment;
        [NonSerialized] public int windowSampleCount, sampleRateHz, ecgBlankingWindowMs;
        [NonSerialized] public double timestampMs;
        [NonSerialized] public float receivedAt = float.NegativeInfinity;
        [NonSerialized] public string deviceName;
        [NonSerialized] public long sequence;
        [NonSerialized] public string sessionId;
        public PolarRegion(string id, string label, Vector2 position)
        { this.id = id; this.label = label; bodyPosition = position; }
    }

    [Serializable]
    public class PolarRmsPacket
    {
        public int version;
        public string type, sessionId, region, sensorName, deviceName, unit;
        public long sequence;
        public double rmsUv;
        public int windowMs;
        public double timestampMs;
        public double rms1sUv, rms2sUv, filteredUv;
        public bool hasCalibrationRms, hasDecisionRms, hasFilteredSample, ecgBlanked, signalGap;
        public long signalSegment;
        public int calibrationWindowMs, decisionWindowMs, windowSampleCount, sampleRateHz, ecgBlankingWindowMs;
    }

    public class PolarRmsReceiver : MonoBehaviour
    {
        private static readonly string[] KnownRegionIds = { "LShoulder", "RShoulder", "MiddleSpine", "LowerSpine",
            "LLowerSpine", "RLowerSpine", "LUpperBack", "RUpperBack", "LFrontShoulder", "RFrontShoulder",
            "LLowerArm", "RLowerArm", "LThigh", "RThigh" };
        // One extra millisecond accommodates rounding of the 130 Hz sample clock.
        public const double MaximumSampleGapMs = 1000.0 / 130.0 + 1.0;
        [Range(1024, 65535)] public int udpPort = 9050;
        [Min(0.25f)] public float staleAfterSeconds = 2f;
        [Tooltip("Display calibration only; does not change the measured RMS.")]
        public float rmsMinimumUv = 0f;
        public float rmsMaximumUv = 120f;
        public PolarRegion[] regions = DefaultRegions();
        public string Status { get; private set; } = "Not listening";
        public int AcceptedPackets { get; private set; }
        // Main-thread callbacks also notify consumers when a location becomes invalid.
        public event Action<PolarRegion> SampleReceived;
        private UdpClient socket;
        private IPEndPoint remote = new IPEndPoint(IPAddress.Loopback, 0);

        private static PolarRegion[] DefaultRegions() => new[] {
            // Preserve the original four indices in existing scenes and consumers.
            new PolarRegion("LShoulder", "Left Shoulder", new Vector2(.33f, .29f)),
            new PolarRegion("RShoulder", "Right Shoulder", new Vector2(.67f, .29f)),
            new PolarRegion("MiddleSpine", "Middle Spine (unmapped)", new Vector2(.5f, .43f)),
            new PolarRegion("LowerSpine", "Lower Spine (unmapped)", new Vector2(.5f, .57f)),
            new PolarRegion("LLowerSpine", "Left Lower Spine", new Vector2(.42f, .57f)),
            new PolarRegion("RLowerSpine", "Right Lower Spine", new Vector2(.58f, .57f)),
            new PolarRegion("LUpperBack", "Left Upper Back", new Vector2(.42f, .40f)),
            new PolarRegion("RUpperBack", "Right Upper Back", new Vector2(.58f, .40f)),
            new PolarRegion("LFrontShoulder", "Left Front Shoulder", new Vector2(.29f, .34f)),
            new PolarRegion("RFrontShoulder", "Right Front Shoulder", new Vector2(.71f, .34f)),
            new PolarRegion("LLowerArm", "Left Forearm", new Vector2(.22f, .52f)),
            new PolarRegion("RLowerArm", "Right Forearm", new Vector2(.78f, .52f)),
            new PolarRegion("LThigh", "Left Thigh", new Vector2(.40f, .75f)),
            new PolarRegion("RThigh", "Right Thigh", new Vector2(.60f, .75f))
        };

        public void EnsureRegions()
        {
            bool complete = regions != null;
            foreach (var id in KnownRegionIds) {
                bool found = false;
                if (regions != null) foreach (var region in regions)
                    if (region != null && region.id == id) { found = true; break; }
                if (!found) { complete = false; break; }
            }
            if (complete) return; // No allocations on the per-sample hot path.
            var current = new List<PolarRegion>(regions ?? new PolarRegion[0]);
            foreach (var expected in DefaultRegions())
                if (!current.Exists(r => r != null && r.id == expected.id)) current.Add(expected);
            regions = current.ToArray();
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool DocumentRegion(string id) => id != "MiddleSpine" && id != "LowerSpine";

        public bool IsLive(PolarRegion region)
        {
            if (region == null || float.IsNegativeInfinity(region.receivedAt)) return false;
            if (Time.realtimeSinceStartup - region.receivedAt < staleAfterSeconds) return true;
            Invalidate(region);
            return false;
        }

        public float Normalized(PolarRegion region) => rmsMaximumUv > rmsMinimumUv
            ? Mathf.Clamp01((float)((region.rmsUv - rmsMinimumUv) / (rmsMaximumUv - rmsMinimumUv))) : 0f;

        private void Invalidate(PolarRegion region)
        {
            region.receivedAt = float.NegativeInfinity;
            region.hasCalibrationRms = region.hasDecisionRms = region.hasFilteredSample = region.ecgBlanked = false;
            region.windowSampleCount = region.ecgBlankingWindowMs = 0;
            region.signalGap = true;
            SampleReceived?.Invoke(region);
        }

        public void ExpireStaleSamples()
        {
            EnsureRegions();
            foreach (var region in regions) if (region != null) IsLive(region);
        }

        private void OnEnable()
        {
            EnsureRegions();
            foreach (var region in regions) if (region != null) { Invalidate(region); region.sessionId = null; }
            try {
                socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, udpPort));
                socket.Client.Blocking = false;
                socket.Client.ReceiveBufferSize = 262144;
                Status = "Listening on localhost:" + udpPort;
            } catch (Exception ex) { socket?.Close(); socket = null; Status = "Receiver error: " + ex.Message; Debug.LogError(Status, this); }
        }

        private void Update()
        {
            ExpireStaleSamples();
            if (socket == null) return;
            // No blocking read, background Unity API calls, or unbounded work per frame.
            for (int i = 0; i < 1024 && socket.Available > 0; i++) {
                try {
                    var bytes = socket.Receive(ref remote);
                    if (bytes.Length > 4096 || !IPAddress.IsLoopback(remote.Address)) continue;
                    AcceptJson(Encoding.UTF8.GetString(bytes));
                } catch (SocketException ex) {
                    if (ex.SocketErrorCode != SocketError.WouldBlock) Status = "UDP error: " + ex.SocketErrorCode;
                    break;
                }
            }
        }

        private static bool ValidWindows(PolarRmsPacket p)
        {
            if (p.hasFilteredSample && !Finite(p.filteredUv)) return false;
            if (p.ecgBlanked && p.ecgBlankingWindowMs != 100) return false;
            if (!p.hasCalibrationRms && !p.hasDecisionRms) return true; // Legacy version 1 display-only packet.
            if (!p.hasFilteredSample || p.signalSegment <= 0 || p.sampleRateHz != 130
                || p.calibrationWindowMs != 1000 || p.decisionWindowMs != 2000
                || p.windowSampleCount < 0 || p.windowSampleCount > 260 || p.signalGap) return false;
            if (p.hasCalibrationRms && (p.windowSampleCount < 130 || !Finite(p.rms1sUv) || p.rms1sUv < 0)) return false;
            if (p.hasDecisionRms && (!p.hasCalibrationRms || p.windowSampleCount < 260 || !Finite(p.rms2sUv) || p.rms2sUv < 0)) return false;
            return true;
        }

        public bool AcceptJson(string json)
        {
            EnsureRegions();
            // Missing extended numeric fields must not silently become valid zero
            // windows when a producer claims hasDecisionRms/hasCalibrationRms.
            var p = new PolarRmsPacket { rmsUv = double.NaN, timestampMs = double.NaN,
                rms1sUv = double.NaN, rms2sUv = double.NaN, filteredUv = double.NaN };
            try { JsonUtility.FromJsonOverwrite(json, p); } catch { return false; }
            if (p.version != 1 || (p.type != "polar_rms" && p.type != "polar_clear")
                || p.windowMs != 200 || p.unit != "uV" || string.IsNullOrEmpty(p.sessionId)
                || string.IsNullOrEmpty(p.deviceName) || p.sequence <= 0
                || !Finite(p.rmsUv) || p.rmsUv < 0 || !Finite(p.timestampMs) || p.timestampMs <= 0) return false;
            // Browser and Unity run on the same computer; delayed UDP must not look live.
            var ageMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - p.timestampMs;
            if (ageMs > staleAfterSeconds * 1000 || ageMs < -2000) return false;
            if (Array.IndexOf(KnownRegionIds, p.region) < 0) return false;
            var region = Array.Find(regions, r => r != null && r.id == p.region);
            if (region == null) return false;
            if (region.sessionId == p.sessionId && p.sequence <= region.sequence) return false;
            if (p.type == "polar_clear") {
                if (region.deviceName != p.deviceName) return false;
                region.sequence = p.sequence; region.sessionId = p.sessionId;
                Invalidate(region);
                return true;
            }
            if (!ValidWindows(p)) return false;
            if (region.sessionId == p.sessionId && region.deviceName == p.deviceName && p.timestampMs <= region.timestampMs) return false;
            // A sensor reassignment cannot activate two body regions simultaneously.
            foreach (var other in regions)
                if (other != null && other != region && other.deviceName == p.deviceName) {
                    other.sequence = p.sequence; other.sessionId = p.sessionId;
                    if (!float.IsNegativeInfinity(other.receivedAt) || other.hasDecisionRms || other.hasCalibrationRms) Invalidate(other);
                }
            var changed = region.sessionId != null && (region.sessionId != p.sessionId || region.deviceName != p.deviceName
                || region.signalSegment != p.signalSegment || region.ecgBlanked != p.ecgBlanked
                || p.timestampMs - region.timestampMs > MaximumSampleGapMs);
            if (changed) Invalidate(region);
            region.rmsUv = p.rmsUv;
            region.rms1sUv = p.hasCalibrationRms ? p.rms1sUv : 0;
            region.rms2sUv = p.hasDecisionRms ? p.rms2sUv : 0;
            region.filteredUv = p.hasFilteredSample ? p.filteredUv : 0;
            region.hasFilteredSample = p.hasFilteredSample;
            region.hasCalibrationRms = DocumentRegion(region.id) && p.hasCalibrationRms;
            region.hasDecisionRms = DocumentRegion(region.id) && p.hasDecisionRms;
            region.ecgBlanked = p.ecgBlanked && p.ecgBlankingWindowMs == 100;
            region.ecgBlankingWindowMs = region.ecgBlanked ? 100 : 0;
            region.signalSegment = p.signalSegment;
            region.signalGap = changed || p.signalGap;
            region.sampleRateHz = p.sampleRateHz;
            region.windowSampleCount = p.windowSampleCount;
            region.timestampMs = p.timestampMs;
            region.deviceName = p.deviceName;
            region.sessionId = p.sessionId;
            region.sequence = p.sequence;
            region.receivedAt = Time.realtimeSinceStartup;
            AcceptedPackets++;
            SampleReceived?.Invoke(region);
            return true;
        }

        private void OnDisable()
        {
            socket?.Close(); socket = null; Status = "Not listening";
            if (regions != null) foreach (var region in regions)
                if (region != null && !float.IsNegativeInfinity(region.receivedAt)) Invalidate(region);
        }
        private void OnValidate()
        {
            if (float.IsNaN(rmsMinimumUv) || float.IsInfinity(rmsMinimumUv)) rmsMinimumUv = 0;
            if (float.IsNaN(rmsMaximumUv) || float.IsInfinity(rmsMaximumUv)) rmsMaximumUv = 120;
            rmsMinimumUv = Mathf.Max(0, rmsMinimumUv);
            rmsMaximumUv = Mathf.Max(rmsMinimumUv + .1f, rmsMaximumUv);
        }
    }
}
