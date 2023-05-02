using ABI_RC.Core.Player;
using MelonLoader;
using System.Collections;

namespace NAK.CVRGizmos;

public class CVRGizmos : MelonMod
{
    public static readonly MelonPreferences_Category CategoryCVRGizmos = 
        MelonPreferences.CreateCategory(nameof(CVRGizmos));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        CategoryCVRGizmos.CreateEntry("Enabled", false, description: "Toggle CVR Gizmos entirely.");

    public static readonly MelonPreferences_Entry<bool> EntryLocalOnly =
        CategoryCVRGizmos.CreateEntry("Local Only", false, description: "Toggle CVR Gizmos local-only mode.");

    public override void OnInitializeMelon()
    {
        EntryEnabled.OnEntryValueChangedUntyped.Subscribe(CVRGizmosEnabled);
        EntryLocalOnly.OnEntryValueChangedUntyped.Subscribe(CVRGizmosLocalOnly);
        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }

    IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;

        PlayerSetup.Instance.gameObject.AddComponent<CVRGizmoManager>();
    }

    public void CVRGizmosEnabled(object arg1, object arg2)
    {
        if (!CVRGizmoManager.Instance) return;
        CVRGizmoManager.Instance.EnableGizmos(EntryEnabled.Value);
    }

    public void CVRGizmosLocalOnly(object arg1, object arg2)
    {
        if (!CVRGizmoManager.Instance) return;
        CVRGizmoManager.Instance.g_localOnly = EntryLocalOnly.Value;
        CVRGizmoManager.Instance.RefreshGizmos();
    }
}