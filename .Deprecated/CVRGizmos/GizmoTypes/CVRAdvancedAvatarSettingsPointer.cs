using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

/**

CVRAdvancedAvatarSettingsPointer shouldn't really be used at this point. Only including because it still exists in the CCK/Game.

**/

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_AdvancedAvatarSettingsPointer : CVRGizmoBase
{
    public static CVRAdvancedAvatarSettingsPointer[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(CVRAdvancedAvatarSettingsPointer)) as CVRAdvancedAvatarSettingsPointer[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (CVRAdvancedAvatarSettingsPointer)item);
        }
        else
        {
            references = found;
        }
    }

    public override void DrawGizmos()
    {
        for (int i = 0; i < references.Count(); i++)
        {
            if (references[i] == null)
            {
                CacheGizmos();
                break;
            }
            if (references[i].isActiveAndEnabled)
            {
                Gizmos.Color = Color.cyan;
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, references[i].transform.lossyScale);
                Gizmos.Sphere(Vector3.zero, 0.015f);
            }
        }
    }
}