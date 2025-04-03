using System.Collections;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;
using NAK.OriginShift;
using NAK.OriginShift.Components;
using UnityEngine;

namespace OriginShift.Integrations;

public static class RagdollAddon
{
    public static void Initialize()
    {
        OriginShiftMod.HarmonyInst.Patch(
            AccessTools.Method(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatar)),
            postfix: new HarmonyMethod(typeof(RagdollAddon), nameof(OnPostPlayerSetupSetupAvatar))
        );
    }
    
    private static void OnPostPlayerSetupSetupAvatar()
    {
        OriginShiftMod.Logger.Msg("Found Ragdoll, fixing compatibility...");
        MelonCoroutines.Start(FixRagdollCompatibility());
    }

    private static IEnumerator FixRagdollCompatibility()
    {
        yield return null; // wait a frame for the avatar to be setup
        GameObject ragdollObj = GameObject.Find("_PLAYERLOCAL/[PlayerAvatarPuppet]");

        // get all rigidbodies in the ragdoll
        var ragdollRigidbodies = ragdollObj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in ragdollRigidbodies) rb.AddComponentIfMissing<OriginShiftRigidbodyReceiver>();
    }
}