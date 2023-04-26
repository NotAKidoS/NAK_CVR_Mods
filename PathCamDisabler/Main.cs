using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using MelonLoader;
using UnityEngine;

namespace NAK.PathCamDisabler;

public class PathCamDisabler : MelonMod
{
    public static readonly MelonPreferences_Category Category = 
        MelonPreferences.CreateCategory(nameof(PathCamDisabler));

    public static readonly MelonPreferences_Entry<bool> EntryDisablePathCam = 
        Category.CreateEntry("Disable Path Camera Controller.", true, "Disable Path Camera Controller.");

    public static readonly MelonPreferences_Entry<bool> EntryDisableFlightBind = 
        Category.CreateEntry("Disable Flight Binding (if controller off).", false, "Disable flight bind if Path Camera Controller is also disabled.");

    public override void OnInitializeMelon()
    {
        EntryDisablePathCam.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);

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
        CVRPathCamController.Instance.enabled = !EntryDisablePathCam.Value;
    }
    private void OnUpdateSettings(object arg1, object arg2) => UpdateSettings();

    public override void OnUpdate()
    {
        if (EntryDisablePathCam.Value && !EntryDisableFlightBind.Value)
        {
            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                PlayerSetup.Instance._movementSystem.ToggleFlight();
            }
        }
    }
}