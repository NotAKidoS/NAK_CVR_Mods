
using ABI.Scripting.CVRSTL.Client;
using ABI.Scripting.CVRSTL.Common;
using HarmonyLib;
using MoonSharp.Interpreter;
using NAK.LuaTTS.Modules;

namespace NAK.LuaTTS.Patches;

internal static class LuaScriptFactoryPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LuaScriptFactory.CVRRequireModule), nameof(LuaScriptFactory.CVRRequireModule.require))]
    private static void Postfix_CVRRequireModule_require(string modid, 
        ref object __result, ref Script  ___script, CVRLuaContext ___context)
    {
        if (modid == "TextToSpeech")
            __result = TTSLuaModule.RegisterUserData(___script, ___context);
    }
}