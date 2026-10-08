using ABI_RC.Systems.IK;
using ABI_RC.Systems.OSC.Modules;
using ABI_RC.Systems.OSC.OSCQuery.Models;
using HarmonyLib;
using LucHeart.CoreOSC;
using MelonLoader;

namespace NAK.VRCFTHeadTracking;

public class VRCFTHeadTrackingMod : MelonMod
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(VRCFTHeadTrackingMod));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Apply VRCFT head data to your avatar head.");

    public static readonly MelonPreferences_Entry<bool> EntryApplyPosition =
        Category.CreateEntry("Apply Position", false, description: "Offset the head from the tracked head position. 1.0 tracked equals 0.5 meters.");

    public override void OnInitializeMelon()
    {
        try
        {
            HarmonyInstance.PatchAll(typeof(OSCAvatarModule_Patches));
            HarmonyInstance.PatchAll(typeof(OSCAvatarModule_QueryPatches));
            HarmonyInstance.PatchAll(typeof(IKSystem_Patches));
        }
        catch (Exception e)
        {
            LoggerInstance.Error(e);
        }
    }

    internal static class IKSystem_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(IKSystem), "InitializeIkGeneral")]
        private static void Postfix_IKSystem_InitializeIkGeneral() 
            => HeadIkDriver.Attach(IKSystem.vrik);
    }

    internal static class OSCAvatarModule_Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OSCAvatarModule), nameof(OSCAvatarModule.HandleIncoming))]
        private static void Prefix_OSCAvatarModule_HandleIncoming(OscMessage packet) 
            => HeadTrackingData.TryIngest(packet.Address, packet.Arguments);
    }

    internal static class OSCAvatarModule_QueryPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OSCAvatarModule), "RegisterFaceTrackingParameters")]
        private static void Postfix_OSCAvatarModule_RegisterFaceTrackingParameters(
            OSCAvatarModule __instance, Node<IDictionary<string, OscParameterNode>> parametersNode,
            List<string> extraParameters, ref bool __result)
        {
            if (ReferenceEquals(extraParameters, HeadTrackingData.ParameterNames)) return;
            __result |= __instance.RegisterFaceTrackingParameters(parametersNode, HeadTrackingData.ParameterNames);
        }
    }
}