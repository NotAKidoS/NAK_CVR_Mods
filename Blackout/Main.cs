using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace Blackout;

public class Blackout : MelonMod
{
    private static bool inVR;
    private static MelonPreferences_Category m_categoryBlackout;
    private static MelonPreferences_Entry<bool> m_entryEnabled, m_entryHudMessages;
    //private static MelonPreferences_Entry<bool> m_entryVROnly;
    private static MelonPreferences_Entry<float>
        m_entryDrowsyThreshold, m_entryAwakeThreshold,
        m_entryDrowsyModeTimer, m_entrySleepModeTimer,
        m_entryDrowsyDimStrength;

    public override void OnApplicationStart()
    {
        m_categoryBlackout = MelonPreferences.CreateCategory(nameof(Blackout));
        m_entryEnabled = m_categoryBlackout.CreateEntry<bool>("Enabled", true, description: "Dim screen when sleeping.");
        m_entryHudMessages = m_categoryBlackout.CreateEntry<bool>("Hud Messages", false, description: "Notify on state change.");
        m_entryDrowsyThreshold = m_categoryBlackout.CreateEntry<float>("Drowsy Threshold", 1f, description: "Degrees of movement to return partial vision.");
        m_entryAwakeThreshold = m_categoryBlackout.CreateEntry<float>("Awake Threshold", 12f, description: "Degrees of movement to return full vision.");
        m_entryDrowsyModeTimer = m_categoryBlackout.CreateEntry<float>("Enter Drowsy Time", 3f, description: "How many minutes without movement until enter drowsy mode.");
        m_entrySleepModeTimer = m_categoryBlackout.CreateEntry<float>("Enter Sleep Time", 10f, description: "How many seconds without movement until enter sleep mode.");
        m_entryDrowsyDimStrength = m_categoryBlackout.CreateEntry<float>("Drowsy Dim Strength", 0.5f, description: "How strong of a dimming effect should drowsy mode have.");
        //m_entryVROnly = m_categoryBlackout.CreateEntry<bool>("VR Only", false, description: "Only enable mod in VR.");
        m_categoryBlackout.SaveToFile(false);

        m_entryEnabled.OnValueChangedUntyped += OnEnabled;
        m_entryHudMessages.OnValueChangedUntyped += OnUpdateSettings;
        m_entryDrowsyThreshold.OnValueChangedUntyped += OnUpdateSettings;
        m_entryAwakeThreshold.OnValueChangedUntyped += OnUpdateSettings;
        m_entryDrowsyModeTimer.OnValueChangedUntyped += OnUpdateSettings;
        m_entrySleepModeTimer.OnValueChangedUntyped += OnUpdateSettings;
        m_entryDrowsyDimStrength.OnValueChangedUntyped += OnUpdateSettings;
        //m_entryVROnly.OnValueChangedUntyped += OnUpdateSettings;
        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }

    System.Collections.IEnumerator WaitForLocalPlayer()
    {
        //load blackout_controller.asset
        AssetsHandler.Load();

        while (PlayerSetup.Instance == null)
            yield return null;

        inVR = PlayerSetup.Instance._inVr;
        PlayerSetup.Instance.gameObject.AddComponent<BlackoutController>();

        //update BlackoutController settings after it initializes
        yield return new WaitForEndOfFrame();
        OnUpdateSettings();
    }

    private void OnEnabled()
    {
        if (!BlackoutController.Instance) return;
        BlackoutController.Instance.enabled = m_entryEnabled.Value;
    }
    private void OnUpdateSettings()
    {
        if (!BlackoutController.Instance) return;
        BlackoutController.Instance.drowsyThreshold = m_entryDrowsyThreshold.Value;
        BlackoutController.Instance.wakeThreshold = m_entryAwakeThreshold.Value;
        BlackoutController.Instance.DrowsyModeTimer = m_entryDrowsyModeTimer.Value;
        BlackoutController.Instance.SleepModeTimer = m_entrySleepModeTimer.Value;
        BlackoutController.Instance.DrowsyDimStrength = m_entryDrowsyDimStrength.Value;
        BlackoutController.Instance.HudMessages = m_entryHudMessages.Value;
    }

    //Support for changing VRMode during runtime.
    [HarmonyPatch]
    private class HarmonyPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerSetup), "CalibrateAvatar")]
        private static void CheckVRModeSwitch()
        {
            if (inVR != PlayerSetup.Instance._inVr)
            {
                BlackoutController.Instance.SetupBlackoutInstance();
            }
        }
    }
}