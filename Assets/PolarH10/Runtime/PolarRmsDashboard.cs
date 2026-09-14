using System.Globalization;
using UnityEngine;

namespace Module3.PolarH10
{
    [RequireComponent(typeof(PolarRmsReceiver))]
    public class PolarRmsDashboard : MonoBehaviour
    {
        public bool expanded = true;
        private PolarRmsReceiver receiver;
        private Texture2D body, circle;
        private GUIStyle title, heading, text, number, small;
        private string minimum, maximum;
        private readonly Color muted = new Color(.51f, .62f, .70f);
        private readonly Color low = new Color(.12f, .28f, .35f);
        private readonly Color high = new Color(1f, .48f, .18f);

        private void Awake()
        {
            receiver = GetComponent<PolarRmsReceiver>();
            if (expanded && Camera.allCamerasCount == 0) {
                var cameraObject = new GameObject("Dashboard Camera");
                cameraObject.transform.SetParent(transform);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f,.055f,.085f);
                camera.cullingMask = 0;
            }
            minimum = receiver.rmsMinimumUv.ToString(CultureInfo.InvariantCulture);
            maximum = receiver.rmsMaximumUv.ToString(CultureInfo.InvariantCulture);
            body = MakeBody();
            circle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                circle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(32 - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32)))));
            circle.Apply();
        }

        private GUIStyle Style(int size, Color color, FontStyle font = FontStyle.Normal)
        { var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = font }; s.normal.textColor = color; return s; }
        private void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        private void Dot(Vector2 p, float size, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),circle); GUI.color=old; }

        private void OnGUI()
        {
            if (title == null) {
                title = Style(34, Color.white, FontStyle.Bold); heading = Style(20, Color.white, FontStyle.Bold);
                text = Style(16, new Color(.80f,.86f,.90f)); number = Style(30, Color.white, FontStyle.Bold); small = Style(13, muted);
            }
            if (!expanded) {
                if (GUI.Button(new Rect(Screen.width - 205, 18, 185, 38), "Polar H10 dashboard")) expanded = true;
                return;
            }
            var oldMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2, (Screen.height-800*scale)/2), Quaternion.identity, new Vector3(scale,scale,1));
            Fill(new Rect(0,0,1280,800), new Color(.035f,.055f,.085f));
            GUI.Label(new Rect(42,24,800,28), "POLAR H10   /   PHYSIOLOGY DASHBOARD", small);
            GUI.Label(new Rect(40,54,950,48), "EXG activation · back view", title);
            GUI.Label(new Rect(42,107,1000,30), "High-pass filtered  /  200 ms rolling RMS  /  voltage in µV", text);
            if (GUI.Button(new Rect(1150,32,90,32), "Minimize")) expanded = false;
            Fill(new Rect(40,154,680,560), new Color(.065f,.09f,.125f));
            GUI.Label(new Rect(62,170,300,30), "POSTERIOR VIEW", small);
            GUI.DrawTexture(new Rect(210,196,330,500), body);
            // The spine line establishes orientation; left is the person's left in back view.
            Fill(new Rect(373,325,3,164), new Color(.27f,.36f,.43f));
            int liveCount = 0;
            for (int i=0; i<receiver.regions.Length; i++) {
                var region = receiver.regions[i]; bool live = receiver.IsLive(region); if(live) liveCount++;
                Color c = live ? Color.Lerp(low,high,receiver.Normalized(region)) : new Color(.24f,.29f,.34f);
                Vector2 p = new Vector2(210 + region.bodyPosition.x*330,196+region.bodyPosition.y*500);
                Dot(p,54,new Color(c.r,c.g,c.b,.2f)); Dot(p,31,c);
                bool left = i == 0 || i == 2;
                float x = left ? 65 : 498;
                float y = i < 2 ? 284 : (i == 2 ? 404 : 480);
                Fill(new Rect(left ? x+142 : p.x+18,p.y,left ? Mathf.Max(1,p.x-18-x-142) : Mathf.Max(1,x-p.x-18),1), muted);
                GUI.Label(new Rect(x,y,180,28),region.label,text);
                GUI.Label(new Rect(x,y+27,180,36), live ? region.rmsUv.ToString("F1")+" µV" : "—", heading);
                Fill(new Rect(748,154+i*118,492,104),new Color(.08f,.11f,.15f));
                Dot(new Vector2(775,181+i*118),10,c);
                GUI.Label(new Rect(794,163+i*118,250,30),region.label,heading);
                GUI.Label(new Rect(770,197+i*118,240,45),live ? region.rmsUv.ToString("F1")+" µV" : "— µV",number);
                GUI.Label(new Rect(1070,176+i*118,160,27),live ? "LIVE" : region.timestampMs > 0 ? "STALE" : "WAITING",small);
                Fill(new Rect(1068,215+i*118,140,7),low);
                if(live) Fill(new Rect(1068,215+i*118,140*receiver.Normalized(region),7),c);
            }
            GUI.Label(new Rect(765,636,450,26),"Color sensitivity · adjustable µV range",text);
            GUI.Label(new Rect(765,675,45,26),"Min",small);
            minimum = GUI.TextField(new Rect(805,673,83,30),minimum,10);
            GUI.Label(new Rect(910,675,45,26),"Max",small);
            maximum = GUI.TextField(new Rect(950,673,83,30),maximum,10);
            if(GUI.Button(new Rect(1052,673,160,30),"Apply range")) {
                if(float.TryParse(minimum,NumberStyles.Float,CultureInfo.InvariantCulture,out float min)
                    && float.TryParse(maximum,NumberStyles.Float,CultureInfo.InvariantCulture,out float max)
                    && !float.IsInfinity(min) && !float.IsInfinity(max) && min>=0 && max>min) {
                    receiver.rmsMinimumUv=min; receiver.rmsMaximumUv=max;
                }
            }
            GUI.Label(new Rect(42,735,720,26),receiver.Status+"   ·   "+liveCount+" / "+receiver.regions.Length+" regions live",text);
            GUI.Label(new Rect(765,729,490,26),$"Applied: {receiver.rmsMinimumUv:G}–{receiver.rmsMaximumUv:G} µV",small);
            GUI.Label(new Rect(42,767,1170,22),"Gray = no current data. Colors show calibrated RMS amplitude. Numerical measurements are never normalized.",small);
            GUI.matrix=oldMatrix;
        }

        private Texture2D MakeBody()
        {
            // Symmetrical back silhouette, generated locally without asset or render-pipeline dependencies.
            Vector2[] outline = {
                new Vector2(.42f,.18f),new Vector2(.30f,.21f),new Vector2(.23f,.27f),new Vector2(.17f,.43f),new Vector2(.10f,.57f),
                new Vector2(.12f,.63f),new Vector2(.18f,.62f),new Vector2(.24f,.47f),new Vector2(.28f,.35f),new Vector2(.32f,.49f),
                new Vector2(.28f,.63f),new Vector2(.32f,.77f),new Vector2(.32f,.96f),new Vector2(.43f,.96f),new Vector2(.46f,.77f),
                new Vector2(.50f,.68f),new Vector2(.54f,.77f),new Vector2(.57f,.96f),new Vector2(.68f,.96f),new Vector2(.68f,.77f),
                new Vector2(.72f,.63f),new Vector2(.68f,.49f),new Vector2(.72f,.35f),new Vector2(.76f,.47f),new Vector2(.82f,.62f),
                new Vector2(.88f,.63f),new Vector2(.90f,.57f),new Vector2(.83f,.43f),new Vector2(.77f,.27f),new Vector2(.70f,.21f),new Vector2(.58f,.18f)
            };
            var t = new Texture2D(330,500,TextureFormat.RGBA32,false);
            for(int y=0;y<500;y++) for(int x=0;x<330;x++) {
                Vector2 p = new Vector2(x/330f,1-y/500f); bool inside=false;
                for(int i=0,j=outline.Length-1;i<outline.Length;j=i++) {
                    var a=outline[i]; var b=outline[j];
                    if((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
                }
                bool head = Mathf.Pow((p.x-.5f)/.105f,2)+Mathf.Pow((p.y-.095f)/.085f,2)<1;
                bool neck = p.x>.44f && p.x<.56f && p.y>.15f && p.y<.23f;
                t.SetPixel(x,y,inside||head||neck ? new Color(.20f,.28f,.35f) : Color.clear);
            }
            t.Apply(); return t;
        }
        private void OnDestroy() { if(body) Destroy(body); if(circle) Destroy(circle); }
    }
}
