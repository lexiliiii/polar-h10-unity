using System;
using NUnit.Framework;
using UnityEngine;

namespace Module3.PolarH10.Tests
{
    public class PolarReceiverTests
    {
        private GameObject go;
        private PolarRmsReceiver receiver;
        [SetUp] public void Setup() { go=new GameObject(); go.SetActive(false); receiver=go.AddComponent<PolarRmsReceiver>(); }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(go); }
        private PolarRmsPacket Packet(string region="LShoulder",long sequence=1) => new PolarRmsPacket {
            version=1,type="polar_rms",sessionId="test",sequence=sequence,region=region,deviceName="H10 ABC",
            rmsUv=42.3,windowMs=200,unit="uV",timestampMs=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        [Test] public void NormalizationDoesNotChangeMeasurement()
        {
            Assert.True(receiver.AcceptJson(JsonUtility.ToJson(Packet())));
            receiver.rmsMinimumUv=20;receiver.rmsMaximumUv=60;
            Assert.That(receiver.Normalized(receiver.regions[0]),Is.EqualTo(.5575f).Within(.0001f));
            Assert.AreEqual(42.3,receiver.regions[0].rmsUv);
            receiver.rmsMaximumUv=30;Assert.AreEqual(1,receiver.Normalized(receiver.regions[0]));
        }
        [Test] public void DelayedDuplicateAndUnsupportedPacketsAreRejected()
        {
            var p=Packet(); Assert.True(receiver.AcceptJson(JsonUtility.ToJson(p)));
            Assert.False(receiver.AcceptJson(JsonUtility.ToJson(p)));
            p=Packet("Chest",2);Assert.False(receiver.AcceptJson(JsonUtility.ToJson(p)));
            p=Packet(sequence:3);p.timestampMs-=10000;Assert.False(receiver.AcceptJson(JsonUtility.ToJson(p)));
            p=Packet(sequence:4);p.windowMs=400;Assert.False(receiver.AcceptJson(JsonUtility.ToJson(p)));
        }
        [Test] public void MovingOneDeviceClearsPreviousLocation()
        {
            Assert.True(receiver.AcceptJson(JsonUtility.ToJson(Packet())));
            Assert.True(receiver.IsLive(receiver.regions[0]));
            Assert.True(receiver.AcceptJson(JsonUtility.ToJson(Packet("LowerSpine",2))));
            Assert.False(receiver.IsLive(receiver.regions[0]));Assert.True(receiver.IsLive(receiver.regions[3]));
        }
    }
}
