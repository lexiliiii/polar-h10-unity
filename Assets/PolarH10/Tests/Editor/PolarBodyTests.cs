using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Module3.PolarH10.Tests
{
    public class PolarBodyTests
    {
        [UnityTest]
        public IEnumerator ExistingHumanShowsIndependentRmsAndStaleMarkers()
        {
            EditorSceneManager.OpenScene("Assets/PolarH10/Scenes/PolarRmsDashboard.unity");
            yield return new EnterPlayMode();
            yield return null;
            var body=UnityEngine.Object.FindObjectOfType<PolarBodyVisualization>();
            Assert.NotNull(body);
            Assert.IsNull(body.SetupStatus);
            var receiver=body.GetComponent<PolarRmsReceiver>();
            Assert.That(body.GetComponentsInChildren<Animator>()[0].enabled,Is.False);
            Assert.That(body.GetComponentsInChildren<SkinnedMeshRenderer>().Length,Is.GreaterThan(0));
            string[] ids={"LShoulder","RShoulder","MiddleSpine","LowerSpine"};
            var block=new MaterialPropertyBlock();
            for(int i=0;i<4;i++) {
                var marker=body.transform.Find(ids[i]+" RMS");
                Assert.NotNull(marker,ids[i]);
                marker.GetComponent<Renderer>().GetPropertyBlock(block);
                Assert.That(Vector4.Distance(PolarRmsDashboard.MissingColor,block.GetColor("_BaseColor")),Is.LessThan(.0001f));
                var p=new PolarRmsPacket { version=1,type="polar_rms",sessionId="visual-test",sequence=i+1,
                    region=ids[i],deviceName="test-device-"+i,rmsUv=i*40,windowMs=200,unit="uV",
                    timestampMs=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                Assert.True(receiver.AcceptJson(JsonUtility.ToJson(p)));
            }
            yield return null;
            for(int i=0;i<4;i++) {
                body.transform.Find(ids[i]+" RMS").GetComponent<Renderer>().GetPropertyBlock(block);
                Assert.That(Vector4.Distance(PolarRmsDashboard.SignalColor(i/3f),block.GetColor("_BaseColor")),Is.LessThan(.0001f));
            }
            receiver.regions[0].receivedAt=float.NegativeInfinity;
            yield return null;
            body.transform.Find(ids[0]+" RMS").GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.That(Vector4.Distance(PolarRmsDashboard.MissingColor,block.GetColor("_BaseColor")),Is.LessThan(.0001f));
            Assert.True(receiver.IsLive(receiver.regions[1]));
            // Optional real GPU preview of the imported mesh and four synthetic amplitude levels.
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) {
                var camera=Array.Find(body.GetComponentsInChildren<Camera>(), c=>c.enabled);
                var texture=new RenderTexture(1000,900,24);
                var oldRect=camera.rect; camera.rect=new Rect(0,0,1,1); camera.targetTexture=texture;
                camera.Render();
                var previous=RenderTexture.active; RenderTexture.active=texture;
                var png=new Texture2D(1000,900,TextureFormat.RGB24,false);
                png.ReadPixels(new Rect(0,0,1000,900),0,0); png.Apply();
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"polar-body-preview.png"),png.EncodeToPNG());
                RenderTexture.active=previous; camera.targetTexture=null; camera.rect=oldRect;
                UnityEngine.Object.Destroy(png); UnityEngine.Object.Destroy(texture);
            }
            yield return new ExitPlayMode();
        }
    }
}
