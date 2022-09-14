using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace CVRGizmos.GismoTypes
{
    public class CVRGizmos_Avatar : CVRGizmoBase
    {
        public static CVRAvatar[] references;

        public override void CacheGizmos()
        {
            var found = Resources.FindObjectsOfTypeAll(typeof(CVRAvatar)) as CVRAvatar[];

            if (CVRGizmoManager.Instance.g_localOnly)
            {
                references = Array.ConvertAll(GetLocalOnly(found), item => (CVRAvatar)item);
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
                    //viewPosition & voicePosition seem to be rounded... not good, may be why viewpoint drift is bad on scale
                    Gizmos.Color = Color.green;
                    Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, references[i].transform.localScale);
                    Gizmos.Sphere(references[i].viewPosition, 0.01f);
                    Gizmos.Color = Color.red;
                    Gizmos.Sphere(references[i].voicePosition, 0.01f);
                }
            }
        }
    }
}