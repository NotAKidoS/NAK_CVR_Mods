using System.Collections;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using MelonLoader;
using NAK.OriginShift;
using NAK.OriginShift.Hacks;
using UnityEngine;
using AccessTools = HarmonyLib.AccessTools;
using HarmonyMethod = HarmonyLib.HarmonyMethod;

namespace OriginShift.Integrations;

public static class ThirdPersonAddon
{
    public static void Initialize()
    {
        OriginShiftMod.HarmonyInst.Patch(
            AccessTools.Method(typeof(PlayerSetup), nameof(PlayerSetup.Start)),
            postfix: new HarmonyMethod(typeof(ThirdPersonAddon), nameof(OnPostPlayerSetupStart))
        );
    }

    private static void OnPostPlayerSetupStart()
    {
        OriginShiftMod.Logger.Msg("Found ThirdPerson, fixing compatibility...");
        MelonCoroutines.Start(FixThirdPersonCompatibility());
    }
    
    private static IEnumerator FixThirdPersonCompatibility()
    {
        yield return null; // wait a frame for the camera to be setup
        yield return null; // wait a frame for the camera to be setup
        GameObject thirdPersonCameraObj = GameObject.Find("_PLAYERLOCAL/[CameraRigDesktop]/Camera/ThirdPersonCameraObj");
        thirdPersonCameraObj.AddComponentIfMissing<OriginShiftOcclusionCullingDisabler>();
    }
}

