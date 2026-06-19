using ABI_RC.Core;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.IO.AssetManagement;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI_RC.Core.Util.Encryption;
using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.Components;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using ABI.CCK.Components;
using UnityEngine;

namespace NAK.PropsButBetter;

public static class QuickMenuPropSelect
{
    private static Page _page;
    private static Category _contentInfoCategory;
    private static ContentDisplay _contentDisplay;
    
    private static Category _contentActionsCategory;
    private static Button _deletePropButton;
    private static Button _selectPropButton;
    private static Button _reloadPropButton;
    private static Button _selectPlayerButton;
    
    // Page State
    private static string _currentSelectedPropId;
    private static CVRSyncHelper.PropData _currentSelectedPropData;
    private static PedestalInfoResponse _currentSpawnableResponse;
    
    private static CancellationTokenSource _fetchPropDetailsCts;
    
    public static void BuildUI()
    {
        _page = Page.GetOrCreatePage(nameof(PropsButBetter), "Prop Info", isRootPage: false, noTab: true);
        _page.OnPageClosed += () => _fetchPropDetailsCts?.Cancel();
        // _page.InPlayerlist = true; // TODO: Investigate removal of page forehead
        
        _contentInfoCategory = _page.AddCategory("Content Info", false, false);
        _contentDisplay = new ContentDisplay(_contentInfoCategory);
        
        _contentActionsCategory = _page.AddCategory("Quick Actions", true, true);
       
        _selectPropButton = _contentActionsCategory.AddButton("Spawn Prop", "PropsButBetter-select", "Select the Prop for spawning.");
        _selectPropButton.OnPress += OnSelectProp;
        
        _reloadPropButton = _contentActionsCategory.AddButton("Reload Prop", "PropsButBetter-reload", "Respawns the selected Prop.");
        _reloadPropButton.OnPress += OnReloadProp;
        
        _deletePropButton = _contentActionsCategory.AddButton("Delete Prop", "PropsButBetter-remove", "Delete the selected Prop.", ButtonStyle.TextWithIcon);
        _deletePropButton.OnPress += OnDeleteProp;
        
        _selectPlayerButton = _contentActionsCategory.AddButton("Select Spawner", "PropsButBetter-remove", "Select the spawner of the Prop.", ButtonStyle.FullSizeImage);
        _selectPlayerButton.OnPress += OnSelectPlayer;
    }

    public static void ListenForQM()
    {
        CVR_MenuManager.Instance.cohtmlView.View.RegisterForEvent("QuickMenuPropSelect-OpenDetails", (Action)OnOpenDetails);
    }
    
    public static void ShowInfo(CVRSyncHelper.PropData propData)
    {
        if (propData == null) return;
        
        _currentSelectedPropData = propData;
        
        CVR_MenuManager.Instance.ToggleQuickMenu(true);
        _page.OpenPage(false, false);

        _currentSelectedPropId = propData.ObjectId;
        
        AssetManagement.UgcMetadata metadata = propData.ContentMetadata;
        
        _page.MenuTitle = $"{metadata.AssetName}";
        _page.MenuSubtitle = $"Spawned by {CVRPlayerManager.Instance.TryGetPlayerName(propData.SpawnedBy)}";
        
        // Reset stuff
        _fetchPropDetailsCts?.Cancel();
        _fetchPropDetailsCts = new CancellationTokenSource();
        _currentSpawnableResponse = null;
        _reloadPropButton.Disabled = false;
        _deletePropButton.Disabled = false;
        
        // Display metadata immediately with placeholder image
        _contentDisplay.SetContent(metadata);
        
        // Set player pfp on button
        string spawnedBy = _currentSelectedPropData.SpawnedBy;
        if (CVRPlayerManager.Instance.UserIdToPlayerEntity.TryGetValue(spawnedBy, out var playerEntity))
        {
            _selectPlayerButton.ButtonIcon = playerEntity.ApiProfileImageUrl;
            _selectPlayerButton.ButtonText = playerEntity.Username;
        }
        // Check if this is our own pro
        else if (spawnedBy == MetaPort.Instance.ownerId)
        {
            _selectPlayerButton.ButtonIcon = PlayerSetup.Instance.AuthInfo.Image.ToString();
            _selectPlayerButton.ButtonText = PlayerSetup.Instance.AuthInfo.Username;
        }
        // Prop is spawned by a user we cannot see... likely blocked
        else
        {
            _selectPlayerButton.ButtonIcon = string.Empty;
            _selectPlayerButton.ButtonText = "Unknown Player";
        }
        
        // Keep disabled until fetch
        _selectPropButton.ButtonTooltip = "Select the Prop for spawning.";
        _selectPropButton.Disabled = true;
        
        // Fetch image and update display
        CVRTools.Run(async () =>
        {
            try
            {
                var response = await PedestalInfoBatchProcessor.QueuePedestalInfoRequest(
                    PedestalType.Prop, 
                    _currentSelectedPropId,
                    skipDelayIfNotCached: true // Not expecting need for batched response here
                );
                
                _currentSpawnableResponse = response;
                string spawnableImageUrl = ImageCache.QueueProcessImage(response.ImageUrl, fallback: response.ImageUrl);
                bool isPermittedToSpawn = response.IsPublished || response.Permitted;

                RootLogic.Instance.MainThreadQueue.Enqueue(() =>
                {
                    if (_fetchPropDetailsCts.IsCancellationRequested) return;
                    // Update with image URL for crossfade and enable button
                    _contentDisplay.SetContent(metadata, spawnableImageUrl);
                    if (isPermittedToSpawn)
                        _selectPropButton.Disabled = false;
                    else
                        _selectPropButton.ButtonTooltip = "Lacking permission to spawn Prop.";
                });
            }
            catch (Exception ex) when (ex is OperationCanceledException) { }
        }, _fetchPropDetailsCts.Token);
    }
    
    private static void OnOpenDetails()
    {
        ViewManager.Instance.GetPropDetails(_currentSelectedPropId);
    }

    private static void OnSelectProp()
    {
        if (_currentSpawnableResponse != null)
        {
            string imageUrl = ImageCache.QueueProcessImage(_currentSpawnableResponse.ImageUrl, fallback: _currentSpawnableResponse.ImageUrl);
            PlayerSetup.Instance.SelectPropToSpawn(_currentSpawnableResponse.Id, imageUrl, _currentSpawnableResponse.Name);
        }
    }

    private static void OnSelectPlayer()
    {
        string spawnedBy = _currentSelectedPropData.SpawnedBy;
        // Check if this is a remote player and they exist
        if (CVRPlayerManager.Instance.TryGetConnected(spawnedBy))
        {
            QuickMenuAPI.OpenPlayerListByUserID(spawnedBy);
        }
        // Check if this is ourselves
        else if (spawnedBy == MetaPort.Instance.ownerId)
        {
            PlayerList.Instance.OpenPlayerActionPage(PlayerList.Instance._localUserObject);
        }
        // User is not real
        else
        {
            ViewManager.Instance.RequestUserDetailsPage(spawnedBy);
            ViewManager.Instance.TriggerPushNotification("Opened profile page as user was not found in Instance.", 2f);
        }
    }
    
    private static void OnDeleteProp()
    {
        CVRSpawnable spawnable = _currentSelectedPropData.Spawnable;
        if (spawnable && !spawnable.IsSpawnedByAdmin())
        {
            spawnable.Delete();
            
            // Disable now unusable buttons
            _reloadPropButton.Disabled = true;
            _deletePropButton.Disabled = true;
        }
    }

    private static void OnReloadProp()
    {
        CVRSpawnable spawnable = _currentSelectedPropData.Spawnable;
        if (spawnable.IsSpawnedByAdmin())
            return; // Can't call delete
        
        CVRSyncHelper.PropData oldData = _currentSelectedPropData;
        
        // If this prop has not yet fully reloaded do not attempt another reload
        if (!oldData.Spawnable) return;
        
        // Find our cached task in the download manager
        // TODO: Noticed issue, download manager doesn't keep track of cached items properly.
        // We are comparing the asset id instead of instance id because the download manager doesn't cache
        // multiple for the same asset id.
        DownloadTask ourTask = null;
        foreach ((string assetId, DownloadTask downloadTask) in CVRDownloadManager.Instance._cachedTasks)
        {
            if (downloadTask.Type != DownloadTask.ObjectType.Prop 
                || assetId != oldData.ObjectId) continue;
            ourTask = downloadTask;
            break;
        }
        if (ourTask == null) return;
        
        // Create new prop data from the old one
        CVRSyncHelper.PropData newData = CVRSyncHelper.PropData.PropDataPool.GetObject();
        newData.CopyFrom(oldData);
        
        // Prep spawnable for partial delete
        spawnable.SpawnedByMe = false; // Prevent telling GS about delete
        spawnable.instanceId = null; // Prevent OnDestroy from recycling our new prop data later in frame
        
        // Destroy the prop
        oldData.Recycle();
        
        // Add our new prop data to sync helper
        CVRSyncHelper.Props.Add(newData);
        newData.CanFireDecommissionEvents = true;
        
        // Do mega jank
        CVREncryptionRouter router = null;
        string filePath = CacheManager.Instance.GetCachePath(newData.ContentMetadata.AssetId, ourTask.FileId);
        if (!string.IsNullOrEmpty(filePath)) router = new CVREncryptionRouter(filePath, newData.ContentMetadata);
        
        PropQueueSystem.Instance.AddCoroutine(
            CoroutineUtil.RunThrowingIterator(
                CVRObjectLoader.Instance.InstantiateSpawnableFromBundle(
                    newData.ContentMetadata.AssetId, 
                    newData.ContentMetadata.FileHash, 
                    newData.InstanceId,
                    router, 
                    newData.ContentMetadata.TagsData,
                    newData.ContentMetadata.CompatibilityVersion,
                    string.Empty, 
                    newData.SpawnedBy),
                Debug.LogError
            )
        );
        
        // Update backing selected prop data
        _currentSelectedPropData = newData;
    }
}