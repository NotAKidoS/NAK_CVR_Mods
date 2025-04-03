using ABI.CCK.Components;
using ABI_RC.Core.Player;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace NAK.CVRGizmos.GismoTypes;

public class CVRGizmos_AdvancedAvatarSettingsTrigger : CVRGizmoBase
{
    public static CVRAdvancedAvatarSettingsTrigger[] references;

    public override void CacheGizmos()
    {
        var found = Resources.FindObjectsOfTypeAll(typeof(CVRAdvancedAvatarSettingsTrigger)) as CVRAdvancedAvatarSettingsTrigger[];

        if (CVRGizmoManager.Instance.g_localOnly)
        {
            references = Array.ConvertAll(GetLocalOnly(found), item => (CVRAdvancedAvatarSettingsTrigger)item);
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
                Gizmos.Color = Color.cyan;
                Gizmos.Matrix = Matrix4x4.TRS(references[i].transform.position, references[i].transform.rotation, references[i].transform.lossyScale);
                Gizmos.Cube(references[i].areaOffset, Quaternion.identity, references[i].areaSize);
                if (references[i].stayTasks.Count > 0)
                {
                    for (int ii = 0; ii < references[i].stayTasks.Count(); ii++)
                    {
                        var stayTask = references[i].stayTasks[ii];
                        float num = PlayerSetup.Instance.GetAnimatorParam(stayTask.settingName);
                        switch (stayTask.updateMethod)
                        {
                            case CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.SetFromPosition:
                                {
                                    num = Mathf.InverseLerp(stayTask.minValue, stayTask.maxValue, num);
                                    Gizmos.Color = new Color(2.0f * num, 2.0f * (1 - num), 0);
                                    break;
                                }
                            case CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.Add:
                                num = num + stayTask.minValue / 60f;
                                Gizmos.Color = new Color(2.0f * num, 2.0f * (1 - num), 0);
                                break;
                            case CVRAdvancedAvatarSettingsTriggerTaskStay.UpdateMethod.Subtract:
                                num = num - stayTask.minValue / 60f;
                                Gizmos.Color = new Color(2.0f * num, 2.0f * (1 - num), 0);
                                break;
                            default:
                                return;
                        }
                    }
                    Vector3 vector = new Vector3(references[i].areaSize.x * 0.5f, references[i].areaSize.y * 0.5f, references[i].areaSize.z * 0.5f);
                    switch (references[i].sampleDirection)
                    {
                        case CVRAdvancedAvatarSettingsTrigger.SampleDirection.XPositive:
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(vector.x, vector.y, 0f) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(vector.x, vector.y, 0f) + references[i].areaOffset);
                            break;
                        case CVRAdvancedAvatarSettingsTrigger.SampleDirection.XNegative:
                            Gizmos.Line(new Vector3(vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, vector.y, 0f) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, vector.y, 0f) + references[i].areaOffset);
                            break;
                        case CVRAdvancedAvatarSettingsTrigger.SampleDirection.YPositive:
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, vector.y, 0f) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, vector.y, 0f) + references[i].areaOffset);
                            break;
                        case CVRAdvancedAvatarSettingsTrigger.SampleDirection.YNegative:
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, -vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(0f, -vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, -vector.y, 0f) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, vector.z) + references[i].areaOffset, new Vector3(-vector.x, -vector.y, 0f) + references[i].areaOffset);
                            break;
                        case CVRAdvancedAvatarSettingsTrigger.SampleDirection.ZPositive:
                            Gizmos.Line(new Vector3(-vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(0f, vector.y, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, vector.z) + references[i].areaOffset);
                            Gizmos.Line(new Vector3(-vector.x, -vector.y, -vector.z) + references[i].areaOffset, new Vector3(-vector.x, 0f, vector.z) + references[i].areaOffset);
                            break;
                        case CVRAdvancedAvatarSettingsTrigger.SampleDirection.ZNegative:
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