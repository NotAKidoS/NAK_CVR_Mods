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

        // Cycle GrabMode Button
        miscCategory.AddButton("Cycle Mode", "", "Cycle grab mode. Position, Rotation, or Both.")
            .OnPress += () => { IKAdjuster.Instance.CycleAdjustMode(); };
    }
}