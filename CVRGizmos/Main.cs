using ABI_RC.Core.Player;
using MelonLoader;
using System.Collections;

namespace CVRGizmos;

public class CVRGizmos : MelonMod
{
    public static readonly MelonPreferences_Category CategoryCVRGizmos = 
        MelonPreferences.CreateCategory(nameof(CVRGizmos));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        CategoryCVRGizmos.CreateEntry("Enabled", false, description: "Toggle CVR Gizmos entirely.", dont_save_default: true);

    public static readonly MelonPreferences_Entry<bool> EntryLocalOnly =
        CategoryCVRGizmos.CreateEntry("Local Only", false, description: "Toggle CVR Gizmos local-only mode.", dont_save_default: true);

    public override void OnInitializeMelon()
    {
        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }

    IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;

        PlayerSetup.Instance.gameObject.AddComponent<CVRGizmoManager>();
    }

    public void CVRGizmosEnabled()
    {
        if (!CVRGizmoManager.Instance) return;
        CVRGizmoManager.Instance.EnableGizmos(EntryEnabled.Value);
    }

    public void CVRGizmosLocalOnly()
    {
        if (!CVRGizmoManager.Instance) return;
        CVRGizmoManager.Instance.g_localOnly = EntryLocalOnly.Value;
        CVRGizmoManager.Instance.RefreshGizmos();
    }
}