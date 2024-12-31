using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.API.Responses;
using ABI.CCK.Components;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using NAK.LegacyContentMitigation.Debug;

namespace NAK.LegacyContentMitigation.Integrations;

public static class BtkUiAddon
{
    private static ToggleButton _currentModState;
    
    public static void Initialize()
    {
        // Create menu late to ensure we at bottom.
        // Doing this cause these settings are "Advanced" & mostly for debugging.
        QuickMenuAPI.OnMenuGenerated += SetupCategory;
    }

    private static void SetupCategory(CVR_MenuManager _)
    {
        QuickMenuAPI.OnMenuGenerated -= SetupCategory;
        
        Category category = QuickMenuAPI.MiscTabPage.AddCategory(ModSettings.LCM_SettingsCategory, ModSettings.ModName, true, true, true);
        
        ToggleButton autoButton = category.AddMelonToggle(ModSettings.EntryAutoForLegacyWorlds);
        autoButton.OnValueUpdated += (_) =>
        {
            if (CVRWorld.CompatibilityVersion == CompatibilityVersions.NotSpi)
                QuickMenuAPI.ShowNotice("Legacy World Notice",
                    "You must reload this World Bundle for Shader Replacement to be undone / applied. " +
                    "Load into a different world and then rejoin.");
        };
        
        _currentModState = category.AddToggle(string.Empty, string.Empty, false);
        _currentModState.OnValueUpdated += OnCurrentStateToggled;

        Button printCameraCallbacksButton = category.AddButton("DEBUG LOG CAMERAS", 
            string.Empty, "Records Camera events & logs them next frame. Useful for determining camera render order shenanigans.");
        printCameraCallbacksButton.OnPress += () => CameraCallbackLogger.Instance.LogCameraEvents();

        OnMultiPassActiveChanged(FakeMultiPassHack.Instance.IsEnabled);
        FakeMultiPassHack.OnMultiPassActiveChanged += OnMultiPassActiveChanged;
    }

    private static void OnCurrentStateToggled(bool state)
    {
        if (state)
        {
            _currentModState.ToggleValue = false; // dont visually update
            QuickMenuAPI.ShowConfirm("Legacy Mitigation Warning",
                "This will change how the main VR view is rendered and cause a noticeable performance hit. " +
                "It is recommended to only enable this within Worlds that require it (Legacy Content/Broken Shaders). " +
                "Shader Replacement will not occur for ALL content that is loaded while enabled. " +
                "If this World is Legacy and already Shader Replaced, you must enable Auto For Legacy Worlds instead, " +
                "load a different World, and then join back.",
                () =>
                {
                    FakeMultiPassHack.Instance.SetMultiPassActive(true);
                    OnMultiPassActiveChanged(true);
                });
        }
        else
        {
            FakeMultiPassHack.Instance.SetMultiPassActive(false);
            OnMultiPassActiveChanged(false);
        }
    }

    private static void OnMultiPassActiveChanged(bool state)
    {
        _currentModState.ToggleValue = state;
        if (state)
        {
            _currentModState.ToggleName = "Currently Active";
            _currentModState.ToggleTooltip = "Fake Multi Pass is currently active.";
        }
        else
        {
            _currentModState.ToggleName = "Currently Inactive";
            _currentModState.ToggleTooltip = "Fake Multi Pass is inactive.";
        }
    }
}