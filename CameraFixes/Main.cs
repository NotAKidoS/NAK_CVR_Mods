using ABI_RC.Core.IO;
using MelonLoader;
using UnityEngine;

namespace NAK.CameraFixes;

public class CameraFixes : MelonMod
{
    internal static MelonLogger.Instance Logger;
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(HarmonyPatches.CVRCamControllerPatches));
    }

    void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            Logger.Msg($"Failed while patching {type.Name}!");
            Logger.Error(e);
        }
    }
}