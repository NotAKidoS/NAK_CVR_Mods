using ABI.CCK.Components;
using ABI_RC.Core.Player;
using MelonLoader;
using System.Reflection;
using static NAK.ThirdPerson.CameraLogic;

namespace NAK.ThirdPerson;

internal static class Patches
{
    internal static void Apply(HarmonyLib.Harmony harmony)
    {
        harmony.Patch(
            typeof(CVRWorld).GetMethod(nameof(CVRWorld.SetDefaultCamValues), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(Patches).GetMethod(nameof(OnWorldStart), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
        harmony.Patch(
            typeof(CVRWorld).GetMethod(nameof(CVRWorld.CopyRefCamValues), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(Patches).GetMethod(nameof(OnWorldStart), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
        harmony.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.SetupIKScaling), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(Patches).GetMethod(nameof(OnScaleAdjusted), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
    }

    //Copy camera settings & postprocessing components
    private static void OnWorldStart() => CopyPlayerCamValues();
    private static void OnScaleAdjusted(float height) => AdjustScale(height);
}