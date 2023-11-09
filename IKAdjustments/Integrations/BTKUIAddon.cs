using System.Runtime.CompilerServices;
using BTKUILib;
using BTKUILib.UIObjects;
using MelonLoader;
using NAK.IKAdjustments.Systems;

namespace NAK.IKAdjustments.Integrations;

public static class BTKUIAddon
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Initialize()
    {
        //Add myself to the Misc Menu
        Page miscPage = QuickMenuAPI.MiscTabPage;
        Category miscCategory = miscPage.AddCategory(IKAdjustments.SettingsCategory);

        // Add button
        miscCategory.AddButton("Tracking Adjust", "",
                "Adjust tracking points in this mode. Grip to adjust. Trigger to reset.")
            .OnPress += () => { IKAdjuster.Instance.EnterAdjustMode(); };

        // Reset Button
        miscCategory.AddButton("Reset Offsets", "", "Reset all tracked point offsets.")
            .OnPress += () => { IKAdjuster.Instance.ResetAllOffsets(); };

        // Cyle GrabMode Button
        miscCategory.AddButton("Cycle Mode", "", "Cycle grab mode. Position, Rotation, or Both.")
            .OnPress += () => { IKAdjuster.Instance.CycleAdjustMode(); };
    }

    private static void AddMelonToggle(ref Category category, MelonPreferences_Entry<bool> entry)
    {
        category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
    }

    private static void AddMelonSlider(ref Page page, MelonPreferences_Entry<float> entry, float min, float max,
        int decimalPlaces = 2)
    {
        page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max, decimalPlaces).OnValueUpdated +=
            f => entry.Value = f;
    }
}