using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace CVRGizmos.GismoTypes
{
    public class CVRGizmos_DistanceConstrain : CVRGizmoBase
    {
        public static CVRDistanceConstrain[] references;

        public override void CacheGizmos()
        {
            var found = Resources.FindObjectsOfTypeAll(typeof(CVRDistanceConstrain)) as CVRDistanceConstrain[];

            if (CVRGizmoManager.Instance.g_localOnly)
            {
                references = Array.ConvertAll(GetLocalOnly(found), item => (CVRDistanceConstrain)item);
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
                if (references[i].target == null)
                {
                    break;
                }
                if (references[i].maxDistance < references[i].minDistance && references[i].maxDistance != 0f)
                {
                    break;
                }
                Vector3 normalized = (references[i].transform.position - references[i].target.position).normalized;

                //BUG: Matrix addition isn't reset, other gizmo types Matrix will persist.
                //This gizmo type could be a bit fucked, but I don't have the time to test.
                Gizmos.Matrix = Matrix4x4.identity;

                if (references[i].minDistance == 0f)
                {
                    if (references[i].maxDistance == 0f)
                    {
                        Gizmos.Color = Color.green;
                        Gizmos.Line(references[i].target.position, normalized * 9999f);
                        break;
                    }
                    Gizmos.Color = Color.green;
                    Gizmos.Line(references[i].target.position, references[i].target.position + normalized * references[i].maxDistance);
                    break;
                }
                else
                {
                    if (references[i].maxDistance == 0f)
                    {
                        Gizmos.Color = Color.red;
                        Gizmos.Line(references[i].target.position, references[i].target.position + normalized * references[i].minDistance);
                        Gizmos.Color = Color.green;
                        Gizmos.Line(references[i].target.position + normalized * references[i].minDistance, normalized * 9999f);
                        break;
                    }
                    Gizmos.Color = Color.red;
                    Gizmos.Line(references[i].target.position, references[i].target.position + normalized * references[i].minDistance);
                    Gizmos.Color = Color.green;
                    Gizmos.Line(references[i].target.position + normalized * references[i].minDistance, references[i].target.position + normalized * references[i].maxDistance);
                    break;
                }
            }
        }
    }
}