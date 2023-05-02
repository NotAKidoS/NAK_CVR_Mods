using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_SphereCollider : CVRGizmoBase
{
    public static SphereCollider[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(SphereCollider)) as SphereCollider[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (SphereCollider)item);
        }
        else
        {
            references = found;
        }
    }

    public override void DrawGizmos()
    {
        for (int i = 0; i < references.Length; i++)
        {
            if (references[i] == null)
            {
                CacheGizmos();
                break;
            }
            if (references[i].gameObject.activeInHierarchy)
            {
                Gizmos.Color = Color.green;
                Vector3 position = references[i].transform.position;
                Quaternion rotation = references[i].transform.rotation;
                Vector3 lossyScale = references[i].transform.lossyScale;
                float maxLossyScale = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z);
                Vector3 scaledCenter = Vector3.Scale(references[i].center, lossyScale);
                float scaledRadius = references[i].radius * maxLossyScale;

                Gizmos.Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
                Gizmos.Sphere(scaledCenter, scaledRadius);
            }
        }
    }

}