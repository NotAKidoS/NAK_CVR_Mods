using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util.Object_Behaviour;
using MelonLoader;
using System.Reflection;
using UnityEngine;
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
        harmony.Patch(
            typeof(CameraFacingObject).GetMethod(nameof(CameraFacingObject.Start), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(Patches).GetMethod(nameof(OnCameraFacingObjectStart), BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod()
         );
    }

    //Copy camera settings & postprocessing components
    private static void OnWorldStart() => CopyPlayerCamValues();
    //Adjust camera distance with height as modifier
    private static void OnScaleAdjusted(float height) => AdjustScale(height);
    //Fix bug when in thirdperson and desktop camera is disabled for performance
    private static void OnCameraFacingObjectStart(ref Camera ___m_Camera) { if (___m_Camera == null) ___m_Camera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>(); }
}