using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugConsole : MonoBehaviour {

    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private string filterString;
    [SerializeField] private int maxTextLength = 1000;
    private string logHistory = "";
    private string neonStatus = "Neon XR status: not started";

    void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type) {
        if (string.IsNullOrWhiteSpace(filterString)
                || logString.IndexOf(filterString, System.StringComparison.OrdinalIgnoreCase) >= 0) {
            logHistory += logString + "\n";
        }
        if (logHistory.Length > maxTextLength) {
            logHistory = logHistory.Substring(logHistory.Length - maxTextLength);
        }

        RefreshText();
    }

    public void SetNeonStatus(string status) {
        neonStatus = status;
        RefreshText();
    }

    private void RefreshText() {
        if (debugText != null)
            debugText.text = neonStatus + "\n\n" + logHistory;
    }
}
