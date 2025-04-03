using System;
using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using ABI.CCK.Components;
using JetBrains.Annotations;
using MagicaCloth;
using NAK.OriginShift.Components;
using NAK.OriginShift.Utility;
using UnityEngine;

namespace NAK.OriginShift;

public class OriginShiftManager : MonoBehaviour
{
    #region Singleton
    
    private static OriginShiftManager _instance;

    public static OriginShiftManager Instance
    {
        get
        {
            if (_instance) return _instance;
            _instance = new GameObject("NAKOriginShiftManager").AddComponent<OriginShiftManager>();
            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }
    
    private static bool _useOriginShift;

    public static bool UseOriginShift
    {
        get => _useOriginShift;
        set
        {
            if (_useOriginShift == value) 
                return;
            
            if (value)
            {
                PlayerSetup.Instance.gameObject.AddComponentIfMissing<OriginShiftMonitor>();
            }
            else
            {
                Instance.ResetOrigin();
                if (PlayerSetup.Instance.TryGetComponent(out OriginShiftMonitor originShiftMonitor))
                    DestroyImmediate(originShiftMonitor);
            }
            
            _useOriginShift = value;
        }
    }

    private static bool _compatibilityMode = true;
    public static bool CompatibilityMode
    {
        get => _useOriginShift && _compatibilityMode;
        set => _compatibilityMode = value;
    }
    
    #endregion Singleton

    #region Shader Globals

    private static readonly int s_OriginShiftChunkOffset = Shader.PropertyToID("_OriginShiftChunkOffset"); // Vector3
    private static readonly int s_OriginShiftChunkPosition = Shader.PropertyToID("_OriginShiftChunkPosition"); // Vector3
    private static readonly int s_OriginShiftChunkThreshold = Shader.PropertyToID("_OriginShiftChunkThreshold"); // float
    
    private static void SetShaderGlobals(Vector3 chunk, float threshold)
    {
        Shader.SetGlobalVector(s_OriginShiftChunkOffset, chunk);
        Shader.SetGlobalFloat(s_OriginShiftChunkThreshold, threshold);
        Shader.SetGlobalVector(s_OriginShiftChunkPosition, chunk * threshold);
    }
    
    private static void ResetShaderGlobals()
    {
        Shader.SetGlobalVector(s_OriginShiftChunkOffset, Vector3.zero);
        Shader.SetGlobalFloat(s_OriginShiftChunkThreshold, -1f);
        Shader.SetGlobalVector(s_OriginShiftChunkPosition, Vector3.zero);
    }
    
    #endregion Shader Globals

    #region Actions

    public static Action<OriginShiftState> OnStateChanged = delegate { };
    
    public static Action<Vector3> OnOriginShifted = delegate { }; // move everything
    public static Action<Vector3> OnPostOriginShifted = delegate { }; // player & chunks

    #endregion Actions

    #region Public Properties
    
    [PublicAPI] public bool IsOriginShifted => ChunkOffset != Vector3Int.zero;
    [PublicAPI] public Vector3Int ChunkOffset { get; internal set; } = Vector3Int.zero;
    [PublicAPI] public Vector3Int ChunkPosition => ChunkOffset * OriginShiftController.ORIGIN_SHIFT_THRESHOLD;
    
    public enum OriginShiftState
    {
        Inactive, // world is not using Origin Shift
        Active, // world is using Origin Shift
        Forced // temp for this session, force world to use Origin Shift
    }
    
    private OriginShiftState _currentState = OriginShiftState.Inactive;
    [PublicAPI] public OriginShiftState CurrentState 
    {
        get => _currentState;
        private set
        {
            if (_currentState == value) 
                return;
            
            _currentState = value;
            OnStateChanged.Invoke(value);
        }
    }
    
    #endregion Public Properties

    #region Manager Lifecycle
    
    private OriginShiftController _forceController;

    public void ForceManager()
    {
        if (CVRWorld.Instance == null)
        {
            OriginShiftMod.Logger.Error("Cannot force Origin Shift without a world.");
            return;
        }
        OriginShiftMod.Logger.Msg("Forcing Origin Shift...");
        
        _forceController = CVRWorld.Instance.gameObject.AddComponentIfMissing<OriginShiftController>();
        _forceController.IsForced = true;
    }

    public void SetupManager(bool isForced = false)
    {
        if (CVRWorld.Instance == null)
        {
            OriginShiftMod.Logger.Error("Cannot setup Origin Shift without a world.");
            return;
        }
        OriginShiftMod.Logger.Msg("Setting up Origin Shift...");
        
        CurrentState = isForced ? OriginShiftState.Forced : OriginShiftState.Active;
        UseOriginShift = true;
    }
    
    public void ResetManager()
    {
        OriginShiftMod.Logger.Msg("Resetting Origin Shift...");
        
        if (_forceController) Destroy(_forceController);
        
        CurrentState = OriginShiftState.Inactive;
        
        ResetOrigin();
        ResetShaderGlobals();
        UseOriginShift = false;
    }

    #endregion Manager Lifecycle

    #region Public Methods
    
    // Called by OriginShiftMonitor when the local player needs to shit
    public void ShiftOrigin(Vector3 rawPosition)
    {
        if (!_useOriginShift) return;
        
        // create stopwatch
        StopWatch stopwatch = new();
        stopwatch.Start();

        // normalize
        float halfThreshold = (OriginShiftController.ORIGIN_SHIFT_THRESHOLD / 2f);
        rawPosition += new Vector3(halfThreshold, halfThreshold, halfThreshold);
        
        // add to chunk
        Vector3Int chunkDifference;
        Vector3Int calculatedChunk = chunkDifference = ChunkOffset;
        
        calculatedChunk.x += Mathf.FloorToInt(rawPosition.x / OriginShiftController.ORIGIN_SHIFT_THRESHOLD);
        calculatedChunk.y += Mathf.FloorToInt(rawPosition.y / OriginShiftController.ORIGIN_SHIFT_THRESHOLD);
        calculatedChunk.z += Mathf.FloorToInt(rawPosition.z / OriginShiftController.ORIGIN_SHIFT_THRESHOLD);

        // get offset
        chunkDifference = (ChunkOffset - calculatedChunk) * OriginShiftController.ORIGIN_SHIFT_THRESHOLD;
        
        // store & invoke
        ChunkOffset = calculatedChunk;
        OnOriginShifted.Invoke(chunkDifference);
        OnPostOriginShifted.Invoke(chunkDifference);
        
        // set shader globals
        SetShaderGlobals(ChunkOffset, OriginShiftController.ORIGIN_SHIFT_THRESHOLD);
        
        // log
        stopwatch.Stop();
        OriginShiftMod.Logger.Msg($"Shifted Origin: {chunkDifference} in {stopwatch.ElapsedMilliseconds:F11}ms");
    }

    public void ResetOrigin()
    {
        if (!_useOriginShift) return;
        
        ShiftOrigin(-ChunkPosition);
        ChunkOffset = Vector3Int.zero;
    }
    
    #endregion Origin Shift Implementation

    #region Utility Methods

    public static Vector3 GetAbsolutePosition(Vector3 localizedPosition)
    {
        // absolute coordinates can be reconstructed using current chunk and threshold
        localizedPosition += Instance.ChunkOffset * OriginShiftController.ORIGIN_SHIFT_THRESHOLD;
        return localizedPosition;
    }
    
    public static Vector3 GetLocalizedPosition(Vector3 absolutePosition)
    {
        return absolutePosition - (Instance.ChunkOffset * OriginShiftController.ORIGIN_SHIFT_THRESHOLD);
    }

    #endregion Utility Methods

    #region Debug Methods

    public void ToggleDebugOverlay(bool state)
    {
        if (TryGetComponent(out DebugTextDisplay debugTextDisplay))
            Destroy(debugTextDisplay);
        else
            gameObject.AddComponent<DebugTextDisplay>();
    }

    #endregion Debug Methods
}