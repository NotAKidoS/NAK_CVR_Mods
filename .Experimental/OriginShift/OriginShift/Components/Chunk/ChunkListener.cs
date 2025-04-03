using UnityEngine;
using UnityEngine.Events;

namespace NAK.OriginShift.Components;

public class ChunkListener : MonoBehaviour
{
    #region Serialized Properties

    [SerializeField] private Vector3Int _chunkCoords;
    [SerializeField] private UnityEvent _onChunkLoad;
    [SerializeField] private UnityEvent _onChunkUnload;

    #endregion Serialized Properties

    #region Private Variables

    private ChunkController.Chunk _chunk;

    #endregion Private Variables
    
    #region Unity Events
    
    private void Awake()
    {
        _chunk = new ChunkController.Chunk(_chunkCoords, OnChunkLoad, OnChunkUnload);
        ChunkController.AddChunk(_chunk);
    }
    
    private void OnDestroy()
    {
        ChunkController.RemoveChunk(_chunk);
    }
    
    #endregion Unity Events

    #region Chunk Events

    private void OnChunkLoad()
    {
        Vector3Int chunkAbsPos = _chunk.Position * OriginShiftController.ORIGIN_SHIFT_THRESHOLD;
        transform.position = OriginShiftManager.GetLocalizedPosition(chunkAbsPos);
        _onChunkLoad?.Invoke();
    }
    
    private void OnChunkUnload()
    {
        _onChunkUnload?.Invoke();
    }

    #endregion Chunk Events
}