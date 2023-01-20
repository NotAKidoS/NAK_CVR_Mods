using ABI_RC.Systems.Camera;
using HarmonyLib;
using NAK.Melons.PortableCameraAdditions.VisualMods;
using UnityEngine;

namespace NAK.Melons.PortableCameraAdditions.HarmonyPatches;

[HarmonyPatch]
internal class HarmonyPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), "Start")]
    private static void Postfix_PortableCamera_Start(ref PortableCamera __instance)
    {
        //run mod.Setup() instead of registering full mod with icon
        AdditionalSettings mainMod = new AdditionalSettings();
        mainMod.Setup(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), "OnWorldLoaded")]
    private static void Postfix_PortableCamera_OnWorldLoaded(Camera worldCamera)
    {
        AdditionalSettings.Instance?.OnWorldLoaded(worldCamera);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), "UpdateOptionsDisplay")]
    private static void Postfix_PortableCamera_UpdateOptionsDisplay(ref bool ____showExpertSettings)
    {
        AdditionalSettings.Instance?.OnUpdateOptionsDisplay(____showExpertSettings);
    }
}