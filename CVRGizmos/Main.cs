using ABI_RC.Core.Player;
using MelonLoader;
using System.Collections;

namespace CVRGizmos;

public class CVRGizmos : MelonMod
{

    private static MelonPreferences_Category m_categoryCVRGizmos;
    private static MelonPreferences_Entry<bool> m_entryCVRGizmosEnabled;
    private static MelonPreferences_Entry<bool> m_entryCVRGizmosLocalOnly;

    public override void OnApplicationStart()
    {
        m_categoryCVRGizmos = MelonPreferences.CreateCategory(nameof(CVRGizmos));
        m_entryCVRGizmosEnabled = m_categoryCVRGizmos.CreateEntry<bool>("Enabled", false);
        m_entryCVRGizmosLocalOnly = m_categoryCVRGizmos.CreateEntry<bool>("Local Only", false);
        m_entryCVRGizmosEnabled.Value = false;

        m_categoryCVRGizmos.SaveToFile(false);
        m_entryCVRGizmosEnabled.OnValueChangedUntyped += CVRGizmosEnabled;
        m_entryCVRGizmosLocalOnly.OnValueChangedUntyped += CVRGizmosLocalOnly;

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
        CVRGizmoManager.Instance.EnableGizmos(m_entryCVRGizmosEnabled.Value);
    }

    public void CVRGizmosLocalOnly()
    {
        CVRGizmoManager.Instance.g_localOnly = m_entryCVRGizmosLocalOnly.Value;
        CVRGizmoManager.Instance.RefreshGizmos();
    }
}