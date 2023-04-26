using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using MelonLoader;
using UnityEngine;

namespace NAK.PathCamDisabler
{
    public class PathCamDisablerMod : MelonMod
    {
        internal const string SettingsCategory = "PathCamDisabler";
        internal static MelonPreferences_Category m_categoryPathCamDisabler;
        internal static MelonPreferences_Entry<bool> m_entryDisablePathCam, m_entryDisableFlightBind;

        public override void OnInitializeMelon()
        {
            m_categoryPathCamDisabler = MelonPreferences.CreateCategory(SettingsCategory);
            m_entryDisablePathCam = m_categoryPathCamDisabler.CreateEntry<bool>("Disable Path Camera Controller.", true, description: "Disable Path Camera Controller.");
            m_entryDisableFlightBind = m_categoryPathCamDisabler.CreateEntry<bool>("Disable Flight Binding (if controller off).", false, description: "Disable flight bind if Path Camera Controller is also disabled.");

            m_entryDisablePathCam.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);

            MelonLoader.MelonCoroutines.Start(WaitForCVRPathCamController());
        }

        private System.Collections.IEnumerator WaitForCVRPathCamController()
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
}