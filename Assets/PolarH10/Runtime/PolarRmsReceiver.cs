using System;
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
    }

    public class PolarRmsReceiver : MonoBehaviour
    {
        [Range(1024, 65535)] public int udpPort = 9050;
        [Min(0.25f)] public float staleAfterSeconds = 2f;
        [Tooltip("Display calibration only; does not change the measured RMS.")]
        public float rmsMinimumUv = 0f;
        public float rmsMaximumUv = 120f;
        public PolarRegion[] regions = {
            new PolarRegion("LShoulder", "Left Shoulder", new Vector2(.33f, .29f)),
            new PolarRegion("RShoulder", "Right Shoulder", new Vector2(.67f, .29f)),
            new PolarRegion("MiddleSpine", "Middle Spine", new Vector2(.5f, .43f)),
            new PolarRegion("LowerSpine", "Lower Spine", new Vector2(.5f, .57f))
        };
        public string Status { get; private set; } = "Not listening";
        public int AcceptedPackets { get; private set; }
        private UdpClient socket;
        private IPEndPoint remote = new IPEndPoint(IPAddress.Loopback, 0);

        public bool IsLive(PolarRegion region) => Time.realtimeSinceStartup - region.receivedAt < staleAfterSeconds;
        public float Normalized(PolarRegion region) => rmsMaximumUv > rmsMinimumUv
            ? Mathf.Clamp01((float)((region.rmsUv - rmsMinimumUv) / (rmsMaximumUv - rmsMinimumUv))) : 0f;

        private void OnEnable()
        {
            foreach (var region in regions) { region.receivedAt = float.NegativeInfinity; region.sessionId = null; }
            try {
                socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, udpPort));
                socket.Client.Blocking = false;
                socket.Client.ReceiveBufferSize = 262144;
                Status = "Listening on localhost:" + udpPort;
            } catch (Exception ex) { socket?.Close(); socket = null; Status = "Receiver error: " + ex.Message; Debug.LogError(Status, this); }
        }

        private void Update()
        {
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

        public bool AcceptJson(string json)
        {
            PolarRmsPacket p;
            try { p = JsonUtility.FromJson<PolarRmsPacket>(json); } catch { return false; }
            if (p == null || p.version != 1 || (p.type != "polar_rms" && p.type != "polar_clear")
                || p.windowMs != 200 || p.unit != "uV" || string.IsNullOrEmpty(p.sessionId)
                || string.IsNullOrEmpty(p.deviceName) || p.sequence <= 0
                || double.IsNaN(p.rmsUv) || double.IsInfinity(p.rmsUv) || p.rmsUv < 0
                || double.IsNaN(p.timestampMs) || double.IsInfinity(p.timestampMs) || p.timestampMs <= 0) return false;
            // Browser and Unity run on the same computer; delayed UDP must not look live.
            var ageMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - p.timestampMs;
            if (ageMs > staleAfterSeconds * 1000 || ageMs < -2000) return false;
            var region = Array.Find(regions, r => r.id == p.region);
            if (region == null) return false;
            if (region.sessionId == p.sessionId && p.sequence <= region.sequence) return false;
            if (p.type == "polar_clear") {
                if (region.deviceName != p.deviceName) return false;
                region.receivedAt = float.NegativeInfinity;
                region.sequence = p.sequence;
                region.sessionId = p.sessionId;
                return true;
            }
            // A sensor reassignment cannot activate two body regions simultaneously.
            foreach (var other in regions)
                if (other != region && other.deviceName == p.deviceName) other.receivedAt = float.NegativeInfinity;
            region.rmsUv = p.rmsUv;
            region.timestampMs = p.timestampMs;
            region.deviceName = p.deviceName;
            region.sessionId = p.sessionId;
            region.sequence = p.sequence;
            region.receivedAt = Time.realtimeSinceStartup;
            AcceptedPackets++;
            return true;
        }

        private void OnDisable() { socket?.Close(); socket = null; Status = "Not listening"; }
        private void OnValidate()
        {
            if (float.IsNaN(rmsMinimumUv) || float.IsInfinity(rmsMinimumUv)) rmsMinimumUv = 0;
            if (float.IsNaN(rmsMaximumUv) || float.IsInfinity(rmsMaximumUv)) rmsMaximumUv = 120;
            rmsMinimumUv = Mathf.Max(0, rmsMinimumUv);
            rmsMaximumUv = Mathf.Max(rmsMinimumUv + .1f, rmsMaximumUv);
        }
    }
}
