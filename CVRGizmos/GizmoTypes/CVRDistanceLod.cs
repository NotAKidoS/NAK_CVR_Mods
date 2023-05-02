using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_DistanceLod : CVRGizmoBase
{
    public static CVRDistanceLod[] references;

    private static Color[] _gizmoColors = new Color[]
    {
        Color.green,
        Color.yellow,
        Color.red,
        Color.white
    };

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(CVRDistanceLod)) as CVRDistanceLod[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (CVRDistanceLod)item);
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
            if (!references[i].distance3D)
            {
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, Quaternion.identity, new Vector3(1f, 0f, 1f));
            }
            else
            {
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, Quaternion.identity, Vector3.one);
            }
            float num = 0;
            foreach (CVRDistanceLodGroup cvrdistanceLodGroup in references[i].Groups)
            {
                //Gizmos.Color = _gizmoColors[Math.Min(num, 3)];
                num = Mathf.InverseLerp(0, references[i].Groups.Count, num);
                Gizmos.Color = new Color(2.0f * num, 2.0f * (1 - num), 0);
                Gizmos.Sphere(Vector3.zero, cvrdistanceLodGroup.MaxDistance);
                num++;
            }
        }
    }
}