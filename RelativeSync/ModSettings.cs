using MelonLoader;
using NAK.RelativeSync.Networking;

namespace NAK.RelativeSync;

internal static class ModSettings
{
    internal const string ModName = nameof(RelativeSync);

    #region Melon Preferences
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
    
    private static readonly MelonPreferences_Entry<bool> DebugLogInbound =
        Category.CreateEntry("DebugLogInbound", false,
            "Debug Log Inbound", description: "Log inbound network messages.");
    
    private static readonly MelonPreferences_Entry<bool> DebugLogOutbound = 
        Category.CreateEntry("DebugLogOutbound", false,
            "Debug Log Outbound", description: "Log outbound network messages.");
    
    private static readonly MelonPreferences_Entry<bool> ExpSyncedObjectHack =
        Category.CreateEntry("ExpSyncedObjectHack", true,
            "Exp Spawnable Sync Fix", description: "Forces CVRSpawnable to update position in FixedUpdate. May help reduce local jitter on synced movement parents.");

    #endregion Melon Preferences
    
    internal static void Initialize()
    {
        foreach (MelonPreferences_Entry setting in Category.Entries)
            setting.OnEntryValueChangedUntyped.Subscribe(OnSettingsChanged);
        
        OnSettingsChanged();
    }

    private static void OnSettingsChanged(object oldValue = null, object newValue = null)
    {
        ModNetwork.Debug_NetworkInbound = DebugLogInbound.Value;
        ModNetwork.Debug_NetworkOutbound = DebugLogOutbound.Value;
        Patches.CVRSpawnablePatches.UseHack = ExpSyncedObjectHack.Value;
    }
}