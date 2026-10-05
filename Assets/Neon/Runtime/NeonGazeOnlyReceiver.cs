using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Module3.Neon
{
    /// <summary>Asynchronous localhost receiver for gaze-only Neon samples.</summary>
    public sealed class NeonGazeOnlyReceiver : MonoBehaviour
    {
        [SerializeField] private int udpPort = 9053;
        [SerializeField, Min(0.05f)] private float maximumPacketAgeSeconds = 0.5f;
        public event Action<NeonRawGazeSample> SampleReceived;
        public string Status { get; private set; } = "Stopped";
        public long AcceptedSamples { get; private set; }
        public long RejectedSamples { get; private set; }

        private readonly ConcurrentQueue<string> pending = new ConcurrentQueue<string>();
        private UdpClient socket;
        private string activeStream;
        private long lastSequence;

        private void OnEnable()
        {
            activeStream = null;
            lastSequence = 0;
            try
            {
                socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, udpPort));
                socket.Client.ReceiveBufferSize = 262144;
                Status = "Listening on localhost:" + udpPort;
                BeginReceive();
            }
            catch (Exception exception)
            {
                Status = "UDP listen failed: " + exception.Message;
                Debug.LogError("[NeonGaze] " + Status, this);
                CloseSocket();
            }
        }

        private void BeginReceive()
        {
            try { socket?.BeginReceive(ReceiveCompleted, socket); }
            catch (ObjectDisposedException) { }
            catch (Exception exception)
            {
                Status = "UDP receive failed: " + exception.Message;
            }
        }

        private void ReceiveCompleted(IAsyncResult result)
        {
            var client = result.AsyncState as UdpClient;
            if (client == null || client != socket) return;
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = client.EndReceive(result, ref endpoint);
                if (IPAddress.IsLoopback(endpoint.Address) && bytes.Length <= 4096)
                    pending.Enqueue(Encoding.UTF8.GetString(bytes));
            }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { }
            finally
            {
                if (client == socket) BeginReceive();
            }
        }

        private void Update()
        {
            // JSON parsing and all event subscribers run on Unity's main thread.
            int handled = 0;
            while (handled++ < 512 && pending.TryDequeue(out string json))
                Accept(json, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        internal bool Accept(string json, double nowMs)
        {
            NeonRawGazeSample sample;
            try { sample = JsonUtility.FromJson<NeonRawGazeSample>(json); }
            catch { return Reject(); }

            if (sample == null || sample.version != 1 || sample.type != "neon_raw_gaze" ||
                string.IsNullOrEmpty(sample.streamId) || sample.sequence <= 0 ||
                !long.TryParse(sample.timestampNs, NumberStyles.None, CultureInfo.InvariantCulture, out long timestamp) ||
                timestamp <= 0 || sample.sourceWidth <= 0 || sample.sourceHeight <= 0 ||
                !Finite(sample.gazeX) || !Finite(sample.gazeY) || !Finite(sample.hostSentMs) ||
                nowMs - sample.hostSentMs > maximumPacketAgeSeconds * 1000 ||
                sample.hostSentMs - nowMs > 100)
                return Reject();

            bool sameStream = activeStream == sample.streamId;
            if (sameStream && sample.sequence <= lastSequence) return Reject();
            activeStream = sample.streamId;
            lastSequence = sample.sequence;
            AcceptedSamples++;
            Status = sample.worn ? "Receiving live Neon gaze" : "Neon connected; glasses not worn";
            SampleReceived?.Invoke(sample);
            return true;
        }

        private bool Reject() { RejectedSamples++; return false; }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private void OnDisable()
        {
            CloseSocket();
            while (pending.TryDequeue(out _)) { }
            Status = "Stopped";
        }

        private void CloseSocket()
        {
            var closing = socket;
            socket = null;
            closing?.Close();
        }
    }
}
