using UnityEngine;
using UnityEngine.Animations;

namespace NAK.OriginShift.Components;

public class ChunkController : MonoBehaviour
{
    // manage all chunks (all axis, likely 10kx10k10)
    // keep track of nearby chunks to player (these are "active" chunks)
    // when origin shift occurs, need to mark flag to go through and disable/enable chunks again
    // allow 1ms each frame to be spent on updating chunks, until work is completely done
    // when a chunk is enabled/disabled, a callback is sent to the chunk to update its state

    #region Serialized Fields

    [SerializeField, NotKeyable] private int _maxChunkDistance = 10;

    #endregion Serialized Fields
    
    #region Chunk Class

    public class Chunk
    {
        public Vector3Int Position { get; }

        public bool IsActive { get; private set; }
        
        public void SetActive(bool active)
        {
            IsActive = active;
            if (active)
                OnChunkLoad?.Invoke();
            else
                OnChunkUnload?.Invoke();
        }
        
        public Chunk(Vector3Int position, Action onChunkLoad, Action onChunkUnload)
        {
            Position = position;
            OnChunkLoad = onChunkLoad;
            OnChunkUnload = onChunkUnload;
        }

        private readonly Action OnChunkLoad;
        private readonly Action OnChunkUnload;
    }

    #endregion Chunk Class
    
    public static Dictionary<Vector3Int, Chunk> Chunks { get; private set; } = new();
    
    public static void AddChunk(Chunk chunk)
        => Chunks.Add(chunk.Position, chunk);
    
    public static void RemoveChunk(Chunk chunk)
        => Chunks.Remove(chunk.Position);
    
    private Chunk[,,] _loadedChunks;
    private int _halfChunkDistance;
    
    private int _originX = int.MaxValue;
    private int _originY = int.MaxValue;
    private int _originZ = int.MaxValue;
    
    #region Unity Events

    private void Start()
    {
        _maxChunkDistance = Mathf.Max(1, _maxChunkDistance);
        _halfChunkDistance = _maxChunkDistance / 2;
        _loadedChunks = new Chunk[_maxChunkDistance, _maxChunkDistance, _maxChunkDistance];
        UpdateMap(0, 0, 0); // initial load
        
        OriginShiftManager.OnPostOriginShifted += OnOriginShift;
    }
    
    private void OnDestroy()
    {
        OriginShiftManager.OnPostOriginShifted -= OnOriginShift;
    }
    
    private void UpdateMap(int xPos, int yPos, int zPos) {
        int deltaX = xPos - _originX;
        int deltaY = yPos - _originY;
        int deltaZ = zPos - _originZ;
        
        _originX = xPos;
        _originY = yPos;
        _originZ = zPos;
        
        for (int x = 0; x < _maxChunkDistance; x++) {
            for (int y = 0; y < _maxChunkDistance; y++) {
                for (int z = 0; z < _maxChunkDistance; z++) {
                    int offsetX = x + deltaX;
                    int offsetY = y + deltaY;
                    int offsetZ = z + deltaZ;
                    
                    if (offsetX < 0 || offsetX >= _maxChunkDistance || offsetY < 0 || offsetY >= _maxChunkDistance
                        || offsetZ < 0 || offsetZ >= _maxChunkDistance) {
                        int tileX = x + xPos;
                        int tileY = y + yPos;
                        int tileZ = z + zPos;
                        
                        int mapX = tileX % _maxChunkDistance;
                        int mapY = tileY % _maxChunkDistance;
                        int mapZ = tileZ % _maxChunkDistance;
                        
                        if (mapX < 0) mapX += _maxChunkDistance;
                        if (mapY < 0) mapY += _maxChunkDistance;
                        if (mapZ < 0) mapZ += _maxChunkDistance;
                        
                        // call OnChunkUnload on chunk
                        _loadedChunks[mapX, mapY, mapZ]?.SetActive(false);
                        
                        // access new chunk
                        tileX -= _halfChunkDistance;
                        tileY -= _halfChunkDistance;
                        tileZ -= _halfChunkDistance;
                        
                        // create Vector3Int for lookup
                        Vector3Int newChunkPosition = new(tileX, tileY, tileZ);
                        if (Chunks.TryGetValue(newChunkPosition, out Chunk chunk)) 
                        {
                            chunk.SetActive(true);
                            _loadedChunks[mapX, mapY, mapZ] = chunk;
                        }
                    }
                }
            }
        }
    }

    #endregion Unity Events

    #region Origin Shift Events

    private void OnOriginShift(Vector3 _)
    {
        Vector3Int currentChunk = OriginShiftManager.Instance.ChunkOffset;
        UpdateMap(currentChunk.x, currentChunk.y, currentChunk.z);
    }

    #endregion Origin Shift Events
}