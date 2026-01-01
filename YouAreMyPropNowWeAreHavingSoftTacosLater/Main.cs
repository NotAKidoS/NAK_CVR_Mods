using System.Collections;
using System.Reflection;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using ABI_RC.Core.Util.Encryption;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.Movement;
using ABI.CCK.Components;
using DarkRift;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace NAK.YouAreMyPropNowWeAreHavingSoftTacosLater;

public class YouAreMyPropNowWeAreHavingSoftTacosLaterMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(YouAreMyPropNowWeAreHavingSoftTacosLater));

    private static readonly MelonPreferences_Entry<bool> EntryTrackPickups =
        Category.CreateEntry("track_pickups", true, display_name: "Track Pickups", description: "Should pickups be tracked?");
    
    private static readonly MelonPreferences_Entry<bool> EntryTrackAttachments =
        Category.CreateEntry("track_attachments", true, display_name: "Track Attachments", description: "Should attachments be tracked?");

    private static readonly MelonPreferences_Entry<bool> EntryTrackSeats =
        Category.CreateEntry("track_seats", true, display_name: "Track Seats", description: "Should seats be tracked?");
    
    private static readonly MelonPreferences_Entry<bool> EntryOnlySpawnedByMe =
        Category.CreateEntry("only_spawned_by_me", true, display_name: "Only Spawned By Me", description: "Should only props spawned by me be tracked?");

    #endregion Melon Preferences
    
    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        #region CVRPickupObject Patches

        HarmonyInstance.Patch(
            typeof(CVRPickupObject).GetMethod(nameof(CVRPickupObject.OnGrab),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnCVRPickupObjectOnGrab),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(CVRPickupObject).GetMethod(nameof(CVRPickupObject.OnDrop), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnCVRPickupObjectOnDrop),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        #endregion CVRPickupObject Patches
        
        #region CVRAttachment Patches
        
        HarmonyInstance.Patch( // Cannot compile when using nameof
            typeof(CVRAttachment).GetMethod(nameof(CVRAttachment.DoAttachmentSetup), 
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnCVRAttachmentAttachInternal),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(CVRAttachment).GetMethod(nameof(CVRAttachment.DeAttach)),
            prefix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnCVRAttachmentDeAttach),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        #endregion CVRAttachment Patches

        #region CVRSeat Patches
        
        HarmonyInstance.Patch(
            typeof(CVRSeat).GetMethod(nameof(CVRSeat.SitDown), 
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnCVRSeatSitDown),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(CVRSeat).GetMethod(nameof(CVRSeat.ExitSeat), 
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnCVRSeatExitSeat),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        #endregion CVRSeat Patches
        
        #region CVRSpawnable Patches
        
        HarmonyInstance.Patch(
            typeof(CVRSpawnable).GetMethod(nameof(CVRSpawnable.OnDestroy), 
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnSpawnableOnDestroy),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        #endregion CVRSpawnable Patches
        
        #region CVRSyncHelper Patches
        
        HarmonyInstance.Patch( // Replaces method, original needlessly ToArray???
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.ClearProps),
                BindingFlags.Public | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnCVRSyncHelperClearProps),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        #endregion CVRSyncHelper Patches
 
        #region CVRDownloadManager Patches

        HarmonyInstance.Patch(
            typeof(CVRDownloadManager).GetMethod(nameof(CVRDownloadManager.QueueTask),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnCVRDownloadManagerQueueTask),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        #endregion CVRDownloadManager Patches

        #region BetterBetterCharacterController Patches

        HarmonyInstance.Patch(
            typeof(BetterBetterCharacterController).GetMethod(nameof(BetterBetterCharacterController.SetSitting),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnBetterBetterCharacterControllerSetSitting),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        #endregion BetterBetterCharacterController Patches
        
        #region CVRWorld Game Events
        
        CVRGameEventSystem.World.OnLoad.AddListener(OnWorldLoad);
        CVRGameEventSystem.World.OnUnload.AddListener(OnWorldUnload);
        
        #endregion CVRWorld Game Events

        #region Instances Game Events

        CVRGameEventSystem.Instance.OnConnected.AddListener(OnInstanceConnected);

        #endregion Instances Game Events
    }
    
    #endregion Melon Events

    #region Harmony Patches
    
    [Flags] private enum HeldPropState { None = 0, Pickup = 1, Attachment = 2, Seat = 3 }
    
    private static readonly Dictionary<CVRSyncHelper.PropData, HeldPropState> _heldPropStates = new();
    
    private static void AddHeldPropState(CVRSyncHelper.PropData propData, HeldPropState state)
    {
        if (!_heldPropStates.TryAdd(propData, state)) _heldPropStates[propData] |= state;
    }
    
    private static void RemoveHeldPropState(CVRSyncHelper.PropData propData, HeldPropState state)
    {
        if (!_heldPropStates.TryGetValue(propData, out HeldPropState currentState)) return;
        currentState &= ~state;
        if (currentState == HeldPropState.None) _heldPropStates.Remove(propData);
        else _heldPropStates[propData] = currentState;
    }
    
    private static GameObject _persistantPropsContainer;
    private static GameObject GetOrCreatePropsContainer()
    {
        if (_persistantPropsContainer) return _persistantPropsContainer;
        _persistantPropsContainer = new("YouAreMyPropNowWeAreHavingSoftTacosLater");
        _persistantPropsContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        _persistantPropsContainer.transform.localScale = Vector3.one;
        Object.DontDestroyOnLoad(_persistantPropsContainer);
        return _persistantPropsContainer;
    }
    
    private static readonly Dictionary<Vector3, CVRSyncHelper.PropData> _keyToPropData = new();
    private static readonly Stack<SpawnablePositionContainer> _spawnablePositionStack = new();
    private static bool _ignoreNextSeatExit;
    private static float _heightOffset;

    private static void OnCVRPickupObjectOnGrab(CVRPickupObject __instance)
    {
        if (!EntryTrackPickups.Value) return;
        if (!TryGetPropData(__instance.GetComponentInParent<CVRSpawnable>(true), out CVRSyncHelper.PropData propData)) return;
        AddHeldPropState(propData, HeldPropState.Pickup);
    }
    
    private static void OnCVRPickupObjectOnDrop(CVRPickupObject __instance)
    {
        if (!TryGetPropData(__instance.GetComponentInParent<CVRSpawnable>(true), out CVRSyncHelper.PropData propData)) return;
        RemoveHeldPropState(propData, HeldPropState.Pickup);
    }
    
    private static void OnCVRAttachmentAttachInternal(CVRAttachment __instance)
    {
        if (!EntryTrackAttachments.Value) return;
        if (!TryGetPropData(__instance.GetComponentInParent<CVRSpawnable>(true), out CVRSyncHelper.PropData propData)) return;
        AddHeldPropState(propData, HeldPropState.Attachment);
    }
    
    private static void OnCVRAttachmentDeAttach(CVRAttachment __instance)
    {
        if (!__instance._isAttached) return; // Can invoke DeAttach without being attached
        if (!TryGetPropData(__instance.GetComponentInParent<CVRSpawnable>(true), out CVRSyncHelper.PropData propData)) return;
        RemoveHeldPropState(propData, HeldPropState.Attachment);
    }
    
    private static void OnCVRSeatSitDown(CVRSeat __instance)
    {
        if (!EntryTrackSeats.Value) return;
        if (!TryGetPropData(__instance.GetComponentInParent<CVRSpawnable>(true), out CVRSyncHelper.PropData propData)) return;
        AddHeldPropState(propData, HeldPropState.Seat);
    }
    
    private static void OnCVRSeatExitSeat(CVRSeat __instance)
    {
        if (!TryGetPropData(__instance.GetComponentInParent<CVRSpawnable>(true), out CVRSyncHelper.PropData propData)) return;
        RemoveHeldPropState(propData, HeldPropState.Seat);
    }
    
    private static void OnSpawnableOnDestroy(CVRSpawnable __instance)
    {
        if (!TryGetPropData(__instance, out CVRSyncHelper.PropData propData)) return;
        _heldPropStates.Remove(propData);
    }

    // ReSharper disable UnusedParameter.Local
    private static bool OnCVRDownloadManagerQueueTask(
        AssetManagement.UgcMetadata metadata,
        DownloadTask.ObjectType type,
        string assetUrl,
        string fileId,
        string toAttach,
        CVRLoadingAvatarController loadingAvatarController = null,
        bool joinOnComplete = false,
        bool isHomeRequested = false,
        string spawnerId = null)
    {
        if (type != DownloadTask.ObjectType.Prop) return true; // Only care about props
     
        // toAttach is our instanceId, lets find the propData
        if (!TryGetPropDataById(toAttach, out CVRSyncHelper.PropData newPropData)) return true;
        
        // Check if this is a prop we requested to spawn
        Vector3 identity = GetIdentityKeyFromPropData(newPropData);
        if (!_keyToPropData.Remove(identity, out CVRSyncHelper.PropData originalPropData)) return true;
        
        // Remove original prop data from held, cache states
        HeldPropState heldState = HeldPropState.None;
        if (_heldPropStates.ContainsKey(originalPropData))
        {
            heldState = _heldPropStates[originalPropData];
            _heldPropStates.Remove(originalPropData);
        }
        
        // If original prop data is null spawn a new prop i guess :(
        if (!originalPropData.Spawnable) return true;
        
        // Add the new prop data to our held props in place of the old one
        // if (!_heldPropData.Contains(newPropData)) _heldPropData.Add(newPropData);
        _heldPropStates.TryAdd(newPropData, heldState);
        
        // Apply new prop data to the spawnable
        newPropData.Spawnable = originalPropData.Spawnable;
        newPropData.Wrapper = originalPropData.Wrapper;
        newPropData.Wrapper.BroadcastMessage("OnHavingSoftTacosNow", SendMessageOptions.DontRequireReceiver); // support with RelativeSyncJitterFix
        newPropData.Wrapper.name = $"p+{newPropData.ObjectId}~{newPropData.InstanceId}";
        
        // Copy sync values
        Array.Copy(newPropData.CustomFloats, originalPropData.CustomFloats, newPropData.CustomFloatsAmount);
        
        CVRSyncHelper.ApplyPropValuesSpawn(newPropData);
        
        // Place the prop in the additive content scene
        PlacePropInAdditiveContentScene(newPropData.Spawnable);

        // Clear old data so Recycle() doesn't delete our prop
        originalPropData.Spawnable = null;
        originalPropData.Wrapper = null;
        originalPropData.Recycle();
        
        return false;
    }
    
    private static bool OnCVRSyncHelperClearProps() // Prevent deleting of our held props on scene load
    {
        for (var index = CVRSyncHelper.Props.Count - 1; index >= 0; index--)
        {
            CVRSyncHelper.PropData prop = CVRSyncHelper.Props[index];
            if (prop.Spawnable && _heldPropStates.ContainsKey(prop))
                continue; // Do not recycle props that are valid & held

            DeleteOrRecycleProp(prop);
        }

        CVRSyncHelper.MySpawnedPropInstanceIds.Clear();
        return false;
    }

    private static bool OnBetterBetterCharacterControllerSetSitting(bool isSitting, CVRSeat cvrSeat = null, bool callExitSeat = true)
    {
        if (!_ignoreNextSeatExit) return true;
        _ignoreNextSeatExit = false;
        if (!BetterBetterCharacterController.Instance._lastCvrSeat) return true; // run original
        return false; // dont run if there is a chair & we skipped it
    }

    #endregion Harmony Patches

    #region Game Events
    
    private object _worldLoadTimer;

    private void OnWorldLoad(string _)
    {
        CVRWorld worldInstance = CVRWorld.Instance;
        if (worldInstance && !worldInstance.allowSpawnables)
        {
            foreach (CVRSyncHelper.PropData prop in _heldPropStates.Keys) DeleteOrRecycleProp(prop); // Delete all props we kept
            return;
        }

        for (var index = _heldPropStates.Count - 1; index >= 0; index--)
        {
            CVRSyncHelper.PropData prop = _heldPropStates.ElementAt(index).Key;
            if (!prop.Spawnable)
            {
                DeleteOrRecycleProp(prop);
                return;
            }

            // apply positions
            int stackCount = _spawnablePositionStack.Count;
            for (int i = stackCount - 1; i >= 0; i--) _spawnablePositionStack.Pop().ReapplyOffsets();
        }
        
        // Start a timer, and anything that did not load within 3 seconds will be destroyed
        if (_worldLoadTimer != null)
        {
            MelonCoroutines.Stop(_worldLoadTimer);
            _worldLoadTimer = null;
        }
        _worldLoadTimer = MelonCoroutines.Start(DestroyPersistantPropContainerInFive());
        _ignoreNextSeatExit = true; // just in case we are in a car / vehicle
    }

    private IEnumerator DestroyPersistantPropContainerInFive()
    {
        yield return new WaitForSeconds(3f);
        _worldLoadTimer = null;
        Object.Destroy(_persistantPropsContainer);
        _persistantPropsContainer = null;
        _keyToPropData.Clear(); // no more chances
    }
    
    private static void OnWorldUnload(string _)
    {
        // Prevent deleting of our held props on scene destruction
        foreach (CVRSyncHelper.PropData prop in _heldPropStates.Keys)
        {
            if (!prop.Spawnable) continue;
            PlacePropInPersistantPropsContainer(prop.Spawnable);
            _spawnablePositionStack.Push(new SpawnablePositionContainer(prop.Spawnable));
        }

        // Likely in a vehicle
        _heightOffset = BetterBetterCharacterController.Instance._lastCvrSeat != null 
            ? GetHeightOffsetFromPlayer()
            : 0f;
    }

    private static void OnInstanceConnected(string _)
    {
        // Request the server to respawn our props by GUID, and add a secret key to the propData to identify it

        foreach (CVRSyncHelper.PropData prop in _heldPropStates.Keys)
        {
            if (!prop.Spawnable) continue;
            
            // Generate a new identity key for the prop (this is used to identify the prop when we respawn it)
            Vector3 identityKey = new(Random.Range(0, 1000), Random.Range(0, 1000), Random.Range(0, 1000));
            _keyToPropData.Add(identityKey, prop);
            
            SpawnPropFromGuid(prop.ObjectId,
                new Vector3(prop.PositionX, prop.PositionY, prop.PositionZ),
                new Vector3(prop.RotationX, prop.RotationY, prop.RotationZ),
                identityKey);
        }
    }

    #endregion Game Events
    
    #region Util

    private static bool TryGetPropData(CVRSpawnable spawnable, out CVRSyncHelper.PropData propData)
    {
        if (!spawnable)
        {
            propData = null;
            return false;
        }
        if (EntryOnlySpawnedByMe.Value && !spawnable.IsMine())
        {
            propData = null;
            return false;
        }
        foreach (CVRSyncHelper.PropData data in CVRSyncHelper.Props)
        {
            if (data.InstanceId != spawnable.instanceId) continue;
            propData = data;
            return true;
        }
        propData = null;
        return false;
    }
    
    private static bool TryGetPropDataById(string instanceId, out CVRSyncHelper.PropData propData)
    {
        foreach (CVRSyncHelper.PropData data in CVRSyncHelper.Props)
        {
            if (data.InstanceId != instanceId) continue;
            propData = data;
            return true;
        }
        propData = null;
        return false;
    }

    private static void PlacePropInAdditiveContentScene(CVRSpawnable spawnable)
    {
        spawnable.transform.parent.SetParent(null); // Unparent from the prop container
        SceneManager.MoveGameObjectToScene(spawnable.transform.parent.gameObject, 
            SceneManager.GetSceneByName(CVRObjectLoader.AdditiveContentSceneName));
    }
    
    private static void PlacePropInPersistantPropsContainer(CVRSpawnable spawnable)
    {
        spawnable.transform.parent.SetParent(GetOrCreatePropsContainer().transform);
    }
    
    private static void DeleteOrRecycleProp(CVRSyncHelper.PropData prop)
    {
        if (!prop.Spawnable) prop.Recycle();
        else prop.Spawnable.Delete();
        _heldPropStates.Remove(prop);
    }
    
    private static void SpawnPropFromGuid(string propGuid, Vector3 position, Vector3 rotation, Vector3 identityKey)
    {
        using DarkRiftWriter darkRiftWriter = DarkRiftWriter.Create();
        darkRiftWriter.Write(propGuid);
        darkRiftWriter.Write(position.x);
        darkRiftWriter.Write(position.y);
        darkRiftWriter.Write(position.z);
        darkRiftWriter.Write(rotation.x);
        darkRiftWriter.Write(rotation.y);
        darkRiftWriter.Write(rotation.z);
        darkRiftWriter.Write(identityKey.x); // for scale, but unused by CVR
        darkRiftWriter.Write(identityKey.y); // we will use this to identify our prop
        darkRiftWriter.Write(identityKey.z); // and recycle existing instance if it exists
        darkRiftWriter.Write(0f); // if not zero, prop spawn will be rejected by gs
        using Message message = Message.Create(10050, darkRiftWriter);
        NetworkManager.Instance.GameNetwork.SendMessage(message, SendMode.Reliable);
    }
    
    private static Vector3 GetIdentityKeyFromPropData(CVRSyncHelper.PropData propData)
        => new(propData.ScaleX, propData.ScaleY, propData.ScaleZ);
    
    private const int WORLD_RAYCAST_LAYER_MASK =
        (1 << 0) | // Default
        (1 << 16) | (1 << 17) | (1 << 18) | (1 << 19) |
        (1 << 20) | (1 << 21) | (1 << 22) | (1 << 23) |
        (1 << 24) | (1 << 25) | (1 << 26) | (1 << 27) |
        (1 << 28) | (1 << 29) | (1 << 30) | (1 << 31);

    private static float GetHeightOffsetFromPlayer()
    {
        Vector3 playerPos = PlayerSetup.Instance.GetPlayerPosition();
        Ray ray = new(playerPos, Vector3.down);

        // ReSharper disable once Unity.PreferNonAllocApi
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, WORLD_RAYCAST_LAYER_MASK, QueryTriggerInteraction.Ignore);
        Scene baseScene = SceneManager.GetActiveScene();

        float closestDist = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;
        bool foundValidHit = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.scene != baseScene) continue; // Ignore objects not in the world scene
            if (!(hit.distance < closestDist)) continue;
            closestDist = hit.distance;
            closestPoint = hit.point;
            foundValidHit = true;
        }

        if (!foundValidHit) return 0f; // TODO: idk if i should do this
        float offset = playerPos.y - closestPoint.y;
        return Mathf.Clamp(offset, 0f, 20f);
    }
    
    #endregion Util

    #region Helper Classes

    private readonly struct SpawnablePositionContainer
    {
        private readonly CVRSpawnable _spawnable;
        private readonly Vector3[] _posOffsets;
        private readonly Quaternion[] _rotOffsets;

        public SpawnablePositionContainer(CVRSpawnable spawnable)
        {
            _spawnable = spawnable;
            
            int syncedTransforms = 1 + _spawnable.subSyncs.Count; // root + subSyncs

            _posOffsets = new Vector3[syncedTransforms];
            _rotOffsets = new Quaternion[syncedTransforms];

            Transform playerTransform = PlayerSetup.Instance.transform;

            // Save root offset relative to player
            Transform _spawnableTransform = _spawnable.transform;
            _posOffsets[0] = playerTransform.InverseTransformPoint(_spawnableTransform.position);
            _rotOffsets[0] = Quaternion.Inverse(playerTransform.rotation) * _spawnableTransform.rotation;

            // Save subSync offsets relative to player
            for (int i = 0; i < _spawnable.subSyncs.Count; i++)
            {
                Transform subSyncTransform = _spawnable.subSyncs[i].transform;
                if (subSyncTransform == null) continue;
                _posOffsets[i + 1] = playerTransform.InverseTransformPoint(subSyncTransform.position);
                _rotOffsets[i + 1] = Quaternion.Inverse(playerTransform.rotation) * subSyncTransform.rotation;
            }
        }

        public void ReapplyOffsets()
        {
            Transform playerTransform = PlayerSetup.Instance.transform;

            // Reapply to root
            Vector3 rootWorldPos = playerTransform.TransformPoint(_posOffsets[0]);
            rootWorldPos.y += _heightOffset;
            _spawnable.transform.position = rootWorldPos;
            _spawnable.transform.rotation = playerTransform.rotation * _rotOffsets[0];

            // Reapply to subSyncs
            for (int i = 0; i < _spawnable.subSyncs.Count; i++)
            {
                Transform subSyncTransform = _spawnable.subSyncs[i].transform;
                if (!subSyncTransform) continue;

                Vector3 subWorldPos = playerTransform.TransformPoint(_posOffsets[i + 1]);
                subWorldPos.y += _heightOffset;
                subSyncTransform.position = subWorldPos;
                subSyncTransform.rotation = playerTransform.rotation * _rotOffsets[i + 1];
            }

            // hack
            _spawnable.needsUpdate = true;
            _spawnable.UpdateSubSyncValues();
            _spawnable.sendUpdate();
        }
    }

    #endregion Helper Classes
}