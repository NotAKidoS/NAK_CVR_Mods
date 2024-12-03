using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.Gravity;
using NAK.ShareBubbles.Impl;
using NAK.ShareBubbles.Networking;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.ShareBubbles;

public class ShareBubbleManager
{
    #region Constants
    
    public const int MaxBubblesPerUser = 3;
    private const float BubbleCreationCooldown = 1f;
    
    #endregion Constants

    #region Singleton
    
    public static ShareBubbleManager Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null) 
            return;
        
        Instance = new ShareBubbleManager();
        RegisterDefaultBubbleTypes();
        
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(Instance.OnPlayerSetupStart);
    }

    private static void RegisterDefaultBubbleTypes()
    {
        ShareBubbleRegistry.RegisterBubbleType(GetMaskedHash("Avatar"), () => new AvatarBubbleImpl());
        ShareBubbleRegistry.RegisterBubbleType(GetMaskedHash("Spawnable"), () => new SpawnableBubbleImpl());
    }

    public static uint GetMaskedHash(string typeName) => StringToHash(typeName) & 0xFFFF;

    public static uint GenerateBubbleId(string content, uint implTypeHash)
    {
        // Allows us to extract the implementation type hash from the bubble ID
        return (implTypeHash << 16) | GetMaskedHash(content);
    }

    private static uint StringToHash(string str)
    {
        unchecked
        {
            uint hash = 5381;
            foreach (var ch in str) hash = ((hash << 5) + hash) + ch;
            return hash;
        }
    }
    
    #endregion

    #region Fields

    private class PlayerBubbleData
    {
        // Can only ever be true when the batched bubble message is what created the player data
        public bool IgnoreBubbleCreationCooldown;
        public float LastBubbleCreationTime;
        public readonly Dictionary<uint, ShareBubble> BubblesByLocalId = new();
    }

    private readonly Dictionary<string, PlayerBubbleData> playerBubbles = new();
    
    public bool IsPlacingBubbleMode { get; set; }
    private ShareBubbleData selectedBubbleData;
    
    #endregion Fields

    #region Game Events
    
    private void OnPlayerSetupStart()
    {
        CVRGameEventSystem.Instance.OnConnected.AddListener(OnConnected);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(OnPlayerLeft);
    }

    private void OnConnected(string _)
    {
        if (Instances.IsReconnecting) 
            return;
        
        // Clear all bubbles on disconnect (most should have died during world unload)
        foreach (PlayerBubbleData playerData in playerBubbles.Values)
        {
            foreach (ShareBubble bubble in playerData.BubblesByLocalId.Values.ToList())
                DestroyBubble(bubble);
        }
        playerBubbles.Clear();
        
        // This also acts to signal to other clients we rejoined and need our bubbles cleared
        ModNetwork.SendActiveBubblesRequest();
    }

    private void OnPlayerLeft(CVRPlayerEntity player)
    {
        if (!playerBubbles.TryGetValue(player.Uuid, out PlayerBubbleData playerData)) 
            return;

        foreach (ShareBubble bubble in playerData.BubblesByLocalId.Values.ToList())
            DestroyBubble(bubble);

        playerBubbles.Remove(player.Uuid);
    }
    
    #endregion Game Events

    #region Local Operations
    
    public void DropBubbleInFront(ShareBubbleData data)
    {
        PlayerSetup localPlayer = PlayerSetup.Instance;
        string localPlayerId = MetaPort.Instance.ownerId;

        if (!CanPlayerCreateBubble(localPlayerId))
        {
            ShareBubblesMod.Logger.Msg("Bubble creation on cooldown!");
            return;
        }

        float playSpaceScale = localPlayer.GetPlaySpaceScale();
        Vector3 playerForward = localPlayer.GetPlayerForward();
        Vector3 position = localPlayer.activeCam.transform.position + playerForward * 0.5f * playSpaceScale;

        if (Physics.Raycast(position,
                localPlayer.CharacterController.GetGravityDirection(),
                out RaycastHit raycastHit, 4f, localPlayer.dropPlacementMask))
        {
            CreateBubbleForPlayer(localPlayerId, raycastHit.point, 
                Quaternion.LookRotation(playerForward, raycastHit.normal), data);
            return;
        }
        
        CreateBubbleForPlayer(localPlayerId, position, 
            Quaternion.LookRotation(playerForward, -localPlayer.transform.up), data);
    }
    
    public void SelectBubbleForPlace(string contentCouiPath, string contentName, ShareBubbleData data)
    {
        selectedBubbleData = data;
        IsPlacingBubbleMode = true;
        CohtmlHud.Instance.SelectPropToSpawn(
            contentCouiPath,
            contentName, 
            "Selected content to bubble:");
    }

    public void PlaceSelectedBubbleFromControllerRay(Transform transform)
    {
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;
        
        // Every layer other than IgnoreRaycast, PlayerLocal, PlayerClone, PlayerNetwork, and UI Internal
        const int LayerMask = ~((1 << 2) | (1 << 8) | (1 << 9) | (1 << 10) | (1 << 15));
        if (!Physics.Raycast(position, forward, out RaycastHit hit, 
                10f, LayerMask, QueryTriggerInteraction.Ignore)) 
            return; // No hit
        
        if (selectedBubbleData.ImplTypeHash == 0)
            return;
        
        Vector3 bubblePos = hit.point;

        PlayerSetup localPlayer = PlayerSetup.Instance;
        Vector3 playerPos = localPlayer.GetPlayerPosition();
        
        // Sample gravity at bubble position to get bubble orientation
        Vector3 bubbleUp = -GravitySystem.TryGetResultingGravity(bubblePos, false).AppliedGravity.normalized;
        Vector3 bubbleForward = Vector3.ProjectOnPlane((playerPos - bubblePos).normalized, bubbleUp);
        
        // Bubble faces player aligned with gravity
        Quaternion bubbleRot = Quaternion.LookRotation(bubbleForward, bubbleUp);
        
        CreateBubbleForPlayer(MetaPort.Instance.ownerId, bubblePos, bubbleRot, selectedBubbleData);
    }
    
    #endregion Local Operations

    #region Player Callbacks

    private void CreateBubbleForPlayer(
        string playerId,
        Vector3 position,
        Quaternion rotation,
        ShareBubbleData data)
    {
        GameObject bubbleRootObject = null;
        try
        {
            if (!CanPlayerCreateBubble(playerId))
                return;

            if (!ShareBubbleRegistry.TryCreateImplementation(data.ImplTypeHash, out IShareBubbleImpl impl))
            {
                Debug.LogError($"Failed to create bubble: Unknown bubble type hash: {data.ImplTypeHash}");
                return;
            }

            // Get or create player data
            if (!playerBubbles.TryGetValue(playerId, out PlayerBubbleData playerData))
            {
                playerData = new PlayerBubbleData();
                playerBubbles[playerId] = playerData;
            }

            // If a bubble with this ID already exists for this player, destroy it
            if (playerData.BubblesByLocalId.TryGetValue(data.BubbleId, out ShareBubble existingBubble))
            {
                ShareBubblesMod.Logger.Msg($"Replacing existing bubble: {data.BubbleId}");
                DestroyBubble(existingBubble);
            }
            // Check bubble limit only if we're not replacing an existing bubble
            else if (playerData.BubblesByLocalId.Count >= MaxBubblesPerUser)
            {
                ShareBubble oldestBubble = playerData.BubblesByLocalId.Values
                    .OrderBy(b => b.Data.CreatedAt)
                    .First();
                ShareBubblesMod.Logger.Msg($"Bubble limit reached, destroying oldest bubble: {oldestBubble.Data.BubbleId}");
                DestroyBubble(oldestBubble);
            }

            bubbleRootObject = Object.Instantiate(ShareBubblesMod.SharingBubblePrefab);
            bubbleRootObject.transform.SetLocalPositionAndRotation(position, rotation);
            bubbleRootObject.SetActive(true);

            Transform bubbleTransform = bubbleRootObject.transform.GetChild(0);
            if (bubbleTransform == null)
                throw new InvalidOperationException("Bubble prefab is missing expected child transform");

            (float targetHeight, float scaleModifier) = GetBubbleHeightAndScale();
            bubbleTransform.localPosition = new Vector3(0f, targetHeight, 0f);
            bubbleTransform.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);

            ShareBubble bubble = bubbleRootObject.GetComponent<ShareBubble>();
            if (bubble == null)
                throw new InvalidOperationException("Bubble prefab is missing ShareBubble component");

            bubble.OwnerId = playerId;
            bubble.Initialize(data, impl);
            playerData.BubblesByLocalId[data.BubbleId] = bubble;
            playerData.LastBubbleCreationTime = Time.time;

            if (playerId == MetaPort.Instance.ownerId)
                ModNetwork.SendBubbleCreated(position, rotation, data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create bubble (ID: {data.BubbleId}, Owner: {playerId}): {ex}");
            
            // Clean up the partially created bubble if it exists
            if (bubbleRootObject != null)
            {
                Object.DestroyImmediate(bubbleRootObject);
                
                // Remove from player data if it was added
                if (playerBubbles.TryGetValue(playerId, out PlayerBubbleData playerData))
                    playerData.BubblesByLocalId.Remove(data.BubbleId);
            }
        }
    }

    public void DestroyBubble(ShareBubble bubble)
    {
        if (bubble == null) return;
        bubble.IsDestroyed = true; // Prevents ShareBubble.OnDestroy invoking OnBubbleDestroyed
        Object.DestroyImmediate(bubble.gameObject);
        OnBubbleDestroyed(bubble.OwnerId, bubble.Data.BubbleId);
    }
    
    public void OnBubbleDestroyed(string ownerId, uint bubbleId)
    {
        if (playerBubbles.TryGetValue(ownerId, out PlayerBubbleData playerData))
            playerData.BubblesByLocalId.Remove(bubbleId);
        
        if (ownerId == MetaPort.Instance.ownerId) ModNetwork.SendBubbleDestroyed(bubbleId);
    }

    #endregion

    #region Remote Events

    public void SetRemoteBatchCreateBubbleState(string ownerId, bool batchCreateBubbleState)
    {
        // Get or create player data
        if (!playerBubbles.TryGetValue(ownerId, out PlayerBubbleData playerData))
        {
            playerData = new PlayerBubbleData();
            playerBubbles[ownerId] = playerData;
            
            // If player data didn't exist, ignore bubble creation cooldown for the time we create active bubbles on join
            playerData.IgnoreBubbleCreationCooldown = batchCreateBubbleState;
        }
        else
        {
            // If player data already exists, ignore batched bubble creation
            // Probably makes a race condition here, but it's not critical
            // This will prevent users from using the batched bubble creation when they've already created bubbles
            playerData.IgnoreBubbleCreationCooldown = false;
        }
    }
    
    public void OnRemoteBubbleCreated(string ownerId, Vector3 position, Vector3 rotation, ShareBubbleData data)
    {
        CreateBubbleForPlayer(ownerId, position, Quaternion.Euler(rotation), data);
    }

    public void OnRemoteBubbleDestroyed(string ownerId, uint bubbleId)
    {
        if (!playerBubbles.TryGetValue(ownerId, out PlayerBubbleData playerData) ||
            !playerData.BubblesByLocalId.TryGetValue(bubbleId, out ShareBubble bubble))
            return;

        DestroyBubble(bubble);
    }
    
    public void OnRemoteBubbleMoved(string ownerId, uint bubbleId, Vector3 position, Vector3 rotation)
    {
        if (!playerBubbles.TryGetValue(ownerId, out PlayerBubbleData playerData) ||
            !playerData.BubblesByLocalId.TryGetValue(bubbleId, out ShareBubble bubble))
            return;

        // TODO: fix
        bubble.transform.parent.SetPositionAndRotation(position, Quaternion.Euler(rotation));
    }

    public void OnRemoteBubbleClaimRequest(string requesterUserId, uint bubbleId)
    {
        // Get our bubble data if exists, respond to requester
        string ownerId = MetaPort.Instance.ownerId;
        if (!playerBubbles.TryGetValue(ownerId, out PlayerBubbleData playerData) ||
            !playerData.BubblesByLocalId.TryGetValue(bubbleId, out ShareBubble bubble))
            return;

        bubble.OnRemoteWantsClaim(requesterUserId);
    }

    public void OnRemoteBubbleClaimResponse(string ownerId, uint bubbleId, bool wasAccepted)
    {
        // Get senders bubble data if exists, receive response
        if (!playerBubbles.TryGetValue(ownerId, out PlayerBubbleData playerData) ||
            !playerData.BubblesByLocalId.TryGetValue(bubbleId, out ShareBubble bubble))
            return;

        bubble.OnClaimResponseReceived(wasAccepted);
    }
    
    public void OnRemoteActiveBubbleRequest(string requesterUserId)
    {
        // Clear all bubbles for requester (as this msg is sent by them on initial join)
        // This catches the case where they rejoin faster than we heard their leave event
        if (playerBubbles.TryGetValue(requesterUserId, out PlayerBubbleData playerData))
        {
            foreach (ShareBubble bubble in playerData.BubblesByLocalId.Values.ToList())
                DestroyBubble(bubble);
        }
        
        var myBubbles = GetOwnActiveBubbles();
        if (myBubbles.Count == 0) 
            return; // No bubbles to send
        
        // Send all active bubbles to requester
        ModNetwork.SendActiveBubblesResponse(requesterUserId, myBubbles);
    }

    #endregion

    #region Utility Methods

    private bool CanPlayerCreateBubble(string playerId)
    {
        if (!playerBubbles.TryGetValue(playerId, out PlayerBubbleData playerData))
            return true;
        
        if (playerData.IgnoreBubbleCreationCooldown) 
            return true; // Only ignore for the time we create active bubbles on join

        return Time.time - playerData.LastBubbleCreationTime >= BubbleCreationCooldown;
    }

    private static (float, float) GetBubbleHeightAndScale()
    {
        float targetHeight = 0.5f * PlayerSetup.Instance.GetAvatarHeight();
        float scaleModifier = PlayerSetup.Instance.GetPlaySpaceScale() * 1.8f;
        return (targetHeight, scaleModifier);
    }

    public void OnPlayerScaleChanged()
    {
        (float targetHeight, float scaleModifier) = GetBubbleHeightAndScale();
        
        foreach (PlayerBubbleData playerData in playerBubbles.Values)
        {
            foreach (ShareBubble bubble in playerData.BubblesByLocalId.Values)
            {
                if (bubble == null) continue;
                Transform bubbleOffset = bubble.transform.GetChild(0);
                Vector3 localPos = bubbleOffset.localPosition;
                bubbleOffset.localPosition = new Vector3(localPos.x, targetHeight, localPos.z);
                bubbleOffset.localScale = new Vector3(scaleModifier, scaleModifier, scaleModifier);
            }
        }
    }

    private List<ShareBubble> GetOwnActiveBubbles()
    {
        string ownerId = MetaPort.Instance.ownerId;
        return playerBubbles.TryGetValue(ownerId, out PlayerBubbleData playerData)
            ? playerData.BubblesByLocalId.Values.ToList()
            : new List<ShareBubble>();
    }

    #endregion
}