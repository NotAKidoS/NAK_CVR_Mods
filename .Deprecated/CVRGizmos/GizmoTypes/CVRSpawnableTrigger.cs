using ABI.CCK.Components;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

/**

CVRSpawnableTrigger **can** be local using CVROfflinePreview or similar mods.

**/

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_SpawnableTrigger : CVRGizmoBase
{
    public static CVRSpawnableTrigger[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(CVRSpawnableTrigger)) as CVRSpawnableTrigger[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (CVRSpawnableTrigger)item);
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

                Gizmos.Color = Color.blue;
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, references[i].transform.lossyScale);

                Gizmos.Cube(references[i].areaOffset, Quaternion.identity, references[i].areaSize);

                //stayTask colors
                if (references[i].stayTasks.Count > 0)
                {
                    for (int ii = 0; ii < references[i].stayTasks.Count(); ii++)
                    {
                        var stayTask = references[i].stayTasks[ii];
                        if (stayTask.spawnable != null)
                        {
                            float num = stayTask.spawnable.GetValue(stayTask.settingIndex);
                            switch (stayTask.updateMethod)
                            {
                                case CVRSpawnableTriggerTaskStay.UpdateMethod.SetFromPosition:
                                    {
                                        num = Mathf.InverseLerp(stayTask.minValue, stayTask.maxValue, num);
                                        Gizmos.Color = new Color(2.0f * num, 2.0f * (1 - num), 0);
                                        break;
                                    }
                                case CVRSpawnableTriggerTaskStay.UpdateMethod.Add:
                                    num = num + stayTask.minValue / 60f;
                                    Gizmos.Color = new Color(2.0f * num, 2.0f * (1 - num), 0);
                                    break;
                                case CVRSpawnableTriggerTaskStay.UpdateMethod.Subtract:
                                    num = num - stayTask.minValue / 60f;
                                    Gizmos.Color = new Color(2.0f * num, 2.0f * (1 - num), 0);
                                    break;
                                default:
                                    return;
                            }
                        }
                    }

                    Vector3 vector = new Vector3(references[i].areaSize.x * 0.5f, references[i].areaSize.y * 0.5f, references[i].areaSize.z * 0.5f);
                    switch (references[i].sampleDirection)
                    {
                        case CVRSpawnableTrigger.SampleDirection.XPositive:
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(vector.x, vector.y, 0f) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(vector.x, vector.y, 0f) + references[i].areaOffset);
                            break;
                        case CVRSpawnableTrigger.SampleDirection.XNegative:
                            Gizmos.Line(new Vector3(vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, vector.y, 0f) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, vector.y, 0f) + references[i].areaOffset);
                            break;
                        case CVRSpawnableTrigger.SampleDirection.YPositive:
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, vector.y, 0f) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, vector.y, 0f) + references[i].areaOffset);
                            break;
                        case CVRSpawnableTrigger.SampleDirection.YNegative:
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, -vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, -vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, -vector.y, 0f) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, -vector.y, 0f) + references[i].areaOffset);
                            break;
                        case CVRSpawnableTrigger.SampleDirection.ZPositive:
                            Gizmos.Line(new Vector3(-vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, vector.z) + references[i].areaOffset);
                            break;
                        case CVRSpawnableTrigger.SampleDirection.ZNegative:
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, -vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, -vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, -vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, -vector.z) + references[i].areaOffset);
                            break;
                        default:
                            break;

                    }
                }
            }
        }
    }
}