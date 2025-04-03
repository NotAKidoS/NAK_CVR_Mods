using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Render State Management
    
    private void MyOnPreCull(Camera cam)
    {
#if UNITY_EDITOR
        // Scene & Preview cameras are not needed
        if (cam.cameraType != CameraType.Game) 
            return;
#endif
        
#if ENABLE_PROFILER
        s_PreCullUpdate.Begin();
#endif
        
        bool isOurUiCamera = IsUIInternalCamera(cam);
        bool rendersOurPlayerLayer = CameraRendersPlayerLocalLayer(cam);
        bool rendersOurCloneLayer = CameraRendersPlayerCloneLayer(cam);
        
        bool rendersBothPlayerLayers = rendersOurPlayerLayer && rendersOurCloneLayer;
        
        // Handle shadow casting when camera renders both layers
        if (!_sourcesSetForShadowCasting 
            && rendersBothPlayerLayers)
        {
            ConfigureSourceShadowCasting(true);
            _sourcesSetForShadowCasting = true;
        }
        else if (_sourcesSetForShadowCasting && !rendersBothPlayerLayers)
        {
            ConfigureSourceShadowCasting(false);
            _sourcesSetForShadowCasting = false;
        }
        
        // Handle UI culling for clone layer
        if (!_clonesSetForUiCulling 
            && isOurUiCamera && rendersOurCloneLayer)
        {
            ConfigureCloneUICulling(true);
            _clonesSetForUiCulling = true;
        }
        else if (_clonesSetForUiCulling)
        {
            ConfigureCloneUICulling(false);
            _clonesSetForUiCulling = false;
        }
        
#if ENABLE_PROFILER
        s_PreCullUpdate.End();
#endif
    }

    private void ConfigureSourceShadowCasting(bool setSourcesToShadowCast)
    {
#if ENABLE_PROFILER
        s_ConfigureShadowCasting.Begin();
#endif
        
        int currentIndex = 0;
        int shadowArrayIndex = 0;
        
        // Handle skinned mesh renderers (always have clones)
        int skinnedCount = _skinnedRenderers.Count;
        for (int i = 0; i < skinnedCount; i++, currentIndex++)
        {
            if (!_rendererActiveStates[currentIndex]) continue;
            
            SkinnedMeshRenderer source = _skinnedRenderers[i];
            
            if (setSourcesToShadowCast)
            {
                ShadowCastingMode originalMode = _originalShadowCastingMode[currentIndex] = source.shadowCastingMode;
                if (originalMode == ShadowCastingMode.Off)
                    source.forceRenderingOff = true;
                else
                    source.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                source.shadowCastingMode = _originalShadowCastingMode[currentIndex];
                source.forceRenderingOff = false;
            }
        }
        
        // Handle mesh renderers based on clone setting
        if (Setting_CloneMeshRenderers)
        {
            int meshCount = _meshRenderers.Count;
            for (int i = 0; i < meshCount; i++, currentIndex++)
            {
                if (!_rendererActiveStates[currentIndex]) continue;                
                
                MeshRenderer source = _meshRenderers[i];
                
                if (setSourcesToShadowCast)
                {
                    ShadowCastingMode originalMode = _originalShadowCastingMode[currentIndex] = source.shadowCastingMode;
                    if (originalMode == ShadowCastingMode.Off)
                        source.forceRenderingOff = true;
                    else
                        source.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                else
                {
                    source.shadowCastingMode = _originalShadowCastingMode[currentIndex];
                    source.forceRenderingOff = false;
                }
            }
        }
        else
        {
            // When not cloned, mesh renderers use the shadow casting array
            int meshCount = _meshRenderers.Count;
            for (int i = 0; i < meshCount; i++, shadowArrayIndex++, currentIndex++)
            {
                if (!_rendererActiveStates[currentIndex]) continue;
                if (!_sourceShouldBeHiddenFromFPR[shadowArrayIndex]) continue;
                
                MeshRenderer source = _meshRenderers[i];

                if (setSourcesToShadowCast)
                {
                    ShadowCastingMode originalMode = _originalShadowCastingMode[currentIndex] = source.shadowCastingMode;
                    if (originalMode == ShadowCastingMode.Off)
                        source.forceRenderingOff = true;
                    else
                        source.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                else
                {
                    source.shadowCastingMode = _originalShadowCastingMode[currentIndex];
                    source.forceRenderingOff = false;
                }
            }
        }
        
        // Handle other renderers (never cloned)
        int otherCount = _otherRenderers.Count;
        for (int i = 0; i < otherCount; i++, shadowArrayIndex++, currentIndex++)
        {
            if (!_rendererActiveStates[currentIndex]) continue;
            if (!_sourceShouldBeHiddenFromFPR[shadowArrayIndex]) continue;
            
            Renderer source = _otherRenderers[i];
            
            if (setSourcesToShadowCast)
            {
                ShadowCastingMode originalMode = _originalShadowCastingMode[currentIndex] = source.shadowCastingMode;
                if (originalMode == ShadowCastingMode.Off)
                    source.forceRenderingOff = true;
                else
                    source.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                source.shadowCastingMode = _originalShadowCastingMode[currentIndex];
                source.forceRenderingOff = false;
            }
        }
        
#if ENABLE_PROFILER
        s_ConfigureShadowCasting.End();
#endif
    }
    
    private void ConfigureCloneUICulling(bool enableCulling)
    {
#if ENABLE_PROFILER
        s_ConfigureUICulling.Begin();
#endif
        
        // Set the materials to our culling materials
        int currentIndex = 0;
        
        int skinnedCount = _skinnedRenderers.Count;
        for (int i = 0; i < skinnedCount; i++, currentIndex++)
        {
            if (!_rendererActiveStates[currentIndex])
                continue;
            
            _skinnedClones[i].sharedMaterials = enableCulling ? 
                _skinnedCloneCullingMaterials[i] : 
                _skinnedCloneMaterials[i];
        }
        
        if (Setting_CloneMeshRenderers)
        {
            int meshCount = _meshRenderers.Count;
            for (int i = 0; i < meshCount; i++, currentIndex++)
            {
                if (!_rendererActiveStates[currentIndex])
                    continue;
                
                _meshClones[i].sharedMaterials = enableCulling ? 
                    _meshCloneCullingMaterials[i] : 
                    _meshCloneMaterials[i];
            }
        }
        
#if ENABLE_PROFILER
        s_ConfigureUICulling.End();
#endif
    }
    
    #endregion Render State Management
}