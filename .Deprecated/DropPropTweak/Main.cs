using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.DropPropTweak;

public class DropPropTweakMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // make drop prop actually usable
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.DropProp),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(DropPropTweakMod).GetMethod(nameof(OnDropProp),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static bool OnDropProp(string propGuid, ref PlayerSetup __instance)
    {
        Vector3 position = __instance.activeCam.transform.position + __instance.GetPlayerForward() * 1.5f; // 1f -> 1.5f

        if (Physics.Raycast(position,
                __instance.CharacterController.GetGravityDirection(), // align with gravity, not player up
                out RaycastHit raycastHit, 4f, __instance.dropPlacementMask))
        {
            // native method passes false, so DropProp doesn't align with gravity :)
            CVRSyncHelper.SpawnProp(propGuid, raycastHit.point.x, raycastHit.point.y, raycastHit.point.z, true);
            return false;
        }

        // unlike original, we will still spawn prop even if raycast fails, giving the method actual utility :3

        // hack- we want to align with *our* rotation, not affecting gravity
        Vector3 ogGravity = __instance.CharacterController.GetGravityDirection();
        __instance.CharacterController.gravity = -__instance.transform.up; // align with our rotation

        // spawn prop with useTargetLocationGravity false, so it pulls our gravity dir we've modified
        CVRSyncHelper.SpawnProp(propGuid, position.x, position.y, position.z, false);

        __instance.CharacterController.gravity = ogGravity; // restore gravity
        return false;
    }
}