using MelonLoader;

namespace NAK.PhysicsGunMod;

internal static class ModSettings
{
    internal const string ModName = nameof(PhysicsGunMod);
    internal const string ASM_SettingsCategory = "Physics Gun Mod";
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);
}