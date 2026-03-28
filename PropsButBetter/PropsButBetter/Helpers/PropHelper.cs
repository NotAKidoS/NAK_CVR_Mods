using System.Collections;
using ABI_RC.Core;
using ABI_RC.Core.AudioEffects;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.GameServer;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI_RC.Core.Util.AssetFiltering;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.Gravity;
using ABI.CCK.Components;
using DarkRift;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.PropsButBetter;

public static class PropHelper
{
    public const int TotalKeptSessionHistory = 30;
    
    public static List<PropHistoryData> SpawnedThisSession { get; } = new(TotalKeptSessionHistory);

    // CVRSyncHelper does not keep track of your own props nicely
    public static readonly List<CVRSyncHelper.PropData> MyProps = [];

    // We get prop spawn/destroy events with emptied prop data sometimes, so we need to track shit ourselves :D
    private static readonly Dictionary<CVRSyncHelper.PropData, string> _propToInstanceId = [];
    
    private static int MaxPropsForUser = 20;
    
    private const int RedoTimeoutLimit = 120; // seconds
    private static readonly int RedoHistoryLimit = Mathf.Max(MaxPropsForUser, CVRSyncHelper.MyPropCount);

    private static readonly List<DeletedPropData> _myDeletedProps = [];
    
    // audio clip names, InterfaceAudio adds "PropsButBetter" prefix
    public const string SFX_Spawn = $"{nameof(PropsButBetter)}_sfx_spawn";
    public const string SFX_Undo = $"{nameof(PropsButBetter)}_sfx_undo";
    public const string SFX_Redo = $"{nameof(PropsButBetter)}_sfx_redo";
    public const string SFX_Warn = $"{nameof(PropsButBetter)}_sfx_warn";
    public const string SFX_Deny = $"{nameof(PropsButBetter)}_sfx_deny";

    public static void PlaySound(string sound)
    {
        if (ModSettings.EntryUseSFX.Value)
            InterfaceAudio.PlayModule(sound);
    }
    
    public static bool IsPropSpawnAllowed() 
        => MetaPort.Instance.worldAllowProps
           && MetaPort.Instance.settings.GetSettingsBool("ContentFilterPropsEnabled")
           && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;

    public static bool IsAtPropLimit()
        => MyProps.Count >= MaxPropsForUser;

    public static bool CanUndo() 
        => MyProps.Count > 0;

    public static bool CanRedo()
    {
        int deletedPropsCount = _myDeletedProps.Count;
        return deletedPropsCount > 0 
            && _myDeletedProps[deletedPropsCount-1].IsWithinTimeLimit
            && !IsAtPropLimit() && IsPropSpawnAllowed();
    }

    public static void UndoProp()
    {
        CVRSyncHelper.PropData latestPropData = MyProps.LastOrDefault();
        if (latestPropData == null)
        {
            PlaySound(SFX_Warn);
            return;
        }
        latestPropData.RecycleSafe();
    }

    public static void RedoProp()
    {
        int index = _myDeletedProps.Count - 1;
        if (index < 0)
        {
            PlaySound(SFX_Warn);
            return;
        }

        if (!IsPropSpawnAllowed() || IsAtPropLimit())
        {
            PlaySound(SFX_Deny);
            return;
        }

        // Only allow redoing props if they were deleted within the timeout
        DeletedPropData deletedProp = _myDeletedProps[index];
        if (deletedProp.IsWithinTimeLimit)
        {
            _skipBecauseIsRedo = true;
            CVRSyncHelper.SpawnProp(deletedProp.PropId, deletedProp.Position, deletedProp.Rotation);
            _skipBecauseIsRedo = false;
            _myDeletedProps.RemoveAt(index);
            PlaySound(SFX_Redo);
        }
        else
        {
            // If latest prop is too old, same with rest
            ClearUndo();
            PlaySound(SFX_Warn);
        }
    }

    public static void ClearUndo()
    {
        _myDeletedProps.Clear();
    }
    
    public static bool RemoveMyProps()
    {
        int propsCount = MyProps.Count;
        if (propsCount == 0)
        {
            PlaySound(SFX_Warn);
            return false;
        }
        
        for (int i = propsCount - 1; i >= 0; i--)
        {
            CVRSyncHelper.PropData prop = MyProps[i];
            prop.RecycleSafe();
        }

        return true;
    }

    public static bool RemoveOthersProps()
    {
        int propsCount = CVRSyncHelper.Props.Count;
        if (propsCount == 0)
        {
            PlaySound(SFX_Warn);
            return false;
        }
        
        for (int i = propsCount - 1; i >= 0; i--)
        {
            CVRSyncHelper.PropData prop = CVRSyncHelper.Props[i];
            if (!prop.IsSpawnedByMe()) prop.RecycleSafe();
        }

        return true;
    }

    public static bool RemoveAllProps()
    {
        int propsCount = CVRSyncHelper.Props.Count;
        if (propsCount == 0)
        {
            PlaySound(SFX_Warn);
            return false;
        }
        
        for (int i = propsCount - 1; i >= 0; i--)
        {
            CVRSyncHelper.PropData prop = CVRSyncHelper.Props[i];
            prop.RecycleSafe();
        }

        return true;
    }
    
    private static void OnPropSpawned(string assetId, CVRSyncHelper.PropData prop)
    {
        // Skip broken props
        string instanceId = prop.InstanceId;
        if (string.IsNullOrEmpty(instanceId)) 
            return;
        
        // Ignore the visualizer prop
        if (instanceId == _lastVisualizerInstanceId)
        {
            OnVisualizerPropSpawned();
            return;
        }
        
        // PropDistanceHider.OnPropSpawned(prop);
        
        // Track ourselves for later
        _propToInstanceId.TryAdd(prop, prop.InstanceId);
        
        // Track our props
        if (prop.IsSpawnedByMe()) MyProps.Add(prop);
        
        // Check if this prop was reloaded
        int spawnedThisSessionCount = SpawnedThisSession.Count;
        for (int i = 0; i < spawnedThisSessionCount; i++)
        {
            if (SpawnedThisSession[i].InstanceId == instanceId)
            {
                SpawnedThisSession[i].IsDestroyed = false;
                return; // No need to add
            }
        }
        
        // Track for session history
        PropHistoryData historyData = new()
        {
            PropId = prop.ObjectId,
            PropName = prop.ContentMetadata.AssetName,
            SpawnerName = CVRPlayerManager.Instance.TryGetPlayerName(prop.SpawnedBy),
            InstanceId = instanceId,
            IsDestroyed = false
        };
        
        // Insert at beginning for newest first
        SpawnedThisSession.Insert(0, historyData);
        
        // Keep only the most recent entries
        if (spawnedThisSessionCount >= TotalKeptSessionHistory)
            SpawnedThisSession.RemoveAt(spawnedThisSessionCount - 1);
    }

    private static void OnPropDestroyed(string assetId, CVRSyncHelper.PropData prop)
    {
        // Only handle props which we tracked the spawn for
        if (!_propToInstanceId.Remove(prop, out string instanceId))
            return;
        
        // PropDistanceHider.OnPropDestroyed(prop);
        
        // Track our props
        if (MyProps.Remove(prop))
        {
            // Track the deleted prop for undo
            if (_myDeletedProps.Count >= RedoHistoryLimit)
                _myDeletedProps.RemoveAt(0);

            DeletedPropData deletedProp = new(prop);
            _myDeletedProps.Add(deletedProp);
        
            PlaySound(SFX_Undo);
        }
        
        // Track for session history
        int spawnedThisSessionCount = SpawnedThisSession.Count;
        for (int i = 0; i < spawnedThisSessionCount; i++)
        {
            if (SpawnedThisSession[i].InstanceId == instanceId)
            {
                SpawnedThisSession[i].IsDestroyed = true;
                break;
            }
        }
    }

    private static void OnGSInfoUpdate(GSInfoUpdate update, GSInfoChanged changed)
    {
        if (changed == GSInfoChanged.MaxPropsPerUser)
            MaxPropsForUser = update.MaxPropsPerUser;
    }

    private static void OnWorldUnload(string _) => ClearUndo();
    
    public static void Initialize()
    {
        CVRGameEventSystem.Spawnable.OnPropSpawned.AddListener(OnPropSpawned);
        CVRGameEventSystem.Spawnable.OnPropDestroyed.AddListener(OnPropDestroyed);
        GSInfoHandler.OnGSInfoUpdate += OnGSInfoUpdate;
        CVRGameEventSystem.World.OnUnload.AddListener(OnWorldUnload);
    }
    
    // Replacement methods for CVRSyncHelper

    public static bool OnPreDeleteMyProps()
    {
        bool removedProps = RemoveMyProps();
        
        if (removedProps)
            ViewManager.Instance.NotifyUser("(Synced) Client", "Removed all my props", 1f);
        else
            ViewManager.Instance.NotifyUser("(Local) Client", "No props to remove", 1f);
        
        return false;
    }

    public static bool OnPreDeleteAllProps()
    {
        bool removedProps = RemoveAllProps();
        
        if (removedProps)
            ViewManager.Instance.NotifyUser("(Synced) Client", "Removed all spawned props", 1f);
        else
            ViewManager.Instance.NotifyUser("(Local) Client", "No props to remove", 1f);
        
        return false;
    }

    private static bool _skipBecauseIsRedo; // Hack
    public static void OnTrySpawnProp()
    {
        if (_skipBecauseIsRedo) return;
        if (!IsPropSpawnAllowed() || IsAtPropLimit())
        {
            PlaySound(SFX_Deny);
            return;
        }
        PlaySound(SFX_Spawn);
    }
    
    // Prop history for session
    public class PropHistoryData
    {
        public string PropId;
        public string PropName;
        public string SpawnerName;
        public string InstanceId;
        public bool IsDestroyed;
    }

    // Deleted prop info for undo history
    private class DeletedPropData
    {
        public readonly string PropId;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float TimeDeleted;

        public bool IsWithinTimeLimit => Time.time - TimeDeleted <= RedoTimeoutLimit;

        public DeletedPropData(CVRSyncHelper.PropData propData)
        {
            CVRSpawnable spawnable = propData.Spawnable;
            
            if (spawnable == null)
            {
                // use original spawn position and rotation / last known position and rotation
                Position = new Vector3(propData.PositionX, propData.PositionY, propData.PositionZ);
                Rotation = Quaternion.Euler(new Vector3(propData.RotationX, propData.RotationY, propData.RotationZ));
            }
            else
            {
                Transform spawnableTransform = spawnable.transform;
                Position = spawnableTransform.position;
                Rotation = spawnableTransform.rotation;

                // Offset spawn height so game can account for it later
                Position.y -= spawnable.spawnHeight;
            }

            PropId = propData.ObjectId;
            TimeDeleted = Time.time;
        }
    }

    // Visualizer
// Add this enum at the top of your file or in a separate file
public enum PropVisualizerMode
{
    SourceRenderers,
    HologramSource,
    HologramBounds
}

// Modified code:

private static string _lastVisualizerInstanceId;
private static object _currentFetchCoroutine;
private static bool _cancelPendingFetch;
private static CVRSyncHelper.PropData _visualizerPropData;
private static GameObject _visualizerGameObject;
private static List<Animator> _visualizerAnimators = new List<Animator>();
private static object _animatorPulseCoroutine;

public static bool IsVisualizerActive => _visualizerGameObject != null && !string.IsNullOrEmpty(_lastVisualizerInstanceId);

public static void OnSelectPropToSpawn(string guid, string propImage, string propName)
{
    // Cancel any existing fetch
    OnClearPropToSpawn();

    if (!ModSettings.EntryPropSpawnVisualizer.Value)
        return;
    
    _cancelPendingFetch = false;
    _currentFetchCoroutine = MelonCoroutines.Start(FetchAndSpawnProp(guid));
}

private static IEnumerator FetchAndSpawnProp(string guid)
{
    PropsButBetterMod.Logger.Msg($"Fetching prop meta for {guid}...");
    
    // Start the async task
    var task = PropApiHelper.GetPropMeta(guid);
    
    // Wait for completion
    while (!task.IsCompleted)
    {
        if (_cancelPendingFetch)
        {
            PropsButBetterMod.Logger.Msg("Fetch cancelled by user");
            _currentFetchCoroutine = null;
            yield break;
        }
        yield return null;
    }
    
    // Check for cancellation after task completes
    if (_cancelPendingFetch)
    {
        PropsButBetterMod.Logger.Msg("Fetch cancelled by user");
        _currentFetchCoroutine = null;
        yield break;
    }
    
    // Check for errors
    if (task.IsFaulted)
    {
        PropsButBetterMod.Logger.Error($"Failed to fetch prop meta: {task.Exception?.Message}");
        _currentFetchCoroutine = null;
        yield break;
    }
    
    var response = task.Result;
    
    // Validate response
    if (response == null || !response.IsSuccessStatusCode)
    {
        PropsButBetterMod.Logger.Error($"Failed to fetch prop meta: {response?.Message ?? "Unknown error"}");
        _currentFetchCoroutine = null;
        yield break;
    }
    
    UgcWithFile propMeta = response.Data;
    
    if (propMeta == null)
    {
        PropsButBetterMod.Logger.Error("Prop meta data was null");
        _currentFetchCoroutine = null;
        yield break;
    }
    
    // Create metadata
    AssetManagement.UgcMetadata metadata = new()
    {
        AssetName = propMeta.Name,
        AssetId = propMeta.Id,
        FileSize = propMeta.FileSize,
        FileKey = propMeta.FileKey,
        FileHash = propMeta.FileHash,
        TagsData = new UgcContentTags(propMeta.Tags),
        CompatibilityVersion = propMeta.CompatibilityVersion,
        EncryptionAlgorithm = propMeta.EncryptionAlgorithm
    };

    // Generate instance ID for visualizer
    _lastVisualizerInstanceId = Guid.NewGuid().ToString();

    // Register prop data
    CVRSyncHelper.PropData newPropData = CVRSyncHelper.PropData.PropDataPool.GetObject();
    newPropData.InstanceId = _lastVisualizerInstanceId;
    newPropData.ContentMetadata = metadata;
    newPropData.ObjectId = guid;
    newPropData.SpawnedBy = "SYSTEM";

    CVRSyncHelper.Props.Add(newPropData);
    _visualizerPropData = newPropData; // Cache it

    // Queue download
    CVRDownloadManager.Instance.QueueTask(metadata, DownloadTask.ObjectType.Prop,
        propMeta.FileLocation, propMeta.FileId, _lastVisualizerInstanceId, spawnerId: "SYSTEM");
    
    PropsButBetterMod.Logger.Msg($"Queued prop '{propMeta.Name}' for download");
    
    _currentFetchCoroutine = null;
}

public static void OnVisualizerPropSpawned()
{
    if (_visualizerPropData == null || _visualizerPropData.Wrapper == null)
        return;

    Transform rootTransform = _visualizerPropData.Wrapper.transform;
    _visualizerGameObject = rootTransform.gameObject;

    PropVisualizerMode mode = ModSettings.EntryPropSpawnVisualizerMode.Value;

    if (mode == PropVisualizerMode.HologramBounds)
    {
        ProcessHologramBoundsMode();
    }
    else
    {
        ProcessSourceRenderersMode();
    }
    
    // Start the animator pulse coroutine
    if (_visualizerAnimators.Count > 0)
        _animatorPulseCoroutine = MelonCoroutines.Start(PulseAnimators());
    
    PropsButBetterMod.Logger.Msg($"Visualizer prop spawned in {mode} mode");
}

private static void ProcessSourceRenderersMode()
{
    var allComponents = _visualizerGameObject.GetComponentsInChildren<Component>(true);
    _visualizerAnimators.Clear();

    List<Material> _sharedMaterials = new();
    
    foreach (var component in allComponents)
    {
        if (component == null) continue;
        
        // Keep these components
        if (component is Transform) continue;
        if (component is Renderer renderer)
        {
            if (ModSettings.EntryPropSpawnVisualizerMode.Value == PropVisualizerMode.HologramSource)
            {
                int materialCount = renderer.GetMaterialCount();
                // Resize sharedMaterials list efficiently
                if (_sharedMaterials.Count < materialCount)
                {
                    for (int i = _sharedMaterials.Count; i < materialCount; i++)
                        _sharedMaterials.Add(MetaPort.Instance.hologrammMaterial);
                }
                else if (_sharedMaterials.Count > materialCount)
                {
                    _sharedMaterials.RemoveRange(materialCount, _sharedMaterials.Count - materialCount);
                }            
                renderer.SetSharedMaterials(_sharedMaterials);
            }
            continue;
        }
        if (component is MeshFilter) continue;
        if (component is CVRSpawnable) continue;
        if (component is CVRAssetInfo) continue;
        if (component is ParticleSystem) continue;
        if (component is Animator animator)
        {
            animator.enabled = false;
            _visualizerAnimators.Add(animator);
            continue;
        }
        
        // Remove everything else
        component.DestroyComponentWithRequirements();
    }
}

private static void ProcessHologramBoundsMode()
{
    var allComponents = _visualizerGameObject.GetComponentsInChildren<Component>(true);
    _visualizerAnimators.Clear();
    
    Mesh cubeMesh = PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Cube);
    
    foreach (var component in allComponents)
    {
        if (component == null) continue;
        
        // Handle animators
        if (component is Animator animator)
        {
            animator.enabled = false;
            _visualizerAnimators.Add(animator);
            continue;
        }
        
        // Handle renderers - create hologram cubes for visible ones
        if (component is Renderer renderer)
        {
            // Check if renderer is visible
            bool isVisible = renderer.enabled && 
                            renderer.gameObject.activeInHierarchy && 
                            renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            
            if (isVisible)
            {
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer smr)
                    mesh = smr.sharedMesh;
                else if (renderer.TryGetComponent(out MeshFilter filter))
                    mesh = filter.sharedMesh;

                if (mesh != null)
                {
                    // Get bounds of mesh
                    Bounds localBounds = MeshBoundsUtility.CalculateTightBounds(mesh);
                    
                    PropsButBetterMod.Logger.Msg(localBounds);
                
                    // Create child GameObject for the cube
                    GameObject cubeObj = new GameObject("HologramCube");
                    cubeObj.transform.SetParent(renderer.transform, false);

                    // Add mesh filter and renderer to child
                    MeshFilter cubeMeshFilter = cubeObj.AddComponent<MeshFilter>();
                    cubeMeshFilter.sharedMesh = cubeMesh;
                
                    MeshRenderer cubeRenderer = cubeObj.AddComponent<MeshRenderer>();
                    cubeRenderer.sharedMaterial = MetaPort.Instance.hologrammMaterial;
                    
                    // Account for lossy scale when setting the cube's scale
                    /*Vector3 sourceScale = renderer.transform.lossyScale;
                    Bounds scaledBounds = new(
                        Vector3.Scale(localBounds.center, sourceScale),
                        Vector3.Scale(localBounds.size, sourceScale)
                    );*/

                    cubeObj.transform.localPosition = localBounds.center;
                    cubeObj.transform.localRotation = Quaternion.identity;
                    cubeObj.transform.localScale = localBounds.size;
                }
                else
                {
                    PropsButBetterMod.Logger.Msg("No mesh on "  + renderer.gameObject.name);
                }
            }
        }
        
        // Keep these components
        if (component is Transform) continue;
        if (component is CVRSpawnable) continue;
        if (component is CVRAssetInfo) continue;
        if (component is MeshFilter) continue; 
        
        // Remove everything else
        try { component.DestroyComponentWithRequirements(); }
        catch (Exception)
        {
            // ignored
        }
    }
}

private static IEnumerator PulseAnimators()
{
    const float skipDuration = 10f;
    const float skipRealTime = 5f;

    const float pulseAmplitude = 2f;   // seconds forward/back
    const float pulseFrequency = 0.2f;   // Hz (1 cycle every 2s)

    float t = 0f;
    float lastPos = 0f;

    while (true)
    {
        if (_visualizerAnimators == null || _visualizerAnimators.Count == 0)
            yield break;

        t += Time.deltaTime;

        // 0 → 1 blend from "skip" to "pulse"
        float blend = Mathf.SmoothStep(0f, 1f, t / skipRealTime);

        // Linear fast-forward (position-based, not speed-based)
        float skipPos = Mathf.Lerp(
            0f,
            skipDuration,
            Mathf.Clamp01(t / skipRealTime)
        );

        // Continuous oscillation around skipDuration
        float pulsePos =
            skipDuration +
            Mathf.Sin(t * Mathf.PI * 2f * pulseFrequency) * pulseAmplitude;

        // Final blended position
        float currentPos = Mathf.Lerp(skipPos, pulsePos, blend);

        float delta = currentPos - lastPos;
        lastPos = currentPos;

        foreach (var animator in _visualizerAnimators)
        {
            if (animator != null)
                animator.Update(delta);
        }

        yield return null;
    }
}

public static void OnHandlePropSpawn(ref ControllerRay __instance)
{
    if (!__instance.uiActive) return; // Skip offhand
    if (!IsVisualizerActive) return; // No visualizer active

    Vector3 spawnPos = __instance.Hit.point;
    Quaternion spawnRot = GetPropSpawnRotation(spawnPos);
    
    // Position active visualizer at this position
    _visualizerGameObject.transform.position = spawnPos;
    _visualizerGameObject.transform.rotation = spawnRot;
}

private static Quaternion GetPropSpawnRotation(Vector3 spawnPos)
{
    Vector3 playerPos = PlayerSetup.Instance.GetPlayerPosition();
    Vector3 directionToPlayer = (playerPos - spawnPos).normalized;

    // Get gravity direction for objects
    GravitySystem.GravityResult result = GravitySystem.TryGetResultingGravity(spawnPos, false);
    Vector3 gravityDirection = result.AppliedGravity.normalized;

    // If there is no gravity, use transform.up
    if (gravityDirection == Vector3.zero) 
        gravityDirection = -PlayerSetup.Instance.transform.up;

    Vector3 projectedDirectionToPlayer = Vector3.ProjectOnPlane(directionToPlayer, gravityDirection).normalized;

    if (projectedDirectionToPlayer == Vector3.zero)
    {
        projectedDirectionToPlayer = Vector3.Cross(gravityDirection, Vector3.right).normalized;
        if (projectedDirectionToPlayer == Vector3.zero)
            projectedDirectionToPlayer = Vector3.Cross(gravityDirection, Vector3.forward).normalized;
    }

    Vector3 right = Vector3.Cross(gravityDirection, projectedDirectionToPlayer).normalized;
    Vector3 finalForward = Vector3.Cross(right, gravityDirection).normalized;

    if (Vector3.Dot(finalForward, directionToPlayer) < 0)
        finalForward *= -1;

    Quaternion rotation = Quaternion.LookRotation(finalForward, -gravityDirection);
    return rotation;
}

public static void OnClearPropToSpawn()
{
    // Stop animator pulse coroutine
    if (_animatorPulseCoroutine != null)
    {
        MelonCoroutines.Stop(_animatorPulseCoroutine);
        _animatorPulseCoroutine = null;
    }
    
    // Clear animator list
    _visualizerAnimators?.Clear();
    
    // Stop any pending fetch coroutine
    if (_currentFetchCoroutine != null)
    {
        _cancelPendingFetch = true;
        MelonCoroutines.Stop(_currentFetchCoroutine);
        _currentFetchCoroutine = null;
        PropsButBetterMod.Logger.Msg("Stopped pending fetch operation");
    }
    
    if (string.IsNullOrEmpty(_lastVisualizerInstanceId))
        return;
    
    // Cancel pending attachment in download queue
    CVRDownloadManager.Instance.CancelAttachment(_lastVisualizerInstanceId);
    
    // Find and remove prop data
    if (_visualizerPropData != null)
    {
        CVRSyncHelper.Props.Remove(_visualizerPropData);
        _visualizerPropData.Recycle(); // this removes the gameobject if spawned
        _visualizerPropData = null;
    }
    
    _visualizerGameObject = null;
    _lastVisualizerInstanceId = null;
    
    PropsButBetterMod.Logger.Msg("Cleared prop visualizer");
}
}