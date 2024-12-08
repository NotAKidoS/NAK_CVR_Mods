using UnityEngine;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using NAK.ShareBubbles.Impl;
using NAK.ShareBubbles.Networking;
using NAK.ShareBubbles.UI;
using TMPro;
using System.Collections;
using ABI_RC.Core.InteractionSystem;
using ShareBubbles.ShareBubbles.Implementation;

namespace NAK.ShareBubbles;

public struct BubbleContentInfo
{
    public string Name { get; set; }
    public string Label { get; set; }
    public Texture2D Icon { get; set; }
}
    
public class ShareBubble : MonoBehaviour
{
    // Shader properties
    private static readonly int _MatcapHueShiftId = Shader.PropertyToID("_MatcapHueShift");
    private static readonly int _MatcapReplaceId = Shader.PropertyToID("_MatcapReplace");
        
    #region Enums

    // State of fetching content info from api
    private enum InfoFetchState
    {
        Fetching, // Fetching content info from api
        Error,    // Content info fetch failed
        Ready     // Content info fetched successfully
    }
    
    // State of asking for content claim from owner
    private enum ClaimState
    {
        Waiting,      // No claim request sent
        Requested, // Claim request sent to owner, waiting for response
        Rejected,  // Claim request rejected by owner
        Permitted, // Claim request accepted by owner, or content is already unlocked
    }

    #endregion Enums
        
    #region Properties
        
    private InfoFetchState _currentApiState;

    private InfoFetchState CurrentApiState
    {
        get => _currentApiState;
        set
        {
            _currentApiState = value;
            UpdateVisualState();
        }
    }

    private ClaimState _currentClaimState;

    private ClaimState CurrentClaimState
    {
        get => _currentClaimState;
        set
        {
            _currentClaimState = value;
            UpdateClaimState();
        }
    }

    public bool IsDestroyed { get; set; }
    public string OwnerId;
    public ShareBubbleData Data { get; private set; }
    public bool IsOwnBubble => OwnerId == MetaPort.Instance.ownerId;
    public bool IsPermitted => (implementation?.IsPermitted ?? false) || CurrentClaimState == ClaimState.Permitted;

    #endregion Properties

    #region Private Fields
        
    private IShareBubbleImpl implementation;
    private DateTime? lastClaimRequest;
    private const float ClaimTimeout = 30f;
    private Coroutine claimStateResetCoroutine;
        
    private float lifetimeTotal;
        
    #endregion Private Fields

    #region Serialized Fields
        
    [Header("Visual Components")]
    [SerializeField] private Renderer hexRenderer;
    [SerializeField] private Renderer iconRenderer;
    [SerializeField] private TextMeshPro droppedByText;
    [SerializeField] private TextMeshPro contentName;
    [SerializeField] private TextMeshPro contentLabel;
    [SerializeField] private BubbleAnimController animController;

    [Header("State Objects")]
    [SerializeField] private GameObject loadingState;
    [SerializeField] private GameObject lockedState;
    [SerializeField] private GameObject errorState;
    [SerializeField] private GameObject contentInfo;
        
    [Header("Button UI")]
    [SerializeField] private TextMeshProUGUI equipButtonLabel;
    [SerializeField] private TextMeshProUGUI claimButtonLabel;
        
    #endregion Serialized Fields

    #region Public Methods

    public void SetEquipButtonLabel(string label)
    {
        equipButtonLabel.text = label;
    }
        
    #endregion Public Methods

    #region Lifecycle Methods
        
    public async void Initialize(ShareBubbleData data, IShareBubbleImpl impl)
    {
        animController = GetComponent<BubbleAnimController>();
            
        Data = data;
        implementation = impl;
        implementation.Initialize(this);
            
        CurrentApiState = InfoFetchState.Fetching;
        CurrentClaimState = ClaimState.Waiting;
            
        string playerName = IsOwnBubble 
            ? AuthManager.Username 
            : CVRPlayerManager.Instance.TryGetPlayerName(OwnerId);
            
        droppedByText.text = $"Dropped by\n{playerName}";
            
        if (Data.Lifetime == ShareLifetime.TwoMinutes)
            lifetimeTotal = 120f;
            
        try 
        {
            await implementation.FetchContentInfo();
            if (this == null || gameObject == null) return; // Bubble was destroyed during fetch
            CurrentApiState = InfoFetchState.Ready;
        }
        catch (Exception ex)
        {
            ShareBubblesMod.Logger.Error($"Failed to load content info: {ex}");
            if (this == null || gameObject == null) return; // Bubble was destroyed during fetch
            CurrentApiState = InfoFetchState.Error;
        }
    }

    private void Update()
    {
        if (Data.Lifetime == ShareLifetime.Session)
            return;
        
        float lifetimeElapsed = (float)(DateTime.UtcNow - Data.CreatedAt).TotalSeconds;
        float lifetimeProgress = Mathf.Clamp01(lifetimeElapsed / lifetimeTotal);
        animController.SetLifetimeVisual(lifetimeProgress);
        if (lifetimeProgress >= 1f)
        {
            //ShareBubblesMod.Logger.Msg($"Bubble expired: {Data.BubbleId}");
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        implementation?.Cleanup();
        if (ShareBubbleManager.Instance != null && !IsDestroyed)
            ShareBubbleManager.Instance.OnBubbleDestroyed(OwnerId, Data.BubbleId);
    }
        
    #endregion Lifecycle Methods

    #region Visual State Management
        
    private void UpdateVisualState()
    {
        loadingState.SetActive(CurrentApiState == InfoFetchState.Fetching);
        errorState.SetActive(CurrentApiState == InfoFetchState.Error);
        contentInfo.SetActive(CurrentApiState == InfoFetchState.Ready);
            
        hexRenderer.material.SetFloat(_MatcapReplaceId, 
            CurrentApiState == InfoFetchState.Fetching ? 0f : 1f);
            
        if (CurrentApiState == InfoFetchState.Ready)
        {
            if (IsPermitted) CurrentClaimState = ClaimState.Permitted;
            animController.ShowHubPivot();
            UpdateButtonStates();
        }
    }

    private void UpdateButtonStates()
    {
        bool canClaim = !IsPermitted && Data.Access != ShareAccess.None && CanRequestClaim();
        bool canEquip = IsPermitted && CurrentApiState == InfoFetchState.Ready;

        claimButtonLabel.transform.parent.gameObject.SetActive(canClaim); // Only show claim button if content is locked & claimable
        equipButtonLabel.transform.parent.gameObject.SetActive(canEquip); // Only show equip button if content is unlocked

        if (canClaim) UpdateClaimButtonState();
    }

    private void UpdateClaimButtonState()
    {
        switch (CurrentClaimState)
        {
            case ClaimState.Requested:
                claimButtonLabel.text = "Claiming...";
                break;
            case ClaimState.Rejected:
                claimButtonLabel.text = "Denied :(";
                StartClaimStateResetTimer();
                break;
            case ClaimState.Permitted:
                claimButtonLabel.text = "Claimed!";
                StartClaimStateResetTimer();
                break;
            default:
                claimButtonLabel.text = "<sprite=3> Claim";
                break;
        }
    }

    private void StartClaimStateResetTimer()
    {
        if (claimStateResetCoroutine != null) StopCoroutine(claimStateResetCoroutine);
        claimStateResetCoroutine = StartCoroutine(ResetClaimStateAfterDelay());
    }

    private IEnumerator ResetClaimStateAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        CurrentClaimState = ClaimState.Waiting;
        UpdateClaimButtonState();
    }

    private void UpdateClaimState()
    {
        bool isLocked = !IsPermitted && CurrentApiState == InfoFetchState.Ready;
        lockedState.SetActive(isLocked); // Only show locked state if content is locked & ready
        UpdateButtonStates();
    }
    
    #endregion Visual State Management

    #region Content Info Updates
        
    public void SetHue(float hue)
    {
        hexRenderer.material.SetFloat(_MatcapHueShiftId, hue);
    }
        
    public void UpdateContent(BubbleContentInfo info)
    {
        contentName.text = info.Name;
        contentLabel.text = info.Label;
        if (info.Icon != null) 
            iconRenderer.material.mainTexture = info.Icon;
    }
        
    #endregion Content Info Updates

    #region Interaction Methods
        
    public void ViewDetailsPage()
    {
        if (CurrentApiState != InfoFetchState.Ready) return;
        implementation?.ViewDetailsPage();
    }

    public void EquipContent()
    {
        // So uh, selecting a prop in will also spawn it as interact is down in same frame
        // Too lazy to fix so we gonna wait a frame before actually equipping :)
        StartCoroutine(ActuallyEquipContentNextFrame());

        return;
        IEnumerator ActuallyEquipContentNextFrame()
        {
            yield return null; // Wait a frame
            
            if (CurrentApiState != InfoFetchState.Ready) yield break;
            
            if (CanRequestClaim()) // Only possible on hold & click, as button is hidden when not permitted
            {
                RequestContentClaim();
                yield break;
            }
        
            if (!IsPermitted) 
                yield break;
        
            implementation?.EquipContent();
        }
    }

    public void RequestContentClaim()
    {
        if (!CanRequestClaim()) return;

        // Fire and forget but with error handling~
        _ = RequestContentClaimAsync().ContinueWith(task =>
        {
            if (!task.IsFaulted) 
                return;
            
            ShareBubblesMod.Logger.Error($"Error requesting content claim: {task.Exception}");
            CurrentClaimState = ClaimState.Rejected;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private async Task<ModNetwork.ClaimResponseType> RequestContentClaimAsync()
    {
        if (!CanRequestClaim()) 
            return ModNetwork.ClaimResponseType.Rejected;

        CurrentClaimState = ClaimState.Requested;

        try
        {
            ModNetwork.ClaimResponseType response = await ModNetwork.SendBubbleClaimRequestAsync(OwnerId, Data.BubbleId);
            
            switch (response)
            {
                case ModNetwork.ClaimResponseType.Accepted:
                case ModNetwork.ClaimResponseType.AlreadyShared:
                    CurrentClaimState = ClaimState.Permitted;
                    break;
                default:
                case ModNetwork.ClaimResponseType.Rejected:
                case ModNetwork.ClaimResponseType.Timeout:
                    CurrentClaimState = ClaimState.Rejected;
                    break;
                case ModNetwork.ClaimResponseType.NotAcceptingSharesFromNonFriends:
                    CurrentClaimState = ClaimState.Rejected;
                    
                    string ownerName = CVRPlayerManager.Instance.TryGetPlayerName(OwnerId);
                    
                    ShareBubblesMod.Logger.Msg($"Claim request for {Data.BubbleId} rejected: " +
                                               $"You are not friends with the owner ({ownerName}) and do not have the permission " +
                                               $"enabled on the Community Hub to accept shares from non-friends.");
                    // Display in UI
                    ViewManager.Instance.TriggerAlert("Claim Request Rejected",
                        $"You are not friends with the owner ({ownerName}) and do not have the permission " +
                        "enabled on the Community Hub to accept shares from non-friends.", -1, false);
                    
                    break;
            }

            return response;
        }
        catch
        {
            CurrentClaimState = ClaimState.Rejected;
            return ModNetwork.ClaimResponseType.Rejected;
        }
    }

    private bool CanRequestClaim()
    {
        if (IsPermitted) return false;
        if (IsOwnBubble) return false;
        return OwnerId == implementation.AuthorId;
    }
        
    #endregion Interaction Methods

    #region Mod Network Callbacks
    
    public void OnClaimResponseReceived(bool accepted)
    {
        lastClaimRequest = null;
        CurrentClaimState = accepted ? ClaimState.Permitted : ClaimState.Rejected;
        UpdateButtonStates();
    }

    public async void OnRemoteWantsClaim(string requesterId)
    {
        if (!IsOwnBubble) return;

        // Rule check bypass attempt - reject immediately
        if (Data.Rule == ShareRule.FriendsOnly && !Friends.FriendsWith(requesterId))
        {
            ModNetwork.SendBubbleClaimResponse(requesterId, Data.BubbleId, ModNetwork.ClaimResponseType.Rejected);
            return;
        }

        ShareClaimResult result = await implementation.HandleClaimAccept(requesterId);
        ModNetwork.SendBubbleClaimResponse(requesterId, Data.BubbleId, result.ResponseType);
    }
        
    #endregion Mod Network Callbacks
}