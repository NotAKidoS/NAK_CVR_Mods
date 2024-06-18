using ABI_RC.Core.Base;
using NAK.OriginShift;
using NAK.OriginShift.Hacks;
using UnityEngine;

namespace OriginShift.ModCompatibility;

public static class ThirdPersonCompatibility
{
    internal static void Fix()
    {
        GameObject thirdPersonCameraObj = GameObject.Find("_PLAYERLOCAL/[CameraRigDesktop]/Camera/ThirdPersonCameraObj");
        if (thirdPersonCameraObj == null) return;
        OriginShiftMod.Logger.Msg("Found ThirdPerson, fixing compatibility...");
        thirdPersonCameraObj.AddComponentIfMissing<OriginShiftOcclusionCullingDisabler>();
    }
}