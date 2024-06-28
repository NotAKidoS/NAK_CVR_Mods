using System.Reflection;
using ABI_RC.Core.Util.AnimatorManager;
using ABI_RC.Systems.Movement;
using ABI.CCK.Scripts;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.KeepVelocityOnExitFlight;

public class KeepVelocityOnExitFlightMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(BetterBetterCharacterController).GetMethod(nameof(BetterBetterCharacterController.ChangeFlight),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(KeepVelocityOnExitFlightMod).GetMethod(nameof(Prefix_OnChangeFlight),
                BindingFlags.NonPublic | BindingFlags.Static)),
            postfix: new HarmonyMethod(typeof(KeepVelocityOnExitFlightMod).GetMethod(nameof(Postfix_OnChangeFlight),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    // ReSharper disable once RedundantAssignment
    private static void Prefix_OnChangeFlight(ref BetterBetterCharacterController __instance, ref Vector3 __state)
    {
        __state = __instance.GetVelocity();
    }
    
    private static void Postfix_OnChangeFlight(ref BetterBetterCharacterController __instance, ref Vector3 __state)
    {
        if (__instance.FlightAllowedInWorld && !__instance.IsFlying()) 
            __instance.SetVelocity(__state);
    }
}