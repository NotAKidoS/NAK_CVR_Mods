using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_Pointer : CVRGizmoBase
{
    public static CVRPointer[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(CVRPointer)) as CVRPointer[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (CVRPointer)item);
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
                Gizmos.Color = Color.blue;
                Vector3 lossyScale = references[i].transform.lossyScale;
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, lossyScale);
                Gizmos.Sphere(Vector3.zero, 0.00125f / Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z));
            }
        }
    }
}