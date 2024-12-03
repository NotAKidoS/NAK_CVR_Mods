using ABI_RC.Core.Base;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI.CCK.Components;
using ABI.Scripting.CVRSTL.Client;
using ABI.Scripting.CVRSTL.Common;
using HarmonyLib;
using MoonSharp.Interpreter;
using NAK.LuaNetVars.Modules;
using UnityEngine;

namespace NAK.LuaNetVars.Patches;

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

internal static class CVRSyncHelper_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVRSyncHelper), nameof(CVRSyncHelper.UpdatePropValues))]
    private static void Postfix_CVRSyncHelper_UpdatePropValues(
        Vector3 position, Vector3 rotation, Vector3 scale, 
        float[] syncValues, string guid, string instanceId,
        Span<float> subSyncValues, int numSyncValues, int syncType = 0)
    {
        CVRSyncHelper.PropData propData = CVRSyncHelper.Props.Find(prop => prop.InstanceId == instanceId);
        if (propData == null) return;
        
        // Update locally stored prop data with new values
        // as GS does not reply with our own data...
        
        propData.PositionX = position.x;
        propData.PositionY = position.y;
        propData.PositionZ = position.z;
        propData.RotationX = rotation.x;
        propData.RotationY = rotation.y;
        propData.RotationZ = rotation.z;
        propData.ScaleX = scale.x;
        propData.ScaleY = scale.y;
        propData.ScaleZ = scale.z;
        propData.CustomFloatsAmount = numSyncValues;
        for (int i = 0; i < numSyncValues; i++)
            propData.CustomFloats[i] = syncValues[i];
        
        //propData.SpawnedBy
        propData.syncedBy = MetaPort.Instance.ownerId;
        propData.syncType = syncType;
    }
}