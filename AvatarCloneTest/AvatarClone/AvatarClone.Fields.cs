using UnityEngine;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone
{
    #region Profile Markers    
//#if UNITY_EDITOR
    private static readonly UnityEngine.Profiling.CustomSampler s_CopyMaterials =
        UnityEngine.Profiling.CustomSampler.Create("AvatarClone2.CopyMaterials");
    private static readonly UnityEngine.Profiling.CustomSampler s_CopyBlendShapes =
        UnityEngine.Profiling.CustomSampler.Create("AvatarClone2.CopyBlendShapes");
    private static readonly UnityEngine.Profiling.CustomSampler s_CopyMeshes =
        UnityEngine.Profiling.CustomSampler.Create("AvatarClone2.CopyMeshes");
    
    private static readonly UnityEngine.Profiling.CustomSampler s_MyOnPreRender =
        UnityEngine.Profiling.CustomSampler.Create("AvatarClone2.MyOnPreRender");
    private static readonly UnityEngine.Profiling.CustomSampler s_SetShadowsOnly =
        UnityEngine.Profiling.CustomSampler.Create("AvatarClone2.SetShadowsOnly");
    private static readonly UnityEngine.Profiling.CustomSampler s_UndoShadowsOnly =
        UnityEngine.Profiling.CustomSampler.Create("AvatarClone2.UndoShadowsOnly");
    private static readonly UnityEngine.Profiling.CustomSampler s_SetUiCulling =
        UnityEngine.Profiling.CustomSampler.Create("AvatarClone2.SetUiCulling");
    private static readonly UnityEngine.Profiling.CustomSampler s_UndoUiCulling =
        UnityEngine.Profiling.CustomSampler.Create("AvatarClone2.UndoUiCulling");
    
//#endif
    #endregion Profile Markers
    
    #region Source Renderers
    private List<MeshRenderer> _standardRenderers;
    private List<MeshFilter> _standardFilters;
    private List<SkinnedMeshRenderer> _skinnedRenderers;
    private List<Renderer> _allSourceRenderers; // For shadow casting only
    #endregion Source Renderers
    
    #region Clone Renderers
    private List<MeshRenderer> _standardClones;
    private List<MeshFilter> _standardCloneFilters;
    private List<SkinnedMeshRenderer> _skinnedClones;
    #endregion Clone Renderers
    
    #region Dynamic Check Lists
    private List<int> _standardRenderersNeedingChecks; // Stores indices into _standardRenderers
    private List<int> _skinnedRenderersNeedingChecks;  // Stores indices into _skinnedRenderers
    private List<int> _cachedSkinnedBoneCounts; // So we don't copy the bones unless they've changed
    private List<Mesh> _cachedSharedMeshes; // So we don't copy the mesh unless it's changed
    #endregion Dynamic Check Lists
    
    #region Material Data
    private List<Material[]> _localMaterials;
    private List<Material[]> _cullingMaterials;
    private List<Material> _mainMaterials;
    private MaterialPropertyBlock _propertyBlock;
    #endregion Material Data
    
    #region Blend Shape Data
    private List<List<float>> _blendShapeWeights;
    #endregion Blend Shape Data
    
    #region Shadow and UI Culling Settings
    private bool _uiCullingActive;
    private bool _shadowsOnlyActive;
    private bool[] _originallyHadShadows;
    private bool[] _originallyWasEnabled;
    #endregion Shadow and UI Culling Settings
}