using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using HarmonyLib;
using MagicaCloth2;
using MelonLoader;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace NAK.FuckOffMagicaCloth2;

public class FuckOffMagicaCloth2Mod : MelonMod
{
    private static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(MagicaCloth_Patches));
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

    internal static class MagicaCloth_Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MagicaCloth2.MagicaCloth), nameof(MagicaCloth2.MagicaCloth.Awake))]
        private static void MagicaCloth_Awake_Prefix(MagicaCloth2.MagicaCloth __instance)
        {
            __instance.SerializeData.selfCollisionConstraint.selfMode = SelfCollisionConstraint.SelfCollisionMode.None;
            __instance.SerializeData.selfCollisionConstraint.syncMode = SelfCollisionConstraint.SelfCollisionMode.None;
        }
    }
}