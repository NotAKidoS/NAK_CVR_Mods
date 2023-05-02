using ABI.CCK.Components;
using HarmonyLib;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_HapticAreaChest : CVRGizmoBase
{
    public static CVRHapticAreaChest[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(CVRHapticAreaChest)) as CVRHapticAreaChest[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (CVRHapticAreaChest)item);
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
                Gizmos.Color = Color.yellow;
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, references[i].transform.lossyScale);
                Gizmos.Cube(Vector3.zero, Quaternion.identity, references[i].chestAreaSize);
                int num = 0;
                foreach (Vector3 center in references[i].HapticPoints40)
                {
                    float[] pointValues = Traverse.Create(references[i]).Field("pointValues").GetValue() as float[];
                    center.Scale(references[i].chestAreaSize * 0.5f);
                    Gizmos.Color = new Color(1f - pointValues[num], pointValues[num], 0f);
                    Gizmos.Cube(center, Quaternion.identity, new Vector3(0.01f, 0.01f, 0.01f));
                    num++;
                }
            }
        }
    }
}