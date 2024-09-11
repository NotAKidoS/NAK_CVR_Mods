using ABI.Scripting.CVRSTL.Client;
using ABI.Scripting.CVRSTL.Common;
using HarmonyLib;
using MoonSharp.Interpreter;
using NAK.LuaTTS.Modules;

namespace NAK.LuaTTS.Patches;

internal static class LuaScriptFactoryPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LuaScriptFactory.CVRRequireModule), nameof(LuaScriptFactory.CVRRequireModule.Require))]
    private static void Postfix_CVRRequireModule_require(
        string modid, 
        ref LuaScriptFactory.CVRRequireModule __instance,
        ref object __result, 
        ref Script  ___script, 
        ref CVRLuaContext ___context)
    {
        const string TTSModuleID = "TextToSpeech";
        if (TTSModuleID != modid) 
            return; // not our module
        
        __result = TTSLuaModule.RegisterUserData(___script, ___context);
        __instance.RegisteredModules[TTSModuleID] = __result; // add module to cache
    }
}