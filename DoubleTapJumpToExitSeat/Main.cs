using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.Movement;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.DoubleTapJumpToExitSeat;

public class DoubleTapJumpToExitSeatMod : MelonMod
{
    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        #region CVRSeat Patches
        
        HarmonyInstance.Patch(
            typeof(CVRSeat).GetMethod(nameof(CVRSeat.Update), 
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(DoubleTapJumpToExitSeatMod).GetMethod(nameof(OnPreCVRSeatUpdate),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        #endregion CVRSeat Patches

        #region ViewManager Patches
        
        HarmonyInstance.Patch(
            typeof(ViewManager).GetMethod(nameof(ViewManager.Update), 
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(DoubleTapJumpToExitSeatMod).GetMethod(nameof(OnPreViewManagerUpdate),
                BindingFlags.NonPublic | BindingFlags.Static)),
            postfix: new HarmonyMethod(typeof(DoubleTapJumpToExitSeatMod).GetMethod(nameof(OnPostViewManagerUpdate),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        #endregion ViewManager Patches
    }
    
    #endregion Melon Events

    #region Harmony Patches
    
    private static float _lastJumpTime = -1f;
    private static bool _wasJumping;
    
    private static bool OnPreCVRSeatUpdate(CVRSeat __instance)
    {
        if (!__instance.occupied) return false;
        
        // Crazy?
        bool jumped = CVRInputManager.Instance.jump;
        bool justJumped = jumped && !_wasJumping;
        _wasJumping = jumped;
        if (justJumped)
        {
            float t = Time.time;
            if (t - _lastJumpTime <= BetterBetterCharacterController.DoubleJumpFlightTimeOut)
            {
                _lastJumpTime = -1f;
                __instance.ExitSeat();
                return false;
            }
            _lastJumpTime = t;
        }
        
        // Double update this frame (this ensures Extrapolate / Every Frame Updated objects are seated correctly)
        if (__instance.vrSitPosition.position != __instance._lastPosition || __instance.vrSitPosition.rotation != __instance._lastRotation)
            __instance.MovePlayerToSeat(__instance.vrSitPositionReady ? __instance.vrSitPosition : __instance.transform);

        // Steal sync
        if (__instance.lockControls)
        {
            if (__instance._spawnable) __instance._spawnable.ForceUpdate(4);
            if (__instance._objectSync) __instance._objectSync.ForceUpdate(4);
        }

        return false; // don't call original method
    }

    // ReSharper disable once RedundantAssignment
    private static void OnPreViewManagerUpdate(ref bool __state)
        => (__state, BetterBetterCharacterController.Instance._isSitting) 
            = (BetterBetterCharacterController.Instance._isSitting, false);

    private static void OnPostViewManagerUpdate(ref bool __state)
        => BetterBetterCharacterController.Instance._isSitting = __state;
    
    #endregion Harmony Patches
}