using System;
using UnityEngine;

namespace Module3.PolarH10
{
    /// <summary>Calibrated effort markers on a reference pose. No motion or pose is inferred from EXG.</summary>
    [RequireComponent(typeof(PolarRmsReceiver))]
    public class PolarBodyVisualization : MonoBehaviour
    {
        public GameObject humanModel;
        public Material bodyMaterial;
        public Material markerMaterial;
        public string[] boneNames = { "Bip01 L UpperArm", "Bip01 R UpperArm", "Bip01 Spine1", "Bip01 Spine" };
        private static readonly string[] RegionIds = { "LShoulder", "RShoulder", "MiddleSpine", "LowerSpine", "LLowerSpine", "RLowerSpine", "LUpperBack", "RUpperBack", "LFrontShoulder", "RFrontShoulder", "LLowerArm", "RLowerArm", "LThigh", "RThigh" };
        private PolarThresholdCoach coach;
        private PolarRmsReceiver receiver;
        private readonly Transform[] anchors = new Transform[14];
        private readonly Transform[] markers = new Transform[14];
        private readonly Renderer[] renderers = new Renderer[14];
        private MaterialPropertyBlock properties;
        private GameObject model;
        private Camera bodyCamera;
        private Vector3 back, center;
        private Bounds bounds;
        private float bodyHeight;
        public string SetupStatus { get; private set; }
        // Read-only references for optional sensor consumers.
        public Camera BodyCamera => bodyCamera;
        public GameObject Model => model;
        public System.Collections.Generic.IReadOnlyList<Transform> MarkerTransforms => markers;

        private void Start()
        {
            receiver=GetComponent<PolarRmsReceiver>();
            coach=GetComponent<PolarThresholdCoach>();
            properties=new MaterialPropertyBlock();
            if(!humanModel || !bodyMaterial || !markerMaterial) {
                SetupStatus="Assign a human model and visualization materials in the Inspector.";
                Debug.LogError(SetupStatus,this); return;
            }
            model=Instantiate(humanModel,transform);
            model.name="Polar human · reference pose";
            foreach(var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled=false;
            foreach(var animation in model.GetComponentsInChildren<Animation>(true)) animation.enabled=false;
            foreach(var camera in model.GetComponentsInChildren<Camera>(true)) camera.enabled=false;
            foreach(var light in model.GetComponentsInChildren<Light>(true)) light.enabled=false;
            // This FBX includes four visible twist helpers; they are rig guides, not body surfaces.
            foreach(var mesh in model.GetComponentsInChildren<Renderer>())
                if(mesh.name=="L UpperArm Twist01" || mesh.name=="L UpperArm Twist02"
                    || mesh.name=="R UpperArm Twist01" || mesh.name=="R UpperArm Twist02") mesh.enabled=false;
            var meshes=Array.FindAll(model.GetComponentsInChildren<Renderer>(),r=>r.enabled);
            if(meshes.Length==0) { SetupStatus="Human model has no visible mesh."; Debug.LogError(SetupStatus,this); return; }
            bounds=meshes[0].bounds;
            foreach(var mesh in meshes) {
                bounds.Encapsulate(mesh.bounds);
                var materials=mesh.sharedMaterials;
                for(int i=0;i<materials.Length;i++) materials[i]=bodyMaterial;
                mesh.sharedMaterials=materials;
            }
            center=bounds.center; bodyHeight=bounds.size.y;
            var bones=model.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<4;i++) {
                anchors[i]=i<boneNames.Length ? Array.Find(bones,t=>t.name==boneNames[i]) : null;
                if(!anchors[i]) { SetupStatus="Missing bone for "+RegionIds[i]; Debug.LogError(SetupStatus,this); return; }
            }
            // Derive the posterior view from anatomical left/right, independent of FBX facing axis.
            back=Vector3.Cross(Vector3.up, anchors[1].position-anchors[0].position).normalized;
            if(back.sqrMagnitude<.5f) { SetupStatus="Cannot determine model posterior direction."; return; }
            for(int i=4;i<RegionIds.Length;i++) {
                string side=RegionIds[i][0]=='L'?"L":"R";
                string bone=RegionIds[i].Contains("Thigh")?"Bip01 "+side+" Thigh":RegionIds[i].Contains("LowerArm")?"Bip01 "+side+" Forearm":RegionIds[i].Contains("LowerSpine")?boneNames[3]:RegionIds[i].Contains("UpperBack")?boneNames[2]:"Bip01 "+side+" UpperArm";
                anchors[i]=Array.Find(bones,t=>t.name==bone) ?? anchors[RegionIds[i][0]=='L'?0:1];
            }
            for(int i=0;i<RegionIds.Length;i++) {
                var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name=RegionIds[i]+" RMS";
                marker.transform.SetParent(transform);
                Destroy(marker.GetComponent<Collider>());
                markers[i]=marker.transform;
                renderers[i]=marker.GetComponent<Renderer>();
                renderers[i].sharedMaterial=markerMaterial;
            }
            var cameraObject=new GameObject("Polar posterior camera");
            cameraObject.transform.SetParent(transform);
            bodyCamera=cameraObject.AddComponent<Camera>();
            bodyCamera.clearFlags=CameraClearFlags.SolidColor;
            bodyCamera.backgroundColor=new Color(.035f,.055f,.085f);
            bodyCamera.orthographic=true;
            bodyCamera.transform.position=center+back*Mathf.Max(5,bodyHeight*3);
            bodyCamera.transform.LookAt(center,Vector3.up);
            bodyCamera.nearClipPlane=.01f;
            bodyCamera.farClipPlane=Mathf.Max(100,bodyHeight*10);
            SetupStatus=null;
        }
        private void LateUpdate()
        {
            if(!bodyCamera) return;
            // Reserve the panel area on landscape screens; keep the body centered below it on portrait screens.
            float panelWidth=Mathf.Min(464,Screen.width*.45f);
            bodyCamera.rect=Screen.width>=900 ? new Rect(0,0,1-panelWidth/Screen.width,1) : new Rect(0,0,1,Mathf.Max(.25f,1-290f/Screen.height));
            float horizontalRadius=0;
            Vector3 right=bodyCamera.transform.right;
            var e=bounds.extents;
            horizontalRadius=Mathf.Abs(right.x)*e.x+Mathf.Abs(right.y)*e.y+Mathf.Abs(right.z)*e.z;
            bodyCamera.orthographicSize=Mathf.Max(bodyHeight*.58f, horizontalRadius*1.12f/Mathf.Max(.1f,bodyCamera.aspect));
            for(int i=0;i<RegionIds.Length;i++) {
                bool active=coach && coach.ActiveChannel(RegionIds[i]);
                markers[i].gameObject.SetActive(active);
                if(!active) continue;
                var region=Array.Find(receiver.regions,r=>r.id==RegionIds[i]);
                bool live=region!=null && receiver.IsLive(region);
                double percent=coach.Percent(RegionIds[i]);
                float value=double.IsNaN(percent)?0:Mathf.Clamp01((float)percent/100);
                // Overlay on the posterior bounding plane to keep every anatomical marker visible.
                float depth=Mathf.Abs(back.x)*bounds.extents.x+Mathf.Abs(back.z)*bounds.extents.z;
                Vector3 p=anchors[i].position;
                if(i>=4 && (RegionIds[i].Contains("LowerSpine") || RegionIds[i].Contains("UpperBack"))) p+=bodyCamera.transform.right*(RegionIds[i][0]=='L'?-1:1)*bodyHeight*.045f;
                if(RegionIds[i].Contains("FrontShoulder")) p+=Vector3.down*bodyHeight*.045f;
                markers[i].position=p+back*(depth-Vector3.Dot(p-center,back)+bodyHeight*.025f);
                markers[i].localScale=Vector3.one*bodyHeight*(.027f+.018f*value);
                Color color=coach.ColorFor(RegionIds[i]);
                properties.SetColor("_BaseColor",color); properties.SetColor("_Color",color);
                renderers[i].SetPropertyBlock(properties);
            }
        }
        private void OnGUI()
        {
            GUI.Label(new Rect(16,Screen.height-48,Screen.width-32,44),SetupStatus ?? "Back view · L                                      R\nMuscle Effort · % of max; reference pose, no joint motion measured.");
        }
        private void OnDestroy()
        {
            if(model) Destroy(model);
            if(bodyCamera) Destroy(bodyCamera.gameObject);
            foreach(var marker in markers) if(marker) Destroy(marker.gameObject);
        }
    }
}
