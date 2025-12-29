using System.Reflection;
using ABI_RC.Core.UI;
using HarmonyLib;
using MelonLoader;

namespace NAK.FuckCohtml2;

public class FuckCohtml2Mod : MelonMod
{
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(FuckCohtml2));
    
    private static readonly MelonPreferences_Entry<bool> EntryFixShouldAdvance =
        Category.CreateEntry(
            identifier: "fix_should_advance",
            true,
            display_name: "Fix ShouldAdvance",
            description: "Fix CohtmlControlledView.ShouldAdvance to respect the Enabled property.");
    
    private static readonly MelonPreferences_Entry<bool> EntryFixShouldRender =
        Category.CreateEntry(
            identifier: "fix_should_render",
            true,
            display_name: "Fix ShouldRender",
            description: "Fix CohtmlControlledView.ShouldRender to respect the Enabled property.");
    
    public override void OnInitializeMelon()
    {
        PatchProperty(nameof(CohtmlControlledView.ShouldAdvance), nameof(OnShouldAdvance));
        PatchProperty(nameof(CohtmlControlledView.ShouldRender), nameof(OnShouldRender));
    }

    private void PatchProperty(string propertyName, string handlerName)
    {
        PropertyInfo prop = typeof(CohtmlControlledView).GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance);
        MethodInfo getter = prop!.GetGetMethod(true);

        MethodInfo postfixMethod = typeof(FuckCohtml2Mod).GetMethod(handlerName,
            BindingFlags.NonPublic | BindingFlags.Static, null,
            [typeof(object), typeof(bool).MakeByRefType()], null);

        HarmonyInstance.Patch(getter, postfix: new HarmonyMethod(postfixMethod));
    }

    private static void OnShouldAdvance(object __instance, ref bool __result)
    {
        if (!EntryFixShouldAdvance.Value) return;
        if (__instance is CohtmlControlledView inst) __result &= inst.Enabled;
    }

    private static void OnShouldRender(object __instance, ref bool __result)
    {
        if (!EntryFixShouldRender.Value) return;
        if (__instance is CohtmlControlledView inst) __result &= inst.Enabled;
    }
}