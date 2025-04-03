using HarmonyLib;
using ABI_RC.Core.Player;

namespace NAK.InteractionTest.HarmonyPatches;

class PuppetMasterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), nameof(PuppetMaster.AvatarInstantiated))]
    static void Postfix_PuppetMaster_SetupAvatar(ref PuppetMaster __instance)
    {
        __instance.avatarObject.AddComponent<AvatarColliders>();
    }
}

class PlayerSetupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetupAvatar))]
    static void Postfix_PlayerSetup_SetupAvatar(ref PlayerSetup __instance)
    {
        __instance._avatar.AddComponent<AvatarColliders>();
    }
}