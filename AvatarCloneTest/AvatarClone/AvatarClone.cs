using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone : MonoBehaviour
{
    #region Unity Events
    
    private void Start()
    {
        InitializeCollections();
        InitializeRenderers();
        SetupMaterialsAndBlendShapes();
        CreateClones();
        
        InitializeExclusions();
        SetupMagicaClothSupport();

        Camera.onPreCull += MyOnPreRender;
    }

    private void OnDestroy()
    {
        Camera.onPreCull -= MyOnPreRender;
    }

    private void LateUpdate()
    {
        // Update all renderers with basic properties (Materials & BlendShapes)
        UpdateStandardRenderers();
        UpdateSkinnedRenderers();
        
        // Additional pass for renderers needing extra checks (Shared Mesh & Bone Changes)
        UpdateStandardRenderersWithChecks();
        UpdateSkinnedRenderersWithChecks();
    }
    
    private void MyOnPreRender(Camera cam)
    {
        s_MyOnPreRender.Begin();
        
        bool isOurUiCamera = IsUIInternalCamera(cam);
        bool rendersOurPlayerLayer = CameraRendersPlayerLocalLayer(cam);
        bool rendersOurCloneLayer = CameraRendersPlayerCloneLayer(cam);
        
        // Renders both player layers.
        // PlayerLocal will now act as a shadow caster, while PlayerClone will act as the actual head-hidden renderer.
        bool rendersBothPlayerLayers = rendersOurPlayerLayer && rendersOurCloneLayer;
        if (!_shadowsOnlyActive && rendersBothPlayerLayers)
        {
            s_SetShadowsOnly.Begin();

            int sourceCount = _allSourceRenderers.Count;
            var sourceRenderers = _allSourceRenderers;
            for (int i = 0; i < sourceCount; i++)
            {
                Renderer renderer = sourceRenderers[i];
                if (!IsRendererValid(renderer)) continue;
                
                bool shouldRender = renderer.shadowCastingMode != ShadowCastingMode.Off;
                _originallyWasEnabled[i] = renderer.enabled;
                _originallyHadShadows[i] = shouldRender;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                if (renderer.forceRenderingOff == shouldRender) renderer.forceRenderingOff = !shouldRender; // TODO: Eval if check is needed
            }
            _shadowsOnlyActive = true;

            s_SetShadowsOnly.End();
        }
        else if (_shadowsOnlyActive && !rendersBothPlayerLayers)
        {
            s_UndoShadowsOnly.Begin();

            int sourceCount = _allSourceRenderers.Count;
            var sourceRenderers = _allSourceRenderers;
            for (int i = 0; i < sourceCount; i++)
            {
                Renderer renderer = sourceRenderers[i];
                if (!IsRendererValid(renderer)) continue;
                
                renderer.shadowCastingMode = _originallyHadShadows[i] ? ShadowCastingMode.On : ShadowCastingMode.Off;
                if (renderer.forceRenderingOff == _originallyWasEnabled[i]) renderer.forceRenderingOff = !_originallyWasEnabled[i]; // TODO: Eval if check is needed
            }
            _shadowsOnlyActive = false;

            s_UndoShadowsOnly.End();
        }
        
        // Handle UI culling material changes
        if (isOurUiCamera && !_uiCullingActive && rendersOurCloneLayer)
        {
            s_SetUiCulling.Begin();

            int standardCount = _standardRenderers.Count;
            var standardClones = _standardClones;
            var cullingMaterials = _cullingMaterials;
            for (int i = 0; i < standardCount; i++) 
                standardClones[i].sharedMaterials = cullingMaterials[i];
            
            int skinnedCount = _skinnedRenderers.Count;
            var skinnedClones = _skinnedClones;
            for (int i = 0; i < skinnedCount; i++)
                skinnedClones[i].sharedMaterials = cullingMaterials[i + standardCount];
            
            _uiCullingActive = true;

            s_SetUiCulling.End();
        }
        else if (!isOurUiCamera && _uiCullingActive)
        {
            s_UndoUiCulling.Begin();

            int standardCount = _standardRenderers.Count;
            var standardClones = _standardClones;
            var localMaterials = _localMaterials;
            for (int i = 0; i < standardCount; i++) 
                standardClones[i].sharedMaterials = localMaterials[i];
            
            int skinnedCount = _skinnedRenderers.Count;
            var skinnedClones = _skinnedClones;
            for (int i = 0; i < skinnedCount; i++)
                skinnedClones[i].sharedMaterials = localMaterials[i + standardCount];
            
            _uiCullingActive = false;

            s_UndoUiCulling.End();
        }
        
        s_MyOnPreRender.End();
    }

    #endregion Unity Events
}