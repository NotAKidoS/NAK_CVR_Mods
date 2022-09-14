using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

/**

CVRToggleStateTrigger **can** be local using CVROfflinePreview or similar mods.

**/

namespace CVRGizmos.GismoTypes
{
    public class CVRGizmos_ToggleStateTrigger : CVRGizmoBase
    {
        public static CVRToggleStateTrigger[] references;

        public override void CacheGizmos()
        {
            var found = Resources.FindObjectsOfTypeAll(typeof(CVRToggleStateTrigger)) as CVRToggleStateTrigger[];

            if (CVRGizmoManager.Instance.g_localOnly)
            {
                references = Array.ConvertAll(GetLocalOnly(found), item => (CVRToggleStateTrigger)item);
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
                    Gizmos.Color = Color.green;
                    Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, references[i].transform.lossyScale);
                    Gizmos.Cube(references[i].areaOffset, Quaternion.identity, references[i].areaSize);
                }
            }
        }
    }
}