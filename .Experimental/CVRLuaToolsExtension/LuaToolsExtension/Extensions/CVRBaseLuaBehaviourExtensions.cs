using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI.CCK.Components;
using ABI.Scripting.CVRSTL.Common;
using UnityEngine;

namespace NAK.CVRLuaToolsExtension;

public static class CVRBaseLuaBehaviourExtensions
{
    // check if the script is eligible for hot reload
    public static bool IsScriptEligibleForHotReload(this CVRBaseLuaBehaviour script)
    {
        return script.Context.IsWornByMe // avatar if worn
               || script.Context.IsSpawnedByMe // prop if spawned
               || script.Context.objContext == CVRLuaObjectContext.WORLD; // always world scripts
    }
    
    // gets the asset id from the script
    public static string GetAssetIdFromScript(this CVRBaseLuaBehaviour script)
    {
        switch (script.Context.objContext)
        {
            case CVRLuaObjectContext.WORLD:
                //return MetaPort.Instance.CurrentWorldId; // do not trust CVRAssetInfo, can be destroyed at runtime
                return "SYSTEM"; // default for world scripts is SYSTEM, but TODO: use actual world id
            case CVRLuaObjectContext.AVATAR:
                Component rootComponent = script.Context.RootComponent;
                return rootComponent switch
                {
                    PlayerSetup => MetaPort.Instance.currentAvatarGuid, // local avatar
                    PuppetMaster puppetMaster => puppetMaster.CVRPlayerEntity.AvatarId, // remote avatar
                    _ => string.Empty // fuck
                };
            case CVRLuaObjectContext.PROP:
            {
                CVRSpawnable spawnable = (CVRSpawnable)script.Context.RootComponent;
                if (!string.IsNullOrEmpty(spawnable.guid)) return spawnable.guid; // after filtering has occured
                return spawnable.TryGetComponent(out CVRAssetInfo assetInfo) 
                    ? assetInfo.objectId // before filtering has occured
                    : string.Empty; // well shit
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}