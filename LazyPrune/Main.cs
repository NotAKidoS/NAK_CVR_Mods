using ABI_RC.Core.IO;
using ABI_RC.Systems.GameEventSystem;
using MelonLoader;
using UnityEngine;

namespace NAK.LazyPrune;

public class LazyPrune : MelonMod
{
    internal static MelonLogger.Instance Logger;

    //private const int MAX_OBJECTS_UNLOADED_AT_ONCE = 5; // just to alleviate hitch on mass destruction
    private const float OBJECT_CACHE_TIMEOUT = 3f; // minutes
    private static readonly Dictionary<CVRObjectLoader.LoadedObject, float> _loadedObjects = new();

    private static string _lastLoadedWorld;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        // every minute, check for objects to prune
        SchedulerSystem.AddJob(CheckForObjectsToPrune, 1f, 60f, -1);
        
        // listen for world load
        CVRGameEventSystem.World.OnLoad.AddListener(OnWorldLoaded);
        
        // listen for local avatar load/clear events
        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener((avatar) => OnObjectCreated(avatar.loadedObject));
        CVRGameEventSystem.Avatar.OnLocalAvatarClear.AddListener((avatar) => OnObjectDestroyed(avatar ? avatar.loadedObject : null));
        
        // listen for remote avatar load/clear events
        CVRGameEventSystem.Avatar.OnRemoteAvatarLoad.AddListener((_, avatar) => OnObjectCreated(avatar.loadedObject));
        CVRGameEventSystem.Avatar.OnRemoteAvatarClear.AddListener((_, avatar) => OnObjectDestroyed(avatar != null ? avatar.loadedObject : null));
        
        // listen for spawnable instantiate/destroy events
        CVRGameEventSystem.Spawnable.OnInstantiate.AddListener((_, spawnable) => OnObjectCreated(spawnable.loadedObject));
        CVRGameEventSystem.Spawnable.OnDestroy.AddListener((_, spawnable) => OnObjectDestroyed(spawnable != null ? spawnable.loadedObject : null));
    }

    #region Game Events
    
    private static void OnWorldLoaded(string guid)
    {
        if (_lastLoadedWorld != guid)
            ForcePrunePendingObjects();
        
        _lastLoadedWorld = guid;
    }
    
    private static void OnObjectCreated(CVRObjectLoader.LoadedObject loadedObject)
    {
        if (loadedObject == null)
            return; // uhh
        
        if (_loadedObjects.ContainsKey(loadedObject))
        {
            _loadedObjects[loadedObject] = -1; // mark as ineligible for pruning
            return; // already in cache
        }
        
        loadedObject.refCount++; // increment ref count
        _loadedObjects.Add(loadedObject, -1); // mark as ineligible for pruning
    }
    
    private static void OnObjectDestroyed(CVRObjectLoader.LoadedObject loadedObject)
    {
        if (loadedObject == null)
            return; // handled by AttemptPruneObject
        
        if (loadedObject.refCount > 2) 
            return; // we added our own ref, so begin death count at 2 (decrements one more after this callback)
        
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
            if (killTime > 0) AttemptPruneObject(loadedObject); // prune all pending objects
        }
    }
    
    private static void CheckForObjectsToPrune()
    {
        //int unloaded = 0;
        float time = Time.time;
        for (int i = _loadedObjects.Count - 1; i >= 0; i--)
        {
            (CVRObjectLoader.LoadedObject loadedObject, var killTime) = _loadedObjects.ElementAt(i);
            if (!(killTime < time)) continue;
            AttemptPruneObject(loadedObject); // prune expired objects
            //if (unloaded++ >= MAX_OBJECTS_UNLOADED_AT_ONCE) break; // limit unloads per check
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
            _loadedObjects[loadedObject] = -1; // mark as ineligible for pruning ???
            return; // is something somehow holding a reference?
        }
        
        Logger.Msg($"Pruning object {loadedObject.prefabName}");
        _loadedObjects.Remove(loadedObject); // remove from cache
        
        loadedObject.refCount--; // decrement ref count
        if (CVRObjectLoader.Instance != null) // provoke destruction
            CVRObjectLoader.Instance.CheckForDestruction(loadedObject);
    }
    
    #endregion Lazy Pruning
}