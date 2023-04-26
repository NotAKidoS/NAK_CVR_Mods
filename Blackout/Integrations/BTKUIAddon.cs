using BTKUILib;
using BTKUILib.UIObjects;
using System.Runtime.CompilerServices;

namespace NAK.Blackout;

public static class BTKUIAddon
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init()
    {
        //Add myself to the Misc Menu
        Page miscPage = QuickMenuAPI.MiscTabPage;
        Category miscCategory = miscPage.AddCategory(Blackout.SettingsCategory);

        AddMelonToggle(ref miscCategory, Blackout.EntryEnabled);

        //Add my own page to not clog up Misc Menu

        Page blackoutPage = miscCategory.AddPage("Blackout Settings", "", "Configure the settings for Blackout.", "Blackout");
        blackoutPage.MenuTitle = "Blackout Settings";
        blackoutPage.MenuSubtitle = "Dim screen after set time of sitting still, or configure with manual control. Should be nice for VR sleeping.";

        Category blackoutCategory = blackoutPage.AddCategory("Blackout");

        AddMelonToggle(ref blackoutCategory, Blackout.EntryEnabled);

        //manual state changing
        var state_Awake = blackoutCategory.AddButton("Awake State", null, "Enter the Awake State.");
        state_Awake.OnPress += () => BlackoutController.Instance?.ChangeBlackoutState(BlackoutController.BlackoutState.Awake);
        var state_Drowsy = blackoutCategory.AddButton("Drowsy State", null, "Enter the Drowsy State.");
        state_Drowsy.OnPress += () => BlackoutController.Instance?.ChangeBlackoutState(BlackoutController.BlackoutState.Drowsy);
        var state_Sleeping = blackoutCategory.AddButton("Sleeping State", null, "Enter the Sleeping State.");
        state_Sleeping.OnPress += () => BlackoutController.Instance?.ChangeBlackoutState(BlackoutController.BlackoutState.Sleeping);

        //dimming strength
        AddMelonSlider(ref blackoutPage, Blackout.EntryDrowsyDimStrength, 0f, 1f);

        //velocity dim multiplier
        AddMelonToggle(ref blackoutCategory, Blackout.EntryDrowsyVelocityMultiplier);

        //hud messages
        AddMelonToggle(ref blackoutCategory, Blackout.EntryHudMessages);

        //lower fps while sleep (desktop)
        AddMelonToggle(ref blackoutCategory, Blackout.EntryDropFPSOnSleep);

        //auto sleep state
        AddMelonToggle(ref blackoutCategory, Blackout.EntryAutoSleepState);

        //i will add the rest of the settings once BTKUILib supports int input
    }

    private static void AddMelonToggle(ref Category category, MelonLoader.MelonPreferences_Entry<bool> entry)
    {
        category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
    }

    private static void AddMelonSlider(ref Page page, MelonLoader.MelonPreferences_Entry<float> entry, float min, float max)
    {
        page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max).OnValueUpdated += f => entry.Value = f;
    }
}