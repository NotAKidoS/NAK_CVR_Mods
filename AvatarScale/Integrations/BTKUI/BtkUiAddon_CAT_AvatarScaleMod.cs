using BTKUILib.UIObjects;

namespace NAK.AvatarScaleMod.Integrations
{
    public static partial class BtkUiAddon
    {
        private static void Setup_AvatarScaleModCategory(Page page)
        {
            Category avScaleModCategory = AddMelonCategory(ref page, ModSettings.Hidden_Foldout_ASM_SettingsCategory);
            
            AddMelonToggle(ref avScaleModCategory, ModSettings.EntryScaleGestureEnabled);
            AddMelonToggle(ref avScaleModCategory, ModSettings.EntryScaleKeybindingsEnabled);
            AddMelonToggle(ref avScaleModCategory, ModSettings.EntryPersistentHeight);
            AddMelonToggle(ref avScaleModCategory, ModSettings.EntryPersistThroughRestart);
        }
    }
}