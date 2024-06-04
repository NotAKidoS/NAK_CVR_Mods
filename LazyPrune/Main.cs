using System.Reflection;
using ABI_RC.Core.IO;
using ABI_RC.Systems.GameEventSystem;
using ABI.CCK.Components;
using HarmonyLib;
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
        HarmonyInstance.Patch(
            typeof(CVRAvatar).GetMethod(nameof(CVRAvatar.Awake)), // earliest callback
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnObjectCreated),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch(
            typeof(CVRAvatar).GetMethod(nameof(CVRAvatar.OnDestroy)),
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnObjectDestroyed),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // listen for prop load/clear events
        HarmonyInstance.Patch(
            typeof(CVRSpawnable).GetMethod(nameof(CVRSpawnable.OnEnable)), // earliest callback
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnObjectCreated),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch(
            typeof(CVRSpawnable).GetMethod(nameof(CVRSpawnable.OnDestroy)),
            prefix: new HarmonyMethod(typeof(LazyPrune).GetMethod(nameof(OnObjectDestroyed),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

    }

    #region Game Events
    
    private static void OnWorldLoaded(string guid)
    {
        if (_lastLoadedWorld != guid)
            ForcePrunePendingObjects();
        
        _lastLoadedWorld = guid;
    }
    
    private static void OnObjectCreated(ref CVRObjectLoader.LoadedObject ___loadedObject)
    {
        if (___loadedObject == null)
            return; // uhh
        
        if (_loadedObjects.ContainsKey(___loadedObject))
        {
            _loadedObjects[___loadedObject] = -1; // mark as ineligible for pruning
            return; // already in cache
        }
        
        ___loadedObject.refCount++; // increment ref count
        _loadedObjects.Add(___loadedObject, -1); // mark as ineligible for pruning
    }
    
    private static void OnObjectDestroyed(ref CVRObjectLoader.LoadedObject ___loadedObject)
    {
        if (___loadedObject == null)
        {
            Logger.Error("Avatar/Prop destroyed with no backed LoadedObject. This is bad.");
            return; // handled by AttemptPruneObject
        }
        
        if (___loadedObject.refCount > 2)
            return; // we added our own ref, so begin death count at 2 (decrements one more after this callback)
        
        if (_loadedObjects.ContainsKey(___loadedObject))
            _loadedObjects[___loadedObject] = Time.time + OBJECT_CACHE_TIMEOUT * 60f;
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
            if (!(killTime < time) || killTime < 0) continue;
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