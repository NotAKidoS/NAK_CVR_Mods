using System.Collections;
using UnityEngine;
using System.Text;
using MelonLoader;

namespace NAK.LegacyContentMitigation.Debug;

public class CameraCallbackLogger
{
    private static CameraCallbackLogger instance;
    private readonly List<string> frameCallbacks = new();
    private bool isListening;
    private readonly StringBuilder logBuilder = new();

    public static CameraCallbackLogger Instance => instance ??= new CameraCallbackLogger();

    private void RegisterCallbacks()
    {
        Camera.onPreCull += (cam) => LogCallback(cam, "OnPreCull");
        Camera.onPreRender += (cam) => LogCallback(cam, "OnPreRender");
        Camera.onPostRender += (cam) => LogCallback(cam, "OnPostRender");
    }

    private void UnregisterCallbacks()
    {
        Camera.onPreCull -= (cam) => LogCallback(cam, "OnPreCull");
        Camera.onPreRender -= (cam) => LogCallback(cam, "OnPreRender");
        Camera.onPostRender -= (cam) => LogCallback(cam, "OnPostRender");
    }

    public void LogCameraEvents()
    {
        MelonCoroutines.Start(LoggingCoroutine());
    }

    private IEnumerator LoggingCoroutine()
    {
        yield return null; // idk at what point in frame start occurs
        
        // First frame: Register and listen
        RegisterCallbacks();
        isListening = true;
        yield return null;

        // Second frame: Log and cleanup
        isListening = false;
        PrintFrameLog();
        UnregisterCallbacks();
    }

    private void LogCallback(Camera camera, string callbackName)
    {
        if (!isListening) return;
        frameCallbacks.Add($"{camera.name} - {callbackName} (Depth: {camera.depth})");
    }

    private void PrintFrameLog()
    {
        logBuilder.Clear();
        logBuilder.AppendLine("\nCamera Callbacks for Frame:");
        
        foreach (var callback in frameCallbacks)
            logBuilder.AppendLine(callback);

        LegacyContentMitigationMod.Logger.Msg(logBuilder.ToString());
        
        frameCallbacks.Clear();
    }
}