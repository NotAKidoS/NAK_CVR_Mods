using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace CVRGizmos.GismoTypes
{
    public class CVRGizmos_HapticZone : CVRGizmoBase
    {
        public static CVRHapticZone[] references;

        public override void CacheGizmos()
        {
            var found = Resources.FindObjectsOfTypeAll(typeof(CVRHapticZone)) as CVRHapticZone[];

            if (CVRGizmoManager.Instance.g_localOnly)
            {
                references = Array.ConvertAll(GetLocalOnly(found), item => (CVRHapticZone)item);
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
                Gizmos.Color = Color.yellow;
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, references[i].transform.lossyScale);
                if (references[i].triggerForm == CVRHapticZone.TriggerForm.Box)
                {
                    Gizmos.Cube(references[i].center, Quaternion.identity, references[i].bounds);
                    return;
                }
                Gizmos.Sphere(references[i].center, references[i].bounds.x);
            }
        }
    }
}