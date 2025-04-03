using ABI_RC.Core.Player;
using ABI_RC.Core.Util.AssetFiltering;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.LateInitComponentHelperHack;

public class LateInitComponentHelperHack : MelonMod
{
    private static bool _hasLoggedIn;
    
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches));
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
    
    private static class HarmonyPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ComponentHelper), nameof(ComponentHelper.Initialize))]
        private static bool Prefix_ComponentHelper_Initialize() => _hasLoggedIn;
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.Start))]
        private static void Postfix_PlayerSetup_Start()
        {
            _hasLoggedIn = true;
            ComponentHelper.Initialize();
        }
    }
}