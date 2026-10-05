using System;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class NeonAndroidNetworkBootstrap : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject multicastLock;
#endif

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject appContext = null;
        AndroidJavaObject wifiManager = null;
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                appContext = activity.Call<AndroidJavaObject>("getApplicationContext");
                wifiManager = appContext.Call<AndroidJavaObject>("getSystemService", "wifi");
                multicastLock = wifiManager.Call<AndroidJavaObject>(
                    "createMulticastLock", "PupilLabs.NeonXR");
                multicastLock.Call("setReferenceCounted", false);
                multicastLock.Call("acquire");
                Debug.Log("Neon XR: Android Wi-Fi multicast reception enabled.", this);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Neon XR: could not acquire Android Wi-Fi multicast lock: "
                + exception.Message, this);
            multicastLock?.Dispose();
            multicastLock = null;
        }
        finally
        {
            wifiManager?.Dispose();
            appContext?.Dispose();
        }
#endif
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (multicastLock == null) return;
        try
        {
            if (multicastLock.Call<bool>("isHeld"))
                multicastLock.Call("release");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Neon XR: could not release Android Wi-Fi multicast lock: "
                + exception.Message, this);
        }
        finally
        {
            multicastLock.Dispose();
            multicastLock = null;
        }
#endif
    }
}
