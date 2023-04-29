using ABI_RC.Core;
using ABI_RC.Core.Player;
using HarmonyLib;

namespace NAK.AvatarScaleMod.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatar))]
    static void Postfix_PlayerSetup_SetupAvatar(ref PlayerSetup __instance, ref float ____initialAvatarHeight)
    {
        if (!AvatarScaleMod.EntryEnabled.Value) return;

        if (AvatarScaleMod.HiddenAvatarScale.Value > 0f)
        {
            __instance.changeAnimatorParam(AvatarScaleMod.ParameterName, AvatarScaleMod.HiddenAvatarScale.Value);
            return;
        }

        // User has cleared MelonPrefs, store a default value.
        AvatarScaleMod.HiddenAvatarScale.Value = CalculateParameterValue(____initialAvatarHeight);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ClearAvatar))]
    static void Prefix_PlayerSetup_ClearAvatar(ref PlayerSetup __instance, ref float ____avatarHeight)
    {
        if (!AvatarScaleMod.EntryEnabled.Value) return;

        if (!IsSupportedAvatar(__instance.animatorManager) && !AvatarScaleMod.EntryPersistAnyways.Value)
        {
            return;
        }

        AvatarScaleMod.HiddenAvatarScale.Value = CalculateParameterValue(____avatarHeight);
    }

    public static float CalculateParameterValue(float lastAvatarHeight)
    {
        float t = (lastAvatarHeight - AvatarScaleMod.MinimumHeight) / (AvatarScaleMod.MaximumHeight - AvatarScaleMod.MinimumHeight);
        return t;
    }

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
}