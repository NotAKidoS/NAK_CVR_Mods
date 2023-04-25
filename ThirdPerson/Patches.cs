using ABI.CCK.Components;
using MelonLoader;
using System.Reflection;
using static NAK.ThirdPerson.CameraLogic;

namespace NAK.ThirdPerson;

internal static class Patches
{
    internal static void Apply(HarmonyLib.Harmony harmony)
    {
        harmony.Patch(
            typeof(CVRWorld).GetMethod("SetDefaultCamValues", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(Patches).GetMethod(nameof(OnWorldStart), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
        harmony.Patch(
            typeof(CVRWorld).GetMethod("CopyRefCamValues", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(Patches).GetMethod(nameof(OnWorldStart), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
    }

    //Copy camera settings & postprocessing components
    private static void OnWorldStart() => CopyFromPlayerCam();
}