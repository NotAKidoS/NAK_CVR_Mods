using System.Reflection;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.GameEventSystem;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.LazyPrune;

public class LazyPrune : MelonMod
{
    private static MelonLogger.Instance Logger;

    private const int MAX_OBJECTS_UNLOADED_AT_ONCE = 3; // just to alleviate hitch on mass destruction
    private const float OBJECT_CACHE_TIMEOUT = 2f; // minutes
    private static readonly Dictionary<CVRObjectLoader.LoadedObject, float> _loadedObjects = new();

    private static string _lastLoadedWorld;
    private static bool _isInitialized;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        // listen for world load
        CVRGameEventSystem.World.OnLoad.AddListener(OnWorldLoaded);
        
        // listen for local avatar bundle load
        HarmonyInstance.Patch(
            typeof(CVRObjectLoader).GetMethod(nameof(CVRObjectLoader.InstantiateAvatarFromExistingPrefab),
                BindingFlags.Public | BindingFlags.Instance), // earliest callback (why the fuck are you public)
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnInstantiateAvatarFromExistingPrefab),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // listen for prop bundle load
        HarmonyInstance.Patch(
            typeof(CVRObjectLoader).GetMethod(nameof(CVRObjectLoader.InstantiateSpawnableFromExistingPrefab),
                BindingFlags.NonPublic | BindingFlags.Instance), // earliest callback
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnInstantiateSpawnableFromExistingPrefab),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        // listen for object destruction
        HarmonyInstance.Patch(
            typeof(CVRObjectLoader).GetMethod(nameof(CVRObjectLoader.CheckForDestruction),
                BindingFlags.Public | BindingFlags.Instance), // earliest callback
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnObjectDestroyed),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // nuke qm debug method as there is a race condition that locks up download queue 
        HarmonyInstance.Patch(
            typeof(CVRDownloadManager).GetMethod(nameof(CVRDownloadManager.UpdateUiDownloadStatus),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnUpdateUiDownloadStatus),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // another race condition that causes instantiation queue to lock up
        HarmonyInstance.Patch(
            typeof(PuppetMaster).GetMethod(nameof(PuppetMaster.AvatarInstantiated),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnAvatarInstantiated),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    #region Game Fixes
    
    private static bool OnUpdateUiDownloadStatus() => false;

    private static bool OnAvatarInstantiated(ref GameObject ___avatarObject)
    {
        if (___avatarObject != null) return true; // instantiate coroutine is try-catch'd, so purposefully throw to have it catch
        Logger.Warning("Caught null avatar object in PuppetMaster.AvatarInstantiated. Throwing exception so ObjectLoader can catch.");
        throw new Exception("LazyPrune: PuppetMaster Avatar instantiated with null object!");
    }

    #endregion Game  Fixes
    
    #region Game Events
    
    private static void OnInstantiateAvatarFromExistingPrefab(string objectId, string instantiationTarget,
        GameObject prefabObject,
        ref CVRObjectLoader.LoadedObject loadedObject, string blockReason, AssetManagement.AvatarTags? avatarTags,
        bool shouldFilter, bool isBlocked,
        CompatibilityVersions compatibilityVersion)
    {
        if (prefabObject == MetaPort.Instance.blockedAvatarPrefab)
        {
            Logger.Msg($"Blocked avatar prefab is loading. Ignoring...");
            return;
        }
        
        OnObjectCreated(ref loadedObject);
    }

    private static void OnInstantiateSpawnableFromExistingPrefab(string objectId, string instantiationTarget,
        GameObject prefabObject, CVRObjectLoader.LoadedObject loadedObject, AssetManagement.PropTags propTags,
        CompatibilityVersions compatibilityVersion)
    {
        if (prefabObject == MetaPort.Instance.blockedSpawnablePrefab)
        {
            Logger.Msg($"Blocked spawnable prefab is loading. Ignoring...");
            return;
        }
        
        OnObjectCreated(ref loadedObject);
    }

    private static void OnWorldLoaded(string guid)
    {
        if (!_isInitialized)
        {
            // every minute, check for objects to prune
            SchedulerSystem.AddJob(CheckForObjectsToPrune, 1f, 10f, -1);
            _isInitialized = true;
        }
        
        if (_lastLoadedWorld != guid)
            ForcePrunePendingObjects();
        
        _lastLoadedWorld = guid;
    }
    
    private static void OnObjectCreated(ref CVRObjectLoader.LoadedObject ___loadedObject)
    {
        if (___loadedObject == null)
        {
            Logger.Error("Avatar/Prop created with no backed LoadedObject.");
            return;
        }
        
        if (_loadedObjects.ContainsKey(___loadedObject))
        {
            _loadedObjects[___loadedObject] = -1; // mark as ineligible for pruning
            return;
        }
        
        ___loadedObject.refCount++; // increment ref count
        _loadedObjects.Add(___loadedObject, -1); // mark as ineligible for pruning
    }
    
    private static void OnObjectDestroyed(CVRObjectLoader.LoadedObject loadedObject)
    {
        if (loadedObject == null)
        {
            Logger.Error("Avatar/Prop destroyed with no backed LoadedObject.");
            return;
        }
        
        if (loadedObject.refCount > 1)
            return; 
        
        if (_loadedObjects.ContainsKey(loadedObject))
            _loadedObjects[loadedObject] = Time.time + OBJECT_CACHE_TIMEOUT * 60f;
    }
    
    #endregion Game Events

    #region Lazy Pruning
    
    private static void ForcePrunePendingObjects()
    {
        for (int i = _loadedObjects.Count - 1; i >= 0; i--)
        {
            (CVRObjectLoader.LoadedObject loadedObject, var killTime) = _loadedObjects.ElementAt(i);
            if (killTime > 0) AttemptPruneObject(loadedObject);
        }
    }
    
    private static void CheckForObjectsToPrune()
    {
        int unloaded = 0;
        float time = Time.time;
        for (int i = _loadedObjects.Count - 1; i >= 0; i--)
        {
            (CVRObjectLoader.LoadedObject loadedObject, var killTime) = _loadedObjects.ElementAt(i);
            if (!(killTime < time) || killTime < 0) continue;
            AttemptPruneObject(loadedObject);
            if (unloaded++ >= MAX_OBJECTS_UNLOADED_AT_ONCE) break;
        }
    }
    
    private static void AttemptPruneObject(CVRObjectLoader.LoadedObject loadedObject)
    {
        if (loadedObject == null)
        {
            Logger.Error("Attempted to prune null object. This happens on initial load sometimes.");
            return;
        }
        
        if (loadedObject.refCount > 1)
        {
            Logger.Error($"Object {loadedObject.prefabName} has ref count {loadedObject.refCount}, expected 1");
            _loadedObjects[loadedObject] = -1;
            return;
        }
        
        Logger.Msg($"Pruning object {loadedObject.prefabName}");
        _loadedObjects.Remove(loadedObject);
        
        loadedObject.refCount--;
        if (CVRObjectLoader.Instance != null)
            CVRObjectLoader.Instance.CheckForDestruction(loadedObject);
    }
    
    #endregion Lazy Pruning
}