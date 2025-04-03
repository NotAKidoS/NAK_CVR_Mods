using System.Reflection;
using System.Reflection.Emit;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;

namespace NAK.StopClosingMyMenuOnWorldLoad;

public class StopClosingMyMenuOnWorldLoad : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(CVRWorld_Patches));
    }
    
    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }

    #region Patches
    
    private static class CVRWorld_Patches
    {
        // prevents CVRWorld from closing menus when world transitioning, cause cool (taken from MenuScalePatch, kafe originally made the transpiler patch)
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CVRWorld), nameof(CVRWorld.Start), MethodType.Enumerator)]
        private static IEnumerable<CodeInstruction> Transpiler_CVRWorld_Start(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var patchedInstructions = new CodeMatcher(instructions).MatchForward(false,
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(i => i.opcode == OpCodes.Callvirt && i.operand is MethodInfo { Name: "ForceUiStatus" }))
                .RemoveInstructions(3)
                .InstructionEnumeration();

            patchedInstructions = new CodeMatcher(patchedInstructions).MatchForward(false,
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(i => i.opcode == OpCodes.Callvirt && i.operand is MethodInfo { Name: "ToggleQuickMenu" }))
                .RemoveInstructions(3)
                .InstructionEnumeration();

            return patchedInstructions;
        }
    }
    
    #endregion Patches
}