using ABI_RC.Core.Player;
using HarmonyLib;
using NAK.AvatarScaleMod.AvatarScaling;
using NAK.AvatarScaleMod.GestureReconizer;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.AvatarScaleMod.HarmonyPatches;

internal class PlayerSetupPatches
 {
     [HarmonyPostfix]
     [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
     private static void Postfix_PlayerSetup_Start()
     {
         try
         {
             GameObject scaleManager = new (nameof(AvatarScaleManager), typeof(AvatarScaleManager));
             Object.DontDestroyOnLoad(scaleManager);
         }
         catch (Exception e)
         {
             AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_Start)}");
             AvatarScaleMod.Logger.Error(e);
         }
     }
     
     [HarmonyPostfix]
     [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatar))]
     private static void Postfix_PlayerSetup_SetupAvatar(ref PlayerSetup __instance)
     {
         try
         {
             AvatarScaleManager.Instance.OnAvatarInstantiated(__instance);
         }
         catch (Exception e)
         {
             AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_SetupAvatar)}");
             AvatarScaleMod.Logger.Error(e);
         }
     }
     
     [HarmonyPostfix]
     [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ClearAvatar))]
     private static void Postfix_PlayerSetup_ClearAvatar(ref PlayerSetup __instance)
     {
         try
         {
             if (__instance == null) return; // this is called when the game is closed
             AvatarScaleManager.Instance.OnAvatarDestroyed(__instance);
         }
         catch (Exception e)
         {
             AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_ClearAvatar)}");
             AvatarScaleMod.Logger.Error(e);
         }
     }
 }

internal class PuppetMasterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), nameof(PuppetMaster.AvatarInstantiated))]
    private static void Postfix_PuppetMaster_AvatarInstantiated(ref PuppetMaster __instance)
    {
        try
        {
            AvatarScaleManager.Instance.OnNetworkAvatarInstantiated(__instance);
        }
        catch (Exception e)
        {
            AvatarScaleMod.Logger.Error(
                $"Error during the patched method {nameof(Postfix_PuppetMaster_AvatarInstantiated)}");
            AvatarScaleMod.Logger.Error(e);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), nameof(PuppetMaster.AvatarDestroyed))]
    private static void Postfix_PuppetMaster_AvatarDestroyed(ref PuppetMaster __instance)
    {
        try
        {
            if (__instance == null) return; // this is called when the game is closed
            AvatarScaleManager.Instance.OnNetworkAvatarDestroyed(__instance);
        }
        catch (Exception e)
        {
            AvatarScaleMod.Logger.Error(
                $"Error during the patched method {nameof(Postfix_PuppetMaster_AvatarDestroyed)}");
            AvatarScaleMod.Logger.Error(e);
        }
    }
}

internal class GesturePlaneTestPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GesturePlaneTest), nameof(GesturePlaneTest.Start))]
    private static void Postfix_GesturePlaneTest_Start()
    {
        try
        {
            // nicked from Kafe >:))))
            ScaleReconizer.Initialize();
        }
        catch (Exception e)
        {
            AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_GesturePlaneTest_Start)}");
            AvatarScaleMod.Logger.Error(e);
        }
    }
}