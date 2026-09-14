using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using TMPro;
using Michsky.UI.Heat;
using Toggle = UnityEngine.UI.Toggle;

namespace Whisper.Samples
{
    /// <summary>
    /// Record audio clip from microphone and make a transcription.
    /// </summary>
    public class RecordResponses : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public bool streamSegments = true;
        public bool printLanguage = true;
        [Header("UI")] 
        public ButtonManager button;
        public TMP_Text buttonTextHighlighted;
        public TMP_Text buttonTextNormal;
        public TMP_Text outputText;
        public GameObject buttonPanel;
        
        private string _buffer;

        private void Awake()
        {
            whisper.OnNewSegment += OnNewSegment;
            microphoneRecord.OnRecordStop += OnRecordStop;
            button.onClick.AddListener(OnButtonPressed);
        }

        private void OnButtonPressed()
        {
            if (!microphoneRecord.IsRecording)
            {
                microphoneRecord.StartRecord();
                buttonTextNormal.text = "Stop Recording";
                buttonTextHighlighted.text = "Stop Recording";
            }
            else
            {
                microphoneRecord.StopRecord();
                buttonTextNormal.text = "Record Response";
                buttonTextHighlighted.text = "Record Response";
            }
        }
        
        private async void OnRecordStop(AudioChunk recordedAudio)
        {
            buttonTextNormal.text = "Record Response";
            buttonTextHighlighted.text = "Record Response";
            _buffer = "";

            var sw = new Stopwatch();
            sw.Start();
            
            var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
            if (res == null || !outputText) 
                return;

            var time = sw.ElapsedMilliseconds;
            var rate = recordedAudio.Length / (time * 0.001f);

            var text = res.Result;
            if (printLanguage)
                text += $"\n\nLanguage: {res.Language}";
            
            outputText.text = text;
            button.transform.parent.gameObject.SetActive(false);
            buttonPanel.SetActive(true);
        }
        
        private void OnNewSegment(WhisperSegment segment)
        {
            if (!streamSegments || !outputText)
                return;

            _buffer += segment;
            outputText.text = _buffer + "...";
        }
    }
}