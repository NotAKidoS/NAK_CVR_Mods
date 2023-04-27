using ABI_RC.Core.Player;
using HarmonyLib;

namespace NAK.AvatarScaleMod.HarmonyPatches;

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatarGeneral))]
    static void Postfix_PlayerSetup_SetupAvatarGeneral(ref PlayerSetup __instance, ref float ____initialAvatarHeight)
    {
        if (!AvatarScaleMod.EntryEnabled.Value) return;

        if (AvatarScaleMod.HiddenAvatarScale.Value > 0)
        {
            __instance.changeAnimatorParam(AvatarScaleMod.ParameterName, AvatarScaleMod.HiddenAvatarScale.Value);
            return;
        }

        // User has cleared MelonPrefs, store a default value.
        AvatarScaleMod.HiddenAvatarScale.Value = CalculateParameterValue(____initialAvatarHeight);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ClearAvatar))]
    static void Prefix_PlayerSetup_ClearAvatar(ref float ____avatarHeight)
    {
        if (!AvatarScaleMod.EntryEnabled.Value) return;

        AvatarScaleMod.HiddenAvatarScale.Value = CalculateParameterValue(____avatarHeight);
    }

    public static float CalculateParameterValue(float lastAvatarHeight)
    {
        float t = (lastAvatarHeight - AvatarScaleMod.MinimumHeight) / (AvatarScaleMod.MaximumHeight - AvatarScaleMod.MinimumHeight);
        return t;
    }
}