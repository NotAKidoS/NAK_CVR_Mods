using System;
using MelonLoader;

namespace NAK.DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.CVRInputManagerPatches));
        ApplyPatches(typeof(HarmonyPatches.VRModeSwitchManagerPatches));
    }

    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }
}