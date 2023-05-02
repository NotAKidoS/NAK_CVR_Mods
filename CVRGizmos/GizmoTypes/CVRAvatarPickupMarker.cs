using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_AvatarPickupMarker : CVRGizmoBase
{
    public static CVRAvatarPickupMarker[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(CVRAvatarPickupMarker)) as CVRAvatarPickupMarker[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (CVRAvatarPickupMarker)item);
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
                Gizmos.Color = Color.magenta;
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, references[i].transform.lossyScale);
                Gizmos.Cube(new Vector3(0f, 0.75f, 0f), Quaternion.identity, new Vector3(1f, 1.5f, 0f));
                Gizmos.Cube(new Vector3(0f, 0.7f, 0f), Quaternion.identity, new Vector3(0.8f, 0.1f, 0f));
                Gizmos.Cube(new Vector3(0f, 0.615f, 0f), Quaternion.identity, new Vector3(0.6f, 0.07f, 0f));
                Gizmos.Cube(new Vector3(0.24f, 0.28f, 0f), Quaternion.identity, new Vector3(0.32f, 0.42f, 0f));
                Gizmos.Cube(new Vector3(-0.24f, 0.28f, 0f), Quaternion.identity, new Vector3(0.32f, 0.42f, 0f));
                Vector3 lossyScale = references[i].transform.lossyScale;
                lossyScale.Scale(new Vector3(1f, 1f, 0f));
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, lossyScale);
                Gizmos.Sphere(new Vector3(0f, 1.11f, 0f), 0.31f);
            }
        }
    }
}