using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Module3.Neon
{
    /// <summary>Starts the project's gaze-only Python bridge when Play mode begins.</summary>
    public sealed class NeonGazeBridgeProcess : MonoBehaviour
    {
        [SerializeField] private string companionIp = "";
        [SerializeField] private int unityPort = 9053;
        [SerializeField] private bool autoStart = true;
        public string Status { get; private set; } = "Not started";

        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private Process process;

        private void OnEnable()
        {
            if (autoStart) StartBridge();
        }

        public void StartBridge()
        {
            if (process != null && !process.HasExited) return;
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string bridge = Path.Combine(projectRoot, "Tools", "NeonBridge", "gaze_bridge.py");
            string venv = Path.Combine(projectRoot, "Tools", "NeonBridge", ".venv");
            string python = Application.platform == RuntimePlatform.WindowsEditor ||
                            Application.platform == RuntimePlatform.WindowsPlayer
                ? Path.Combine(venv, "Scripts", "python.exe")
                : Path.Combine(venv, "bin", "python");
            if (!File.Exists(python) || !File.Exists(bridge))
            {
                Status = "Missing Neon bridge Python environment or gaze_bridge.py";
                Debug.LogError("[NeonGaze] " + Status, this);
                return;
            }

            string arguments = Quote(bridge) + " --port " + unityPort;
            if (!string.IsNullOrWhiteSpace(companionIp)) arguments += " --ip " + Quote(companionIp.Trim());
            try
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = python,
                        Arguments = arguments,
                        WorkingDirectory = Path.GetDirectoryName(bridge),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };
                process.OutputDataReceived += (_, args) => Enqueue(args.Data);
                process.ErrorDataReceived += (_, args) => Enqueue(args.Data);
                process.Exited += (_, __) => Enqueue("Bridge exited with code " + process.ExitCode);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                Status = "Starting gaze-only bridge";
                Debug.Log("[NeonGaze] " + Status, this);
            }
            catch (Exception exception)
            {
                Status = "Bridge start failed: " + exception.Message;
                Debug.LogError("[NeonGaze] " + Status, this);
                DisposeProcess();
            }
        }

        private void Update()
        {
            while (messages.TryDequeue(out string message))
            {
                Status = message;
                Debug.Log("[NeonGaze bridge] " + message, this);
            }
        }

        private void Enqueue(string message)
        {
            if (!string.IsNullOrWhiteSpace(message)) messages.Enqueue(message);
        }

        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

        private void OnDisable()
        {
            if (process != null)
            {
                try { if (!process.HasExited) process.Kill(); }
                catch (InvalidOperationException) { }
            }
            DisposeProcess();
            Status = "Stopped";
        }

        private void DisposeProcess()
        {
            process?.Dispose();
            process = null;
        }
    }
}
