using System.Reflection;
using ABI_RC.Core;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.WindowFocusCheckFix;

public class WindowFocusCheckFixMod : MelonMod
{
    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        #region WindowFocusManager Patches

        HarmonyInstance.Patch(
            typeof(WindowFocusManager).GetMethod(nameof(WindowFocusManager.IsWindowFocused),
                BindingFlags.NonPublic | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(WindowFocusCheckFixMod).GetMethod(nameof(OnPreWindowFocusManagerIsWindowFocused),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        #endregion WindowFocusManager Patches
    }
    
    #endregion Melon Events

    #region Harmony Patches

    // ReSharper disable once RedundantAssignment
    private static bool OnPreWindowFocusManagerIsWindowFocused(ref bool __result)
    {
        __result = Application.isFocused; // use Unity method instead
        return false;
    }
    
    #endregion Harmony Patches
}