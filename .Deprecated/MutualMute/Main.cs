using System.Reflection;
using ABI_RC.Systems.Communications.Audio.Components;
using HarmonyLib;
using MelonLoader;

namespace NAK.MutualMute;

public class MutualMuteMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(Comms_ParticipantPipeline).GetMethod(nameof(Comms_ParticipantPipeline.SetFlowControlState),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(MutualMuteMod).GetMethod(nameof(OnSetFlowControlState),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static void OnSetFlowControlState(
        ref bool state, 
        Comms_ParticipantPipeline __instance)
        => state &= !__instance._selfModerationMute;
}