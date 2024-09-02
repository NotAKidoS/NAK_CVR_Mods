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

    private void OnUpdate()
    {
        float currentTime = Time.time;
        for (int i = 0; i < _playerStickers.Values.Count; i++)
        {
            StickerData stickerData = _playerStickers.Values.ElementAt(i);

            if (stickerData.DeathTime > 0f)
            {
                if (currentTime < stickerData.DeathTime)
                {
                    stickerData.SetAlpha(Mathf.Lerp(0f, 1f, (stickerData.DeathTime - currentTime) / StickerKillTime));
                    continue;
                }

                stickerData.Cleanup();
                _playerStickers.Remove(stickerData.PlayerId);
                continue;
            }

            if (stickerData.IdentifyTime > 0)
            {
                if (currentTime < stickerData.IdentifyTime)
                {
                    // blink alpha 3 times but not completely off
                    stickerData.SetAlpha(Mathf.Lerp(0.2f, 1f, Mathf.PingPong((stickerData.IdentifyTime - currentTime) * 2f, 1f)));
                    continue;
                }

                stickerData.SetAlpha(1f);
                stickerData.IdentifyTime = -1;
            }
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
    
    public void OnStickerIdentifyReceived(string playerId)
    {
        if (!_playerStickers.TryGetValue(playerId, out StickerData stickerData))
            return;
        
        stickerData.IdentifyTime = Time.time + 3f;
    }

    #endregion Player Callbacks
}
