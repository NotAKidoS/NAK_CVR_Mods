using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Systems.InputManagement;
using NAK.Stickers.Networking;
using UnityEngine;

namespace NAK.Stickers;

public partial class StickerSystem
{
    #region Sticker Lifecycle
    
    private StickerData GetOrCreateStickerData(string playerId)
    {
        if (_playerStickers.TryGetValue(playerId, out StickerData stickerData)) 
            return stickerData;
        
        stickerData = new StickerData(playerId, ModSettings.MaxStickerSlots);
        _playerStickers[playerId] = stickerData;
        return stickerData;
    }

    public void PlaceStickerFromControllerRay(Transform transform, CVRHand hand = CVRHand.Left, bool isPreview = false)
    {
        Vector3 controllerForward = transform.forward;
        Vector3 controllerUp = transform.up;
        Vector3 playerUp = PlayerSetup.Instance.transform.up;
        
        // extracting angle of controller ray on forward axis
        Vector3 projectedControllerUp = Vector3.ProjectOnPlane(controllerUp, controllerForward).normalized;
        Vector3 projectedPlayerUp = Vector3.ProjectOnPlane(playerUp, controllerForward).normalized;
        float angle = Vector3.Angle(projectedControllerUp, projectedPlayerUp);
        
        float angleThreshold = ModSettings.Entry_PlayerUpAlignmentThreshold.Value;
        Vector3 targetUp = (angleThreshold != 0f && angle <= angleThreshold) 
            // leave 0.01% of the controller up vector to prevent issues with alignment on floor & ceiling in Desktop
            ? Vector3.Slerp(controllerUp, playerUp, 0.99f) 
            : controllerUp;
        
        if (isPreview)
        {
            PlaceStickerPreview(transform.position, controllerForward, targetUp);
            return;
        }
        
        if (!PlaceStickerSelf(transform.position, transform.forward, targetUp))
            return;
        
        CVRInputManager.Instance.Vibrate(0f, 0.1f, 10f, 0.1f, hand);
    }

    private bool PlaceStickerSelf(Vector3 position, Vector3 forward, Vector3 up, bool alignWithNormal = true)
    {
        if (!AttemptPlaceSticker(PlayerLocalId, position, forward, up, alignWithNormal, SelectedStickerSlot))
            return false; // failed
        
        // placed, now network
        ModNetwork.SendPlaceSticker(SelectedStickerSlot, position, forward, up);
        return true;
    }
    
    private bool AttemptPlaceSticker(string playerId, Vector3 position, Vector3 forward, Vector3 up, bool alignWithNormal = true, int stickerSlot = 0, bool isPreview = false)
    {
        // if the world contained a gameobject with the [DisableStickers] name and restricted the instance disable stickers!
        if (IsRestrictedInstance)
            return false;
        
        StickerData stickerData = GetOrCreateStickerData(playerId);
        if (Time.time - stickerData.LastPlacedTime < StickerCooldown)
            return false;

        // Every layer other than IgnoreRaycast, PlayerLocal, PlayerClone, PlayerNetwork, and UI Internal
        const int LayerMask = ~((1 << 2) | (1 << 8) | (1 << 9) | (1 << 10) | (1 << 15));
        if (!Physics.Raycast(position, forward, out RaycastHit hit, 
                10f, LayerMask, QueryTriggerInteraction.Ignore)) 
            return false;
        
        // if gameobject name starts with [NoSticker] then don't place sticker
        if (hit.transform.gameObject.name.StartsWith("[NoSticker]"))
            return false;

        if (isPreview)
        {
            stickerData.PlacePreview(hit, alignWithNormal ? -hit.normal : forward, up, stickerSlot);
            return true;
        }
        
        stickerData.Place(hit, alignWithNormal ? -hit.normal : forward, up, stickerSlot);
        stickerData.PlayAudio();
        return true;
    }

    public void ClearStickersSelf()
    {
        ClearStickersForPlayer(PlayerLocalId);
        ModNetwork.SendClearAllStickers();
    }
    
    public void ClearStickerSelf(int stickerSlot)
    {
        ClearStickersForPlayer(PlayerLocalId, stickerSlot);
        ModNetwork.SendClearSticker(stickerSlot);
    }
    
    private void ClearStickersForPlayer(string playerId)
    {
        if (!_playerStickers.TryGetValue(playerId, out StickerData stickerData)) 
            return;
        
        stickerData.Clear();
    }
    
    public void ClearStickersForPlayer(string playerId, int stickerSlot)
    {
        if (!_playerStickers.TryGetValue(playerId, out StickerData stickerData)) 
            return;
        
        stickerData.Clear(stickerSlot);
    }
    
    private void SetTextureSelf(byte[] imageBytes, int stickerSlot = 0)
    {
        Texture2D texture = new(1, 1); // placeholder
        texture.LoadImage(imageBytes);
        texture.Compress(true); // noachi said to do
        
        ClearStickerSelf(stickerSlot); // clear placed stickers in-scene so we can't replace an entire wall at once
        OnPlayerStickerTextureReceived(PlayerLocalId, Guid.Empty, texture, stickerSlot);
        ModNetwork.SetTexture(stickerSlot, imageBytes);
    }

    public void ClearAllStickers()
    {
        foreach (StickerData stickerData in _playerStickers.Values)
            stickerData.Clear();
        
        ModNetwork.SendClearAllStickers();
    }

    public void OnPlayerStickerTextureReceived(string playerId, Guid textureHash, Texture2D texture, int stickerSlot = 0)
    {
        StickerData stickerData = GetOrCreateStickerData(playerId);
        stickerData.SetTexture(textureHash, texture, stickerSlot);
    }
    
    public bool HasTextureHash(string playerId, Guid textureHash)
    {
        StickerData stickerData = GetOrCreateStickerData(playerId);
        return stickerData.CheckHasTextureHash(textureHash);
    }

    public void CleanupAll()
    {
        foreach ((_, StickerData data) in _playerStickers)
            data.Cleanup();
        
        _playerStickers.Clear();
    }

    public void CleanupAllButSelf()
    {
        StickerData localStickerData = GetOrCreateStickerData(PlayerLocalId);
        
        foreach ((_, StickerData data) in _playerStickers)
        {
            if (data == localStickerData) data.Clear();
            else data.Cleanup();
        }
        
        _playerStickers.Clear();
        _playerStickers[PlayerLocalId] = localStickerData;
    }
    
    public void PlaceStickerPreview(Vector3 position, Vector3 forward, Vector3 up)
    {
        AttemptPlaceSticker(PlayerLocalId, position, forward, up, true, SelectedStickerSlot, true);
    }

    public void UpdateStickerPreview()
    {
        if (!IsInStickerMode) return;
        
        StickerData localStickerData = GetOrCreateStickerData(PlayerLocalId);
        localStickerData.ClearPreview(); // clear prior frames sticker preview
        localStickerData.UpdatePreview(SelectedStickerSlot);
    }
    
    public void ClearStickerPreview()
    {
        StickerData localStickerData = GetOrCreateStickerData(PlayerLocalId);
        localStickerData.ClearPreview();
    }

    #endregion Sticker Lifecycle
}
