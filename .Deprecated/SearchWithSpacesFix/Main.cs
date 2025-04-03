using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using HarmonyLib;
using MelonLoader;

namespace NAK.SearchWithSpacesFix;

public class SearchWithSpacesFixMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(ViewManager).GetMethod(nameof(ViewManager.GetSearchResults),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(SearchWithSpacesFixMod).GetMethod(nameof(OnPreGetSearchResults),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    // this is so crazy

    private static void OnPreGetSearchResults(ref string searchTerm)
        => searchTerm = searchTerm.Replace(" ", "_");

    // this is so crazy
}