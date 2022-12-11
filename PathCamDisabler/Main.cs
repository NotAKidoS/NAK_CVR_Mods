using ABI_RC.Core.Player;
using ABI_RC.Core.IO;
using MelonLoader;
using UnityEngine;

namespace PathCamDisabler;

public class PathCamDisabler : MelonMod
{
    private static MelonPreferences_Category m_categoryPathCamDisabler;
    private static MelonPreferences_Entry<bool> m_entryDisablePathCam, m_entryDisableFlightBind;

    public override void OnInitializeMelon()
    {
        m_categoryPathCamDisabler = MelonPreferences.CreateCategory(nameof(PathCamDisabler));
        m_entryDisablePathCam = m_categoryPathCamDisabler.CreateEntry<bool>("Disable Path Camera Controller.", true, description: "Disable path camera controller so you can use your numpad keys without funky shit.");
        m_entryDisableFlightBind = m_categoryPathCamDisabler.CreateEntry<bool>("Disable Flight Binding (if controller off).", false, description: "Disables the flight binding as the path camera controller handles that...?");
        m_categoryPathCamDisabler.SaveToFile(false);

        foreach (var setting in m_categoryPathCamDisabler.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }

    System.Collections.IEnumerator WaitForLocalPlayer()
    {
        while (CVRPathCamController.Instance == null)
            yield return null;
        UpdateSettings();
    }

    private void UpdateSettings()
    {
        CVRPathCamController.Instance.enabled = !m_entryDisablePathCam.Value;
    }

    private void OnUpdateSettings(object arg1, object arg2) => UpdateSettings();

    public override void OnUpdate()
    {
        if (m_entryDisablePathCam.Value && !m_entryDisableFlightBind.Value)
        {
            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                PlayerSetup.Instance._movementSystem.ToggleFlight();
            }
        }
    }
}