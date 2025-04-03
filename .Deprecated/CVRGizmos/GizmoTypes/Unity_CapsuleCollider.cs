using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_CapsuleCollider : CVRGizmoBase
{
    public static CapsuleCollider[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(CapsuleCollider)) as CapsuleCollider[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (CapsuleCollider)item);
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
                CapsuleCollider capsule = references[i];

                Gizmos.Color = Color.green;
                Vector3 position = capsule.transform.position;
                Quaternion rotation = capsule.transform.rotation;
                Vector3 lossyScale = capsule.transform.lossyScale;
                float maxLossyScale = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z);
                Vector3 scaledCenter = Vector3.Scale(capsule.center, lossyScale);
                float scaledRadius = capsule.radius * maxLossyScale;
                float scaledHeight = capsule.height * maxLossyScale;

                Gizmos.Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

                // Draw top sphere
                Vector3 topSphereOffset = Vector3.zero;
                topSphereOffset[capsule.direction] = (scaledHeight - (2 * scaledRadius)) / 2;
                Gizmos.Sphere(scaledCenter + topSphereOffset, scaledRadius);

                // Draw bottom sphere
                Vector3 bottomSphereOffset = Vector3.zero;
                bottomSphereOffset[capsule.direction] = -(scaledHeight - (2 * scaledRadius)) / 2;
                Gizmos.Sphere(scaledCenter + bottomSphereOffset, scaledRadius);

                // Draw cylinder
                int cylinderResolution = 24;
                Vector3 previousTopPoint = Vector3.zero;
                Vector3 previousBottomPoint = Vector3.zero;
                for (int j = 0; j <= cylinderResolution; j++)
                {
                    float angle = j * 2 * Mathf.PI / cylinderResolution;
                    Vector3 directionVector = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    directionVector[capsule.direction] = 0;
                    Vector3 topPoint = (scaledCenter + topSphereOffset) + directionVector * scaledRadius;
                    Vector3 bottomPoint = (scaledCenter + bottomSphereOffset) + directionVector * scaledRadius;

                    if (j > 0)
                    {
                        Gizmos.Line(previousTopPoint, topPoint);
                        Gizmos.Line(previousBottomPoint, bottomPoint);
                        Gizmos.Line(previousTopPoint, previousBottomPoint);
                        Gizmos.Line(topPoint, bottomPoint);
                    }

                    previousTopPoint = topPoint;
                    previousBottomPoint = bottomPoint;
                }
            }
        }
    }
}