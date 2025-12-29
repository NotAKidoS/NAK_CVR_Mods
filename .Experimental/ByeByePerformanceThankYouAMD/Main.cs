using System.Collections;
using ABI_RC.Core;
using ABI_RC.Core.IO;
using ABI_RC.Core.Util.Encryption;
using ABI_RC.Systems.GameEventSystem;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.ByeByePerformanceThankYouAMD;

public class ByeByePerformanceThankYouAMDMod : MelonMod
{
    private static MelonLogger.Instance Logger;

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(ByeByePerformanceThankYouAMD));

    private static readonly MelonPreferences_Entry<bool> EntryDisableMaterialInstancing =
        Category.CreateEntry(
            identifier: "disable_material_instancing",
            true,
            display_name: "Disable Material Instancing",
            description: "Disables material instancing to mitigate a shit visual issue on AMD");
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(CVREncryptionRouter_Patches));
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
    
    internal static void ScanForInstancedMaterials()
    {
        if (!EntryDisableMaterialInstancing.Value)
            return;

        Logger.Msg("An Asset Bundle has loaded, scanning for instanced materials to disable...");

        if (Resources.FindObjectsOfTypeAll(typeof(Material)) is not Material[] allMaterials)
        {
            Logger.Msg("No materials found.");
            return;
        }

        int count = 0;
        foreach (Material material in allMaterials)
        {
            if (!material || !material.enableInstancing) continue;
            material.enableInstancing = false;
            count++;
        }

        Logger.Msg($"Finished scanning for instanced materials. Disabled instancing on {count} loaded materials.");
    }
}

internal static class CVREncryptionRouter_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVREncryptionRouter), nameof(CVREncryptionRouter.LoadEncryptedBundle), typeof(bool))]
    private static void CVREncryptionRouter_LoadEncryptedBundle_Postfix(ref IEnumerator __result)
    {
        __result = Wrapper(__result);
    }

    private static IEnumerator Wrapper(IEnumerator inner)
    {
        yield return null; // before start

        while (inner.MoveNext())
            yield return inner.Current;

        // after finish
        ByeByePerformanceThankYouAMDMod.ScanForInstancedMaterials();
    }
}