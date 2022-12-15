using ABI_RC.Core;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Core.Util.Object_Behaviour;
using ABI_RC.Systems.Camera;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.MovementSystem;
using DesktopVRSwitch.Patches;
using HarmonyLib;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using Object = UnityEngine.Object;

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
    }
}