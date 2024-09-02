using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.SmartReticle;

public class SmartReticleMod : MelonMod
{
    #region Melon Preferences
    
    private const string SettingsCategory = nameof(SmartReticleMod);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);
    
    private static readonly MelonPreferences_Entry<bool> Entry_Enabled =
        Category.CreateEntry("enabled", true, display_name: "Enabled",description: "Toggle SmartReticleMod entirely.");

    private static readonly MelonPreferences_Entry<float> Entry_HideTimeout =
        Category.CreateEntry("hide_timeout", 1f, display_name: "Hide Timeout (s)", description: "Timeout before the reticle hides again. Set to 0 to instantly hide.");
    
    #endregion Melon Preferences
    
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(ControllerRay_Patches));
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

    #region Patches
    
    private static class ControllerRay_Patches
    {
        private static Transform _mainMenuTransform;
        private static Transform _quickMenuTransform;
        private static float _lastDisplayedTime;
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ControllerRay), nameof(ControllerRay.Start))]
        private static void Postfix_ControllerRay_Start()
        {
            _mainMenuTransform = ViewManager.Instance.transform;
            _quickMenuTransform = CVR_MenuManager.Instance.transform;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ControllerRay), nameof(ControllerRay.DisplayAuraHighlight))]
        private static void Postfix_ControllerRay_DisplayAuraHighlight(ref ControllerRay __instance)
        {
            if (!Entry_Enabled.Value) 
                return;
            
            GameObject pointer;
            if (__instance.isDesktopRay) // in desktop mode
                pointer = CohtmlHud.Instance.desktopPointer;
            else if (__instance.isHeadRay) // in VR mode with no controllers
                pointer = __instance.backupCrossHair;
            else
                return;

            if (!pointer.activeSelf)
            {
                _lastDisplayedTime = 0; // reset time
                return; // pointing at menu or cursor / controllers active
            }
            
            bool shouldDisplayPointer = (__instance._interact  // pressing mouse1 or mouse2
                                           || __instance._isTryingToPickup
                                           // using some tool/utility
                                           || (PlayerSetup.Instance.GetCurrentPropSelectionMode() 
                                               != PlayerSetup.PropSelectionMode.None)
                                           // hit something- other than the two menus
                                           || (__instance._objectWasHit 
                                               && (__instance.hitTransform != _mainMenuTransform 
                                                   && __instance.hitTransform != _quickMenuTransform)));
            
            if (shouldDisplayPointer)
            {
                _lastDisplayedTime = Time.time;
                return;
            }
            
            if (Time.time - _lastDisplayedTime > Entry_HideTimeout.Value) 
                pointer.SetActive(false);
        }
    }
    
    #endregion Patches
}