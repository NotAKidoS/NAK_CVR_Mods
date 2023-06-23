using ABI_RC.Core.Player;
using HarmonyLib;

namespace NAK.AvatarScaleMod.HarmonyPatches;

class PlayerSetupPatches
{
    /**
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
        AvatarScaleMod.HiddenAvatarScale.Value = Utils.CalculateParameterValue(____initialAvatarHeight);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.ClearAvatar))]
    static void Prefix_PlayerSetup_ClearAvatar(ref PlayerSetup __instance, ref float ____avatarHeight)
    {
        if (!AvatarScaleMod.EntryEnabled.Value) return;

        if (!Utils.IsSupportedAvatar(__instance.animatorManager) && !AvatarScaleMod.EntryPersistAnyways.Value)
        {
            return;
        }
        
        AvatarScaleMod.HiddenAvatarScale.Value = Utils.CalculateParameterValue(____avatarHeight);
    }
    **/

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatar))]
    static void Postfix_PlayerSetup_SetupAvatar(ref PlayerSetup __instance)
    {
        try
        {
            __instance._avatar.AddComponent<AvatarScaleManager>().Initialize(__instance._initialAvatarHeight, __instance.initialScale);
        }
        catch (Exception e)
        {
            AvatarScaleMod.Logger.Error($"Error during the patched method {nameof(Postfix_PlayerSetup_SetupAvatar)}");
            AvatarScaleMod.Logger.Error(e);
        }
    }
}