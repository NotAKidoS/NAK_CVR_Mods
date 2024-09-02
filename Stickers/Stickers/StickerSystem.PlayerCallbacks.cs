using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.Stickers;

public partial class StickerSystem
{
    #region Player Callbacks
    
    private void OnPlayerJoined(CVRPlayerEntity playerEntity)
    {
        if (!_playerStickers.TryGetValue(playerEntity.Uuid, out StickerData stickerData))
            return;

        stickerData.DeathTime = -1f;
        stickerData.SetAlpha(1f);
    }

    private void OnPlayerLeft(CVRPlayerEntity playerEntity)
    {
        if (!_playerStickers.TryGetValue(playerEntity.Uuid, out StickerData stickerData))
            return;

        stickerData.DeathTime = Time.time + StickerKillTime;
        stickerData.SetAlpha(1f);
    }

    private void OnOccasionalUpdate()
    {
        float currentTime = Time.time;
        for (int i = 0; i < _playerStickers.Values.Count; i++)
        {
            StickerData stickerData = _playerStickers.Values.ElementAt(i);
            
            if (stickerData.DeathTime < 0f)
                continue;

            if (currentTime < stickerData.DeathTime)
            {
                stickerData.SetAlpha(Mathf.Lerp(0f, 1f, (stickerData.DeathTime - currentTime) / StickerKillTime));
                continue;
            }
            
            stickerData.Cleanup();
            _playerStickers.Remove(stickerData.PlayerId);
        }
    }

    #endregion Player Callbacks

    #region Player Callbacks

    public void OnStickerPlaceReceived(string playerId, int stickerSlot, Vector3 position, Vector3 forward, Vector3 up)
        => AttemptPlaceSticker(playerId, position, forward, up, alignWithNormal: true, stickerSlot);

    public void OnStickerClearReceived(string playerId, int stickerSlot)
        => ClearStickersForPlayer(playerId, stickerSlot);
    
    public void OnStickerClearAllReceived(string playerId)
        => ClearStickersForPlayer(playerId);
    
    // public void OnStickerIdentifyReceived(string playerId)
    // {
    //     if (!_playerStickers.TryGetValue(playerId, out StickerData stickerData))
    //         return;
    //     
    //     // todo: make prettier (idk shaders)
    //     SchedulerSystem.AddJob(() => stickerData.Identify(), 0f, 0.1f, 30);
    //     SchedulerSystem.AddJob(() => stickerData.ResetIdentify(), 4f, 1f, 1);
    // }

    #endregion Player Callbacks
}
