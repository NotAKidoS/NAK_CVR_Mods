<<<<<<< Updated upstream
﻿using ABI_RC.Core.Player;
using MelonLoader;

//tell the game to change VRMode/DesktopMode for Steam/Discord presence
//RichPresence.PopulatePresence();

//nvm that resets the RichPresence clock- i want people to know how long ive wasted staring at mirror 

namespace DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{
    internal const string SettingsCategory = "DesktopVRSwitch";
    internal static MelonPreferences_Category m_categoryDesktopVRSwitch;
    internal static MelonPreferences_Entry<bool> m_entryTimedErrorCatch;
    internal static MelonPreferences_Entry<float> m_entryTimedErrorTimer;

    public override void OnInitializeMelon()
    {
        m_categoryDesktopVRSwitch = MelonPreferences.CreateCategory(SettingsCategory);
        m_entryTimedErrorCatch = m_categoryDesktopVRSwitch.CreateEntry<bool>("Timed Error Catch", true, description: "Attempt to switch back if an error is found after n seconds.");
        m_entryTimedErrorTimer = m_categoryDesktopVRSwitch.CreateEntry<float>("Timed Error Timer", 10f, description: "Amount of seconds to wait before assuming there was an error.");

        m_categoryDesktopVRSwitch.SaveToFile(false);
        foreach (var setting in m_categoryDesktopVRSwitch.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        //UIExpansionKit addon
        if (MelonMod.RegisteredMelons.Any(it => it.Info.Name == "UI Expansion Kit"))
        {
            MelonLogger.Msg("Initializing UIExpansionKit support.");
            UiExtensionsAddon.Init();
        }
        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }

    System.Collections.IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;

        PlayerSetup.Instance.gameObject.AddComponent<DesktopVRSwitchHelper>();

        while (DesktopVRSwitchHelper.Instance == null)
            yield return null;

        UpdateAllSettings();
    }

    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();
    private void UpdateAllSettings()
    {
        if (!DesktopVRSwitchHelper.Instance) return;
        DesktopVRSwitchHelper.Instance.SettingTimedErrorCatch = m_entryTimedErrorCatch.Value;
        DesktopVRSwitchHelper.Instance.SettingTimedErrorTimer = m_entryTimedErrorTimer.Value;
=======
﻿using MelonLoader;

namespace NAK.Melons.DesktopVRSwitch;

public class DesktopVRSwitchMod : MelonMod
{
    internal const string SettingsCategory = "DesktopVRSwitch";
    internal static MelonPreferences_Category m_categoryDesktopVRSwitch;
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        m_categoryDesktopVRSwitch = MelonPreferences.CreateCategory(SettingsCategory);

        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRPickupObjectPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRWorldPatches));
        ApplyPatches(typeof(HarmonyPatches.CameraFacingObjectPatches));
        ApplyPatches(typeof(HarmonyPatches.IKSystemPatches));
        ApplyPatches(typeof(HarmonyPatches.MovementSystemPatches));
    }

    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            Logger.Msg($"Failed while patching {type.Name}!");
            Logger.Error(e);
        }
>>>>>>> Stashed changes
    }
}