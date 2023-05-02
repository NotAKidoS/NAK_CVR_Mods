using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_BoxCollider : CVRGizmoBase
{
    public static BoxCollider[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(BoxCollider)) as BoxCollider[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (BoxCollider)item);
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
                BoxCollider box = references[i];

                Gizmos.Color = Color.green;
                Vector3 position = box.transform.TransformPoint(box.center);
                Quaternion rotation = box.transform.rotation;
                Vector3 scaledSize = Vector3.Scale(box.size, box.transform.lossyScale);

                Gizmos.Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
                Gizmos.Cube(Vector3.zero, Quaternion.identity, scaledSize);
            }
        }
    }
}