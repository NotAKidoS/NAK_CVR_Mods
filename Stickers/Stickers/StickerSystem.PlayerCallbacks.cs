using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Player;
using ABI_RC.Systems.GameEventSystem;
using UnityEngine;

namespace NAK.Stickers;

public partial class StickerSystem
{
    #region Player Callbacks
    
    private void OnPlayerSetupStart()
    {
        CVRGameEventSystem.World.OnUnload.AddListener(_ => Instance.CleanupAllButSelf());
        CVRGameEventSystem.Instance.OnConnected.AddListener((_) => { if (!Instances.IsReconnecting) Instance.ClearStickersSelf(); });
        
        CVRGameEventSystem.Player.OnJoinEntity.AddListener(Instance.OnPlayerJoined);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(Instance.OnPlayerLeft);
        SchedulerSystem.AddJob(Instance.OnOccasionalUpdate, 10f, 1f);
        LoadAllImagesAtStartup();
    }

    private void OnPlayerJoined(CVRPlayerEntity playerEntity)
    {
        if (!_playerStickers.TryGetValue(playerEntity.Uuid, out StickerData stickerData))
            return;

        stickerData.DeathTime = -1f;
        stickerData.SetAlpha(1f);
        _deadStickerPool.Remove(stickerData);
    }

    private void OnPlayerLeft(CVRPlayerEntity playerEntity)
    {
        if (!_playerStickers.TryGetValue(playerEntity.Uuid, out StickerData stickerData))
            return;

        stickerData.DeathTime = Time.time + StickerKillTime;
        stickerData.SetAlpha(1f);
        _deadStickerPool.Add(stickerData);
    }

    private void OnOccasionalUpdate()
    {
        if (_deadStickerPool.Count == 0) 
            return;

        for (var i = _deadStickerPool.Count - 1; i >= 0; i--)
        {
            float currentTime = Time.time;
            StickerData stickerData = _deadStickerPool[i];
            if (stickerData == null)
            {
                _deadStickerPool.RemoveAt(i);
                continue;
            }
            
            if (stickerData.DeathTime < 0f) 
                continue;

            if (currentTime < stickerData.DeathTime)
            {
                stickerData.SetAlpha(Mathf.Lerp(0f, 1f, (stickerData.DeathTime - currentTime) / StickerKillTime));
                continue;
            }
            
            for (int j = 0; j < _playerStickers.Values.Count; j++)
            {
                if (_playerStickers.Values.ElementAt(j) != stickerData) continue;
                _playerStickers.Remove(_playerStickers.Keys.ElementAt(j));
                break;
            }
            
            _deadStickerPool.RemoveAt(i);
            stickerData.Cleanup();
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
