using ABI_RC.Core;

namespace NAK.AvatarScaleMod;

class Utils
{
    public static bool IsSupportedAvatar(CVRAnimatorManager manager)
    {
        if (manager.animatorParameterFloatList.Contains(AvatarScaleMod.ParameterName) && manager._animator != null)
        {
            if (manager._advancedAvatarIndicesFloat.TryGetValue(AvatarScaleMod.ParameterName, out int index))
            {
                return index < manager._advancedAvatarCacheFloat.Count;
            }
        }
        return false;
    }

    public static float CalculateParameterValue(float lastAvatarHeight)
    {
        float t = (lastAvatarHeight - AvatarScaleMod.MinimumHeight) / (AvatarScaleMod.MaximumHeight - AvatarScaleMod.MinimumHeight);
        return t;
    }
}
