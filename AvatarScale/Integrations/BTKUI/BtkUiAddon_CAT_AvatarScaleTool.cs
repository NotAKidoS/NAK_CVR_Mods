using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;

namespace NAK.AvatarScaleMod.Integrations
{
    public static partial class BtkUiAddon
    {
        private static void Setup_AvatarScaleToolCategory(Page page)
        {
            Category avScaleToolCategory = AddMelonCategory(ref page, ModSettings.Hidden_Foldout_AST_SettingsCategory);
            
            AddMelonStringInput(ref avScaleToolCategory, ModSettings.EntryASTScaleParameter, "icon");
            AddMelonNumberInput(ref avScaleToolCategory, ModSettings.EntryASTMinHeight, "icon");
            AddMelonNumberInput(ref avScaleToolCategory, ModSettings.EntryASTMaxHeight, "icon");
            avScaleToolCategory.AddButton("Reset Overrides", "", "Reset to Avatar Scale Tool default values.", ButtonStyle.TextOnly)
            .OnPress += () =>{
                ModSettings.EntryASTScaleParameter.ResetToDefault();
                ModSettings.EntryASTMinHeight.ResetToDefault();
                ModSettings.EntryASTMaxHeight.ResetToDefault();
            };
        }
    }
}