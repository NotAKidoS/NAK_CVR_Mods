using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.BetterShadowClone;

public class MeshTransformHider : ITransformHider, IFPRExclusionTask
{
    // lame 2 frame init stuff
    private const int FrameInitCount = 0;
    private int _frameInitCounter;
    private bool _hasInitialized;
    private bool _markedForDeath;
    
    // mesh
    private readonly MeshRenderer _mainMesh;
    private bool _enabledState;
    
    #region ITransformHider Methods
    
    public bool IsActive { get; set; } = true; // default hide, but FPRExclusion can override
    
    // anything player can touch is suspect to death
    public bool IsValid => _mainMesh != null && !_markedForDeath;

    public MeshTransformHider(MeshRenderer renderer, IReadOnlyDictionary<Transform, FPRExclusion> exclusions)
    {
        Transform rootBone = renderer.transform;
        
        // if no key found, dispose
        if (!exclusions.TryGetValue(rootBone, out FPRExclusion exclusion))
        {
            Dispose();
            return;
        }

        exclusion.relatedTasks.Add(this);
        
        _mainMesh = renderer;
        
        if (_mainMesh == null
            || _mainMesh.sharedMaterials == null
            || _mainMesh.sharedMaterials.Length == 0)
        {
            Dispose();
        }
    }
    
    public bool Process()
    {
        bool shouldRender = _mainMesh.enabled && _mainMesh.gameObject.activeInHierarchy;
        
        // GraphicsBuffer becomes stale when mesh is disabled
        if (!shouldRender)
        {
            _frameInitCounter = 0;
            _hasInitialized = false;
            return false;
        }
        
        // Unity is weird, so we need to wait 2 frames before we can get the graphics buffer
        if (_frameInitCounter >= FrameInitCount)
        {
            if (_hasInitialized) 
                return true;
            
            _hasInitialized = true;
            return true;
        }
        
        _frameInitCounter++;
        return false;
    }

    public bool PostProcess()
        => true;

    public void HideTransform(bool forced = false)
    {
        if (!forced && !IsActive) 
            return;
        
        _enabledState = _mainMesh.enabled;
        _mainMesh.enabled = false;
    }
    
    public void ShowTransform()
    {
        _mainMesh.enabled = _enabledState;
    }

    public void Dispose()
    {
        _markedForDeath = true;
    }
    
    #endregion
}