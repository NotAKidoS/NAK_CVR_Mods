using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using MelonLoader;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Events;

namespace Blackout;

public class Blackout : MelonMod
{
    BlackoutController m_blackoutController = null;

    private static MelonPreferences_Category m_categoryBlackout;
    private static MelonPreferences_Entry<bool> m_entryEnabled;
    private static MelonPreferences_Entry<float> m_entryDrowsyThreshold;
    private static MelonPreferences_Entry<float> m_entryAwakeThreshold;
    private static MelonPreferences_Entry<float> m_entryEnterSleepTime;
    private static MelonPreferences_Entry<float> m_entryReturnSleepTime;

    public override void OnApplicationStart()
    {
        m_categoryBlackout = MelonPreferences.CreateCategory(nameof(Blackout));
        m_entryEnabled = m_categoryBlackout.CreateEntry<bool>("Enabled", true, description: "Dim screen when sleeping.");
        m_entryDrowsyThreshold = m_categoryBlackout.CreateEntry<float>("Drowsy Threshold", 1f, description: "Degrees of movement to return partial vision.");
        m_entryAwakeThreshold = m_categoryBlackout.CreateEntry<float>("Awake Threshold", 12f, description: "Degrees of movement to return full vision.");
        m_entryEnterSleepTime = m_categoryBlackout.CreateEntry<float>("Enter Sleep Time", 3f, description: "How many minutes without movement until enter sleep mode.");
        m_entryReturnSleepTime = m_categoryBlackout.CreateEntry<float>("Return Sleep Time", 10f, description: "How many seconds should the wake state last before return.");
        m_categoryBlackout.SaveToFile(false);

        m_entryEnabled.OnValueChangedUntyped += OnEnabled;
        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }

    System.Collections.IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;

        m_blackoutController = PlayerSetup.Instance.gameObject.AddComponent<BlackoutController>();
    }

    public void OnEnabled()
    {
        if (!m_blackoutController) return;
        m_blackoutController.enabled = m_entryEnabled.Value;
    }
    public void OnUpdateSettings()
    {
        if (!m_blackoutController) return;
        m_blackoutController.drowsyThreshold = m_entryDrowsyThreshold.Value;
        m_blackoutController.wakeThreshold = m_entryAwakeThreshold.Value;
        m_blackoutController.enterSleepTime = m_entryEnterSleepTime.Value;
        m_blackoutController.returnSleepTime = m_entryReturnSleepTime.Value;
    }
}