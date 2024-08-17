using System.Text;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.UI;
using ABI.CCK.Components;
using ABI.CCK.Components.ScriptableObjects;
using UnityEngine;

namespace NAK.CVRLuaToolsExtension;

public static class LuaHotReloadManager
{
    private static readonly Dictionary<string, List<int>> s_AssetIdToLuaClientBehaviourIds = new();
    private static readonly Dictionary<int, CVRLuaClientBehaviour> s_LuaComponentIdsToLuaClientBehaviour = new();

    #region Game Events

    public static void OnCVRLuaBaseBehaviourLoadAndRunScript(CVRLuaClientBehaviour clientBehaviour)
    {
        //CVRLuaToolsExtensionMod.Logger.Msg($"[LuaHotReloadManager] Script awake: {clientBehaviour.name}");

        if (!clientBehaviour.IsScriptEligibleForHotReload())
            return;

        var assetId = clientBehaviour.GetAssetIdFromScript();
        if (!s_AssetIdToLuaClientBehaviourIds.ContainsKey(assetId))
            s_AssetIdToLuaClientBehaviourIds[assetId] = new List<int>();

        var luaComponentId = GetGameObjectPathHashCode(clientBehaviour.transform);
        if (s_AssetIdToLuaClientBehaviourIds[assetId].Contains(luaComponentId))
        {
            CVRLuaToolsExtensionMod.Logger.Warning(
                $"[LuaHotReloadManager] Script already exists: {clientBehaviour.name}");
            return;
        }

        s_AssetIdToLuaClientBehaviourIds[assetId].Add(luaComponentId);
        s_LuaComponentIdsToLuaClientBehaviour[luaComponentId] = clientBehaviour;
        CVRLuaToolsExtensionMod.Logger.Msg($"[LuaHotReloadManager] Added script: {clientBehaviour.name}");
    }

    public static void OnCVRLuaBaseBehaviourDestroy(CVRLuaClientBehaviour clientBehaviour)
    {
        //CVRLuaToolsExtensionMod.Logger.Msg($"[LuaHotReloadManager] Script destroy: {clientBehaviour.name}");

        var assetId = clientBehaviour.GetAssetIdFromScript();
        if (!s_AssetIdToLuaClientBehaviourIds.ContainsKey(assetId))
            return;

        var luaClientBehaviourIds = s_AssetIdToLuaClientBehaviourIds[assetId];
        foreach (var luaComponentId in luaClientBehaviourIds)
        {
            if (!s_LuaComponentIdsToLuaClientBehaviour.TryGetValue(luaComponentId,
                out CVRLuaClientBehaviour luaClientBehaviour))
                continue;

            if (luaClientBehaviour != clientBehaviour)
                continue;

            s_LuaComponentIdsToLuaClientBehaviour.Remove(luaComponentId);
            luaClientBehaviourIds.Remove(luaComponentId);
            if (luaClientBehaviourIds.Count == 0) s_AssetIdToLuaClientBehaviourIds.Remove(assetId);
            CVRLuaToolsExtensionMod.Logger.Msg($"[LuaHotReloadManager] Removed script: {clientBehaviour.name}");
            break;
        }
    }
    
    public static void OnReceiveUpdatedScript(ScriptInfo info)
    {
        if (!s_AssetIdToLuaClientBehaviourIds.TryGetValue(info.AssetId, out var luaComponentIds))
        {
            CVRLuaToolsExtensionMod.Logger.Warning(
                $"[LuaHotReloadManager] No scripts found for asset id: {info.AssetId}");
            return;
        }

        bool found = false;
        foreach (var luaComponentId in luaComponentIds)
        {
            if (!s_LuaComponentIdsToLuaClientBehaviour.TryGetValue(luaComponentId,
                    out CVRLuaClientBehaviour clientBehaviour))
                continue;

            found = true;
            //CVRLuaToolsExtensionMod.Logger.Msg($"[LuaHotReloadManager] Reloading script: {info.ScriptName} for {clientBehaviour.name}");

            if (clientBehaviour.asset.m_ScriptPath == info.ScriptPath)
            {
                CVRLuaToolsExtensionMod.Logger.Msg("[LuaHotReloadManager] Script path match, updating script.");
                clientBehaviour.asset.name = info.ScriptName;
                clientBehaviour.asset.m_ScriptText = info.ScriptText;
                clientBehaviour.Restart();
            }
            else
            {
                CVRLuaToolsExtensionMod.Logger.Msg("[LuaHotReloadManager] Script path mismatch, creating new script.");

                clientBehaviour.asset = null;
                clientBehaviour.asset = ScriptableObject.CreateInstance<CVRLuaScript>();

                clientBehaviour.asset.name = info.ScriptName;
                clientBehaviour.asset.m_ScriptPath = info.ScriptPath;
                clientBehaviour.asset.m_ScriptText = info.ScriptText;

                clientBehaviour.Restart();
            }
        }

        if (found) CohtmlHud.Instance.ViewDropTextImmediate("(Local) CVRLuaTools", "Received script update", "Reloaded script: " + info.ScriptName);
    }

    #endregion Game Events

    #region Private Methods
    
    private static int GetGameObjectPathHashCode(Transform transform)
    {
        // Attempt to find the root component transform in one step

        Transform rootComponentTransform = null;
        
        // both CVRAvatar & CVRSpawnable *should* have an asset info component
        CVRAssetInfo rootComponent = transform.GetComponentInParent<CVRAssetInfo>(true);
        if (rootComponent != null && rootComponent.type != CVRAssetInfo.AssetType.World)
            rootComponentTransform = rootComponent.transform;
        
        // easy case, no need to crawl up the hierarchy
        if (rootComponentTransform == transform)
            return 581452743; // hash code for "[Root]"

        StringBuilder pathBuilder = new(transform.name);
        Transform parentTransform = transform.parent;
        
        while (parentTransform != null)
        {
            // reached root component
            // due to object loader renaming root, we can't rely on transform name, so we use "[Root]" instead
            if (parentTransform == rootComponentTransform)
            {
                pathBuilder.Insert(0, "[Root]/");
                break;
            }

            pathBuilder.Insert(0, parentTransform.name + "/");
            parentTransform = parentTransform.parent;
        }

        string path = pathBuilder.ToString();

        //Debug.Log($"[LuaComponentManager] Path: {path}");

        return path.GetHashCode();
    }
    
    #endregion Private Methods
}