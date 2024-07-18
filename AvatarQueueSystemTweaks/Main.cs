using MelonLoader;
using NAK.AvatarQueueSystemTweaks.Patches;

namespace NAK.AvatarQueueSystemTweaks;

public class AvatarQueueSystemTweaksMod : MelonMod
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(AvatarQueueSystemTweaks));

    public static readonly MelonPreferences_Entry<bool> EntryPrioritizeSelf =
        Category.CreateEntry("prioritize_self", true, "Prioritize Self", description: "Prioritize loading of your own avatar over others.");
    
    public static readonly MelonPreferences_Entry<bool> EntryPrioritizeFriends =
        Category.CreateEntry("prioritize_friends", true, "Prioritize Friends", description: "Prioritize loading of friends avatars over others.");
    
    public static readonly MelonPreferences_Entry<bool> EntryLoadByDistance =
        Category.CreateEntry("load_by_distance", true, "Load By Distance", description: "Prioritize loading of avatars by distance.");

    // public static readonly MelonPreferences_Entry<bool> EntryChokeInstantiation =
    //     Category.CreateEntry("choke_instantiation", false, "Choke Instantiation",
    //         description: "Chokes the instantiation queue by waiting 0.2s");

    public override void OnInitializeMelon()
    {
        //ApplyPatches(typeof(CVRObjectLoaderPatches));
        ApplyPatches(typeof(AvatarQueueSystemPatches));
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
}