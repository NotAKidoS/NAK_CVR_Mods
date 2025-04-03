using System.Text;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.UI;
using ABI.CCK.Components;
using ABI.CCK.Components.ScriptableObjects;
using UnityEngine;

namespace NAK.CVRLuaToolsExtension;

public static class LuaHotReloadManager
{
    // (asset id + lua component id) -> index is component reference
    private static readonly List<int> s_CombinedKeys = new();
    private static readonly List<CVRLuaClientBehaviour> s_LuaComponentInstances = new();

    #region Game Events

    public static void OnCVRLuaBaseBehaviourLoadAndRunScript(CVRLuaClientBehaviour clientBehaviour)
    {
        if (!clientBehaviour.IsScriptEligibleForHotReload())
            return;

        // check if the component is already in the list (shouldn't happen)
        if (s_LuaComponentInstances.Contains(clientBehaviour))
        {
            CVRLuaToolsExtensionMod.Logger.Warning($"[LuaHotReloadManager] Script already added: {clientBehaviour.name}");
            return;
        }
        
        // combine the assetId and instanceId into a single key, so multiple instances of the same script can be tracked
        string assetId = clientBehaviour.GetAssetIdFromScript();
        int instanceId = GetGameObjectPathHashCode(clientBehaviour.transform);
        int combinedKey = GenerateCombinedKey(assetId.GetHashCode(), instanceId);

        s_CombinedKeys.Add(combinedKey);
        s_LuaComponentInstances.Add(clientBehaviour);

        CVRLuaToolsExtensionMod.Logger.Msg($"[LuaHotReloadManager] Added script: {clientBehaviour.name}");
    }

    public static void OnCVRLuaBaseBehaviourDestroy(CVRLuaClientBehaviour clientBehaviour)
    {
        if (!clientBehaviour.IsScriptEligibleForHotReload())
            return;

        if (!s_LuaComponentInstances.Contains(clientBehaviour))
        {
            CVRLuaToolsExtensionMod.Logger.Warning($"[LuaHotReloadManager] Eligible for Hot Reload script destroyed without being tracked first: {clientBehaviour.name}");
            return;
        }
        
        int index = s_LuaComponentInstances.IndexOf(clientBehaviour);
        s_CombinedKeys.RemoveAt(index);
        s_LuaComponentInstances.RemoveAt(index);

        CVRLuaToolsExtensionMod.Logger.Msg($"[LuaHotReloadManager] Removed script: {clientBehaviour.name}");
    }

    public static void OnReceiveUpdatedScript(ScriptInfo info)
    {
        int combinedKey = GenerateCombinedKey(info.AssetId.GetHashCode(), info.LuaComponentId);

        bool found = false;
        for (int i = 0; i < s_CombinedKeys.Count; i++)
        {
            if (combinedKey != s_CombinedKeys[i])
                continue;

            CVRLuaClientBehaviour clientBehaviour = s_LuaComponentInstances[i];

            if (clientBehaviour.asset.m_ScriptPath == info.ScriptPath)
            {
                CVRLuaToolsExtensionMod.Logger.Msg("[LuaHotReloadManager] Script path match, updating script.");
                clientBehaviour.asset.name = info.ScriptName;
                clientBehaviour.asset.m_ScriptText = info.ScriptText;
                clientBehaviour.Restart();
                found = true;
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
                found = true;
            }
        }

        if (found)
        {
            CohtmlHud.Instance.ViewDropTextImmediate("(Local) CVRLuaTools", "Received script update", "Reloaded script: " + info.ScriptName);
        }
    }

    #endregion Game Events

    #region Private Methods
    
    private static int GenerateCombinedKey(int assetId, int instanceId)
    {
        return (assetId << 16) | instanceId; // yes
    }
    
    private static int GetGameObjectPathHashCode(Transform transform)
    {
        // both CVRAvatar & CVRSpawnable *should* have an asset info component
        Transform rootComponentTransform = null;
        CVRAssetInfo rootComponent = transform.GetComponentInParent<CVRAssetInfo>(true);
        if (rootComponent != null && rootComponent.type != CVRAssetInfo.AssetType.World) // ignore if under world instance
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