using ABI.CCK.Components;
using ABI_RC.Core.Player;
using MelonLoader;
using System.Reflection;
using static NAK.ThirdPerson.CameraLogic;
using ABI_RC.Core;

namespace NAK.ThirdPerson;

internal static class Patches
{
    internal static void Apply(HarmonyLib.Harmony harmony)
    {
        harmony.Patch(
            typeof(CVRWorld).GetMethod(nameof(CVRWorld.SetDefaultCamValues), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(Patches).GetMethod(nameof(OnPostWorldStart), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
        harmony.Patch(
            typeof(CVRWorld).GetMethod(nameof(CVRWorld.CopyRefCamValues), BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: typeof(Patches).GetMethod(nameof(OnPreWorldStart), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod(),
            postfix: typeof(Patches).GetMethod(nameof(OnPostWorldStart), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
        harmony.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.SetupIKScaling), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(Patches).GetMethod(nameof(OnScaleAdjusted), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
        harmony.Patch(
            typeof(CVRTools).GetMethod(nameof(CVRTools.ConfigureHudAffinity), BindingFlags.Public | BindingFlags.Static),
            postfix: typeof(Patches).GetMethod(nameof(OnConfigureHudAffinity), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
    }

    //Copy camera settings & postprocessing components
    private static void OnPreWorldStart() => ResetPlayerCamValues();
    private static void OnPostWorldStart() => CopyPlayerCamValues();
    //Adjust camera distance with height as modifier
    private static void OnScaleAdjusted(float height) => AdjustScale(height);
    private static void OnConfigureHudAffinity() => CheckVRMode();
}