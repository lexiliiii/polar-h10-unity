using System.Globalization;
using UnityEngine;

namespace Module3.PolarH10
{
    [RequireComponent(typeof(PolarRmsReceiver))]
    public class PolarRmsDashboard : MonoBehaviour
    {
        public bool expanded = true;
        private PolarRmsReceiver receiver;
        private GUIStyle title, text, small;
        private string minimum, maximum, rangeError;
        private bool showRange;
        public static readonly Color MissingColor = new Color(.42f, .46f, .51f);
        public static Color SignalColor(float value) => Color.Lerp(new Color(.12f,.65f,.72f), new Color(1f,.43f,.15f), value);

        private void Awake()
        {
            receiver = GetComponent<PolarRmsReceiver>();
            minimum = receiver.rmsMinimumUv.ToString(CultureInfo.InvariantCulture);
            maximum = receiver.rmsMaximumUv.ToString(CultureInfo.InvariantCulture);
        }
        private GUIStyle Style(int size, Color color, bool bold = false)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
            style.normal.textColor = color;
            return style;
        }
        private static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private void OnGUI()
        {
            if (title == null) {
                title = Style(18, Color.white, true);
                text = Style(14, new Color(.9f,.94f,.97f));
                small = Style(12, new Color(.7f,.77f,.82f));
            }
            // Fixed readable logical size at desktop resolutions; shrink only to fit small windows.
            float height = expanded ? (showRange ? 358 : 274) : 42;
            float scale = Mathf.Min(1f, Mathf.Min(Screen.width / 368f, Screen.height / (height + 24)));
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width - 356 * scale, 12 * scale), Quaternion.identity, new Vector3(scale,scale,1));
            try {
                Fill(new Rect(0,0,344,height), new Color(.035f,.055f,.085f,.96f));
                GUI.Label(new Rect(12,8,260,28), "POLAR H10  /  EXG", title);
                if (GUI.Button(new Rect(304,8,28,26), expanded ? "−" : "+")) expanded = !expanded;
                if (!expanded) return;
                GUI.Label(new Rect(12,38,320,22), "200 ms RMS · amplitude, not joint motion", small);
                int liveCount = 0;
                for (int i=0; i<receiver.regions.Length; i++) {
                    var region = receiver.regions[i];
                    bool live = receiver.IsLive(region); if (live) liveCount++;
                    float y = 66 + i*39;
                    Color color = live ? SignalColor(receiver.Normalized(region)) : MissingColor;
                    Fill(new Rect(12,y+5,7,19),color);
                    GUI.Label(new Rect(26,y,135,24),region.label,text);
                    GUI.Label(new Rect(162,y,100,24),live ? region.rmsUv.ToString("F1")+" µV" : "— µV",text);
                    GUI.Label(new Rect(264,y+2,72,22),live ? "LIVE" : region.timestampMs>0 ? "STALE" : "WAITING",small);
                    Fill(new Rect(26,y+27,304,3), new Color(.15f,.2f,.25f));
                    if(live) Fill(new Rect(26,y+27,304*receiver.Normalized(region),3),color);
                }
                GUI.Label(new Rect(12,223,320,22), $"{liveCount}/4 live · Gray = no current data", small);
                if(GUI.Button(new Rect(12,246,320,22),$"Range {receiver.rmsMinimumUv:G}–{receiver.rmsMaximumUv:G} µV  ▾")) showRange=!showRange;
                if(showRange) {
                    GUI.Label(new Rect(12,276,30,24),"Min",small);
                    minimum=GUI.TextField(new Rect(43,276,67,24),minimum,10);
                    GUI.Label(new Rect(117,276,30,24),"Max",small);
                    maximum=GUI.TextField(new Rect(150,276,67,24),maximum,10);
                    if(GUI.Button(new Rect(225,276,107,24),"Apply range")) {
                        if(float.TryParse(minimum,NumberStyles.Float,CultureInfo.InvariantCulture,out float min)
                            && float.TryParse(maximum,NumberStyles.Float,CultureInfo.InvariantCulture,out float max)
                            && !float.IsInfinity(min) && !float.IsInfinity(max) && min>=0 && max>min) {
                            receiver.rmsMinimumUv=min; receiver.rmsMaximumUv=max; rangeError=null;
                        } else rangeError="Use finite values with 0 ≤ Min < Max.";
                    }
                    GUI.Label(new Rect(12,304,320,22),rangeError ?? receiver.Status,small);
                    GUI.Label(new Rect(12,329,320,22),"Display range only; measured RMS is unchanged.",small);
                }
            } finally { GUI.matrix=previous; }
        }
    }
}
