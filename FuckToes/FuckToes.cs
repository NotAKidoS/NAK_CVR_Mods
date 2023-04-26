using ABI_RC.Core.Savior;
using HarmonyLib;
using RootMotion.FinalIK;


namespace NAK.FuckToes.HarmonyPatches;

//yes im patching VRIK directly, cvr does not force calibration or mess with references, and leaves it to vrik to handle
class VRIKPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(VRIK), "AutoDetectReferences")]
    private static void Postfix_VRIK_AutoDetectReferences(ref VRIK __instance)
    {
        //only run for PlayerLocal VRIK
        if (__instance.gameObject.layer != 8) return;

        if (FuckToesMod.m_entryEnabledVR.Value && MetaPort.Instance.isUsingVr)
        {
            if (!FuckToesMod.m_entryEnabledFBT.Value && MetaPort.Instance.isUsingFullbody) return;
            __instance.references.leftToes = null;
            __instance.references.rightToes = null;
        }
    }
}