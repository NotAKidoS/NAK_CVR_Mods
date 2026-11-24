using System.Collections;
using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Player;
using ABI_RC.Core.PropManagement;
using ABI_RC.Systems.ContentClones;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.Movement;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using PropRelationshipState = ABI_RC.Systems.ContentClones.PropRelationshipTracker.PropRelationshipState;

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
        #region Props Patches

        HarmonyInstance.Patch( // Prevent deleting attached props on disconnect
            typeof(Props).GetMethod(nameof(Props.OnInstanceDisconnected),
                BindingFlags.NonPublic | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnPropsOnInstanceDisconnected),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        HarmonyInstance.Patch( // Prevent deleting attached props on scene load
            typeof(Props).GetMethod(nameof(Props.OnWorldUnload),
                BindingFlags.NonPublic | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(YouAreMyPropNowWeAreHavingSoftTacosLaterMod).GetMethod(nameof(OnPropsOnWorldUnload),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        #endregion Props Patches

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

        CVRGameEventSystem.World.OnReady.AddListener(OnWorldReady);
        CVRGameEventSystem.World.OnWorldUnload.AddListener(OnWorldUnload);

        #endregion CVRWorld Game Events

        #region Instances Game Events

        CVRGameEventSystem.Instance.OnConnected.AddListener(OnInstanceConnected);

        #endregion Instances Game Events
    }

    #endregion Melon Events

    #region Harmony Patches

    private const float HELD_PROP_TIMEOUT = 5f;

    private static readonly List<SpawnablePositionContainer> _heldProps = new();
    private static bool _placeHeldProps;
    private static bool _ignoreNextSeatExit;
    private static float _heightOffset;

    private static void OnPropsOnInstanceDisconnected() => KeepHeldProps(true); // Only server props are deleted

    private static void OnPropsOnWorldUnload() => KeepHeldProps(false);

    private static void KeepHeldProps(bool onlyServerBacked) // Detach attached props from souls
    {
        int heldCount = _heldProps.Count;

        foreach (PropSoul prop in Props.All)
        {
            if (onlyServerBacked && prop.Kind != PropKind.ServerBacked) continue;
            if (!prop.Spawnable || !prop.Wrapper) continue;
            if (!ShouldKeepProp(prop.Spawnable)) continue;

            _heldProps.Add(new SpawnablePositionContainer(prop));
            Object.DontDestroyOnLoad(prop.Wrapper);
            prop.Wrapper = null;
        }

        // Destroy props not respawned in time
        if (_heldProps.Count != heldCount && !CVRObjectLoader.Instance._isLoadingWorldToJoin)
            StartWorldLoadTimer();
    }

    private static bool OnCVRDownloadManagerQueueTask(DownloadTask.ObjectType type, string toAttach)
    {
        if (type != DownloadTask.ObjectType.Prop) return true; // Only care about props

        // toAttach is our instanceId, lets find the prop
        if (!Props.TryGet(toAttach, out PropSoul prop) || !prop.IsSpawnedByMe) return true;

        // Check if this is a prop we requested to spawn
        int index = _heldProps.FindIndex(container => container.ObjectId == prop.ObjectId);
        if (index < 0) return true;

        SpawnablePositionContainer container = _heldProps[index];
        _heldProps.RemoveAt(index);

        // If original prop is null spawn a new prop i guess :(
        if (!container.Spawnable) return true;

        // Original prop replaces download
        prop.Wrapper = container.Wrapper;
        prop.Wrapper.BroadcastMessage("OnHavingSoftTacosNow", SendMessageOptions.DontRequireReceiver); // support with RelativeSyncJitterFix
        prop.Wrapper.name = prop.InstanceId;

        // Place the prop in the additive content scene
        PlacePropInAdditiveContentScene(prop.Wrapper);

        container.Spawnable.SpawnedByMe = true;
        Props.OnInstanceAttached(prop.InstanceId, container.Spawnable);
        CVRGameEventSystem.Spawnable.OnPropSpawned.Invoke(prop.SpawnedBy, prop);

        // Content clone died with old soul
        if (PropRelationshipTracker._propRelationshipStates.TryGetValue(container.Spawnable, out PropRelationshipState state))
            PropRelationshipTracker.OnPropRelationshipStateChanged?.Invoke(container.Spawnable, state, state);

        return false;
    }

    private static bool OnBetterBetterCharacterControllerSetSitting()
    {
        if (!_ignoreNextSeatExit) return true;
        _ignoreNextSeatExit = false;
        if (!BetterBetterCharacterController.Instance._lastCvrSeat) return true; // run original
        return false; // dont run if there is a chair & we skipped it
    }

    #endregion Harmony Patches

    #region Game Events

    private static object _worldLoadTimer;

    private static void OnWorldReady(string _)
    {
        CVRWorld worldInstance = CVRWorld.Instance;
        if (worldInstance && !worldInstance.allowSpawnables)
        {
            DeleteHeldProps(); // Delete all props we kept
            return;
        }

        PlaceHeldProps();

        // Destroy props not respawned in time
        StartWorldLoadTimer();
    }

    private static void StartWorldLoadTimer()
    {
        StopWorldLoadTimer();
        _worldLoadTimer = MelonCoroutines.Start(DestroyHeldPropsInFive());
    }

    private static void StopWorldLoadTimer()
    {
        if (_worldLoadTimer == null) return;
        MelonCoroutines.Stop(_worldLoadTimer);
        _worldLoadTimer = null;
    }

    private static IEnumerator DestroyHeldPropsInFive()
    {
        yield return new WaitForSeconds(HELD_PROP_TIMEOUT);
        _worldLoadTimer = null;
        DeleteHeldProps(); // no more chances
    }

    private static void OnWorldUnload(string _, CVRObjectLoader.WorldLoadIntent __)
    {
        StopWorldLoadTimer();

        // Determine which props to keep
        for (int i = _heldProps.Count - 1; i >= 0; i--)
        {
            SpawnablePositionContainer container = _heldProps[i];
            if (container.Spawnable && ShouldKeepProp(container.Spawnable))
            {
                container.SaveOffsets();
                continue;
            }

            DeleteOrRecycleProp(container);
            _heldProps.RemoveAt(i);
        }

        _placeHeldProps = true;

        // Likely in a vehicle
        CVRSeat seat = BetterBetterCharacterController.Instance._lastCvrSeat;
        _heightOffset = seat != null
            ? GetHeightOffsetFromPlayer()
            : 0f;

        // World load kicks us out of seat
        CVRSpawnable seatSpawnable = seat != null ? seat.GetComponentInParent<CVRSpawnable>(true) : null;
        _ignoreNextSeatExit = seatSpawnable && ShouldKeepProp(seatSpawnable);
    }

    private static void OnInstanceConnected(string _)
    {
        // Can connect before world ready event
        PlaceHeldProps();

        // Respawn props by GUID, original prop replaces download
        for (int i = _heldProps.Count - 1; i >= 0; i--)
        {
            SpawnablePositionContainer container = _heldProps[i];
            if (container.Spawnable && PropUserActions.CanSpawn(container.ObjectId, out string _))
            {
                Transform spawnableTransform = container.Spawnable.transform;
                if (PropNetwork.RequestSpawn(container.ObjectId,
                        spawnableTransform.position - new Vector3(0f, container.Spawnable.spawnHeight, 0f),
                        spawnableTransform.rotation))
                    continue;
            }

            DeleteOrRecycleProp(container);
            _heldProps.RemoveAt(i);
        }
    }

    #endregion Game Events

    #region Util

    private static bool ShouldKeepProp(CVRSpawnable spawnable)
    {
        PropSoul prop = spawnable.PropSoul;
        if (prop is not { Kind: PropKind.ServerBacked or PropKind.LocalOnly }) return false;
        if (EntryOnlySpawnedByMe.Value && !prop.IsSpawnedByMe) return false;

        if (!PropRelationshipTracker._propRelationshipStates.TryGetValue(spawnable, out PropRelationshipState state)) return false;
        if (!EntryTrackPickups.Value) state &= ~PropRelationshipState.Pickup;
        if (!EntryTrackAttachments.Value) state &= ~PropRelationshipState.Attachment;
        if (!EntryTrackSeats.Value) state &= ~PropRelationshipState.Seat;
        return state != PropRelationshipState.None;
    }

    private static void PlaceHeldProps()
    {
        if (!_placeHeldProps) return;
        _placeHeldProps = false;

        // apply positions
        foreach (SpawnablePositionContainer container in _heldProps)
            if (container.Spawnable) container.ReapplyOffsets();
    }

    private static void PlacePropInAdditiveContentScene(GameObject wrapper)
    {
        Scene scene = SceneManager.GetSceneByName(CVRObjectLoader.AdditiveContentSceneName);
        if (scene.isLoaded) SceneManager.MoveGameObjectToScene(wrapper, scene);
    }

    private static void DeleteOrRecycleProp(SpawnablePositionContainer container)
    {
        if (container.Wrapper) Object.Destroy(container.Wrapper);
    }

    private static void DeleteHeldProps()
    {
        foreach (SpawnablePositionContainer container in _heldProps) DeleteOrRecycleProp(container);
        _heldProps.Clear();
    }

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

    private class SpawnablePositionContainer
    {
        public readonly string ObjectId;
        public readonly CVRSpawnable Spawnable;
        public readonly GameObject Wrapper;

        private readonly Vector3[] _posOffsets;
        private readonly Quaternion[] _rotOffsets;

        public SpawnablePositionContainer(PropSoul prop)
        {
            ObjectId = prop.ObjectId;
            Spawnable = prop.Spawnable;
            Wrapper = prop.Wrapper;

            int syncedTransforms = 1 + Spawnable.subSyncs.Count; // root + subSyncs

            _posOffsets = new Vector3[syncedTransforms];
            _rotOffsets = new Quaternion[syncedTransforms];

            SaveOffsets();
        }

        public void SaveOffsets()
        {
            Transform playerTransform = PlayerSetup.Instance.transform;

            // Save root offset relative to player
            Transform spawnableTransform = Spawnable.transform;
            _posOffsets[0] = playerTransform.InverseTransformPoint(spawnableTransform.position);
            _rotOffsets[0] = Quaternion.Inverse(playerTransform.rotation) * spawnableTransform.rotation;

            // Save subSync offsets relative to player
            for (int i = 0; i < Spawnable.subSyncs.Count; i++)
            {
                Transform subSyncTransform = Spawnable.subSyncs[i].transform;
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
            Spawnable.transform.position = rootWorldPos;
            Spawnable.transform.rotation = playerTransform.rotation * _rotOffsets[0];

            // Reapply to subSyncs
            for (int i = 0; i < Spawnable.subSyncs.Count; i++)
            {
                Transform subSyncTransform = Spawnable.subSyncs[i].transform;
                if (!subSyncTransform) continue;

                Vector3 subWorldPos = playerTransform.TransformPoint(_posOffsets[i + 1]);
                subWorldPos.y += _heightOffset;
                subSyncTransform.position = subWorldPos;
                subSyncTransform.rotation = playerTransform.rotation * _rotOffsets[i + 1];
            }
        }
    }

    #endregion Helper Classes
}