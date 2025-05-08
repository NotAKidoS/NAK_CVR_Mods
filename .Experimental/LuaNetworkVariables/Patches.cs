using ABI.Scripting.CVRSTL.Client;
using ABI.Scripting.CVRSTL.Common;
using HarmonyLib;
using MoonSharp.Interpreter;
using NAK.LuaNetworkVariables.Modules;

namespace NAK.LuaNetworkVariables.Patches;

internal static class LuaScriptFactory_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LuaScriptFactory.CVRRequireModule), nameof(LuaScriptFactory.CVRRequireModule.Require))]
    private static void Postfix_CVRRequireModule_require(
        string moduleFriendlyName, 
        ref LuaScriptFactory.CVRRequireModule __instance,
        ref object __result, 
        ref Script  ____script, 
        ref CVRLuaContext ____context)
    {
        if (LuaNetModule.MODULE_ID != moduleFriendlyName) 
            return; // not our module
        
        __result = LuaNetModule.RegisterUserData(____script, ____context);
        __instance.RegisteredModules[LuaNetModule.MODULE_ID] = __result; // add module to cache
    }
}