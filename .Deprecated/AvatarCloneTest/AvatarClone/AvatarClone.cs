using NAK.AvatarCloneTest;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.AvatarCloneTest;

public partial class AvatarClone : MonoBehaviour
{
    #region Constants
    
    private const int LOCAL_LAYER = 8;
    private const int CLONE_LAYER = 9;
    
    #endregion Constants

    #region Profiler Markers
    
#if ENABLE_PROFILER
    private static readonly ProfilerMarker s_CopyEnabledState = new($"{nameof(AvatarClone)}.{nameof(SyncEnabledState)}");
    private static readonly ProfilerMarker s_CopyMaterials = new($"{nameof(AvatarClone)}.{nameof(CopyMaterialsAndProperties)}");
    private static readonly ProfilerMarker s_CopyBlendShapes = new($"{nameof(AvatarClone)}.{nameof(CopyBlendShapes)}");
    private static readonly ProfilerMarker s_InitializeData = new($"{nameof(AvatarClone)}.Initialize");
    private static readonly ProfilerMarker s_UpdateExclusions = new($"{nameof(AvatarClone)}.{nameof(HandleExclusionUpdate)}");
    private static readonly ProfilerMarker s_CollectExclusionData = new($"{nameof(AvatarClone)}.{nameof(CollectExclusionData)}");
    private static readonly ProfilerMarker s_HandleBoneUpdates = new($"{nameof(AvatarClone)}.{nameof(UpdateSkinnedMeshBones)}");
    private static readonly ProfilerMarker s_PreCullUpdate = new($"{nameof(AvatarClone)}.{nameof(MyOnPreCull)}");
    private static readonly ProfilerMarker s_ConfigureShadowCasting = new($"{nameof(AvatarClone)}.{nameof(ConfigureSourceShadowCasting)}");
    private static readonly ProfilerMarker s_ConfigureUICulling = new($"{nameof(AvatarClone)}.{nameof(ConfigureCloneUICulling)}");
    private static readonly ProfilerMarker s_AddRenderer = new($"{nameof(AvatarClone)}.AddRenderer");
    private static readonly ProfilerMarker s_CreateClone = new($"{nameof(AvatarClone)}.CreateClone");
#endif
    
    #endregion Profiler Markers
    
    #region Settings
    
    public bool Setting_CloneMeshRenderers;
    public bool Setting_CopyMaterials = true;
    public bool Setting_CopyBlendShapes = true;
    
    #endregion Settings

    #region Source Collections - Cloned Renderers
    
    // Skinned mesh renderers (always cloned)
    private List<SkinnedMeshRenderer> _skinnedRenderers;
    private List<List<float>> _blendShapeWeights;
    
    // Mesh renderers (optionally cloned)
    private List<MeshRenderer> _meshRenderers;
    private List<MeshFilter> _meshFilters;
    
    #endregion Source Collections - Cloned Renderers
    
    #region Source Collections - Non-Cloned Renderers
    
    // All other renderers (never cloned)
    private List<Renderer> _otherRenderers;
    
    // True if source renderer should hide. False if source renderer should show.
    // Only used for non-cloned renderers (MeshRenderers and other Renderers).
    private bool[] _sourceShouldBeHiddenFromFPR; 
    // Three states: On, ShadowsOnly, Off
    private ShadowCastingMode[] _originalShadowCastingMode;
    
    #endregion Source Collections - Non-Cloned Renderers
    
    #region Clone Collections
    
    // Skinned mesh clones
    private List<SkinnedMeshRenderer> _skinnedClones;
    private List<Material[]> _skinnedCloneMaterials;
    private List<Material[]> _skinnedCloneCullingMaterials;
    
    // Mesh clones (optional)
    private List<MeshRenderer> _meshClones;
    private List<MeshFilter> _meshCloneFilters;
    private List<Material[]> _meshCloneMaterials;
    private List<Material[]> _meshCloneCullingMaterials;
    
    #endregion Clone Collections
    
    #region Shared Resources
    
    private List<Material> _materialWorkingList; // Used for GetSharedMaterials
    private MaterialPropertyBlock _propertyBlock;
    
    #endregion Shared Resources

    #region State
    
    private bool _sourcesSetForShadowCasting;
    private bool _clonesSetForUiCulling;
    private bool[] _rendererActiveStates;
    
    #endregion State
    
    #region Unity Events
    
    private void Start()
    {
        Setting_CloneMeshRenderers = AvatarCloneTestMod.EntryCloneMeshRenderers.Value;
        
        InitializeCollections();
        CollectRenderers();
        CreateClones();
        AddExclusionToHeadIfNeeded();
        InitializeExclusions();
        SetupMagicaClothSupport();

        // bool animatesClone = transform.Find("[ExplicitlyAnimatesVisualClones]") != null;
        // Setting_CopyMaterials = !animatesClone;
        // Setting_CopyBlendShapes = !animatesClone;
        // Animator animator = GetComponent<Animator>();
        // if (animator && animatesClone) animator.Rebind();

        // Likely a Unity bug with where we can touch shadowCastingMode & forceRenderingOff
#if !UNITY_EDITOR
        Camera.onPreCull += MyOnPreCull;
#else 
        Camera.onPreRender += MyOnPreCull;
#endif
    }
    
    private void LateUpdate()
    {
        SyncEnabledState();
        
        if (Setting_CopyMaterials && AvatarCloneTestMod.EntryCopyMaterials.Value)
            SyncMaterials();
        
        if (Setting_CopyBlendShapes && AvatarCloneTestMod.EntryCopyBlendShapes.Value)
            SyncBlendShapes();
    }
    
    private void OnDestroy()
    {
        // Likely a Unity bug with where we can touch shadowCastingMode & forceRenderingOff
#if !UNITY_EDITOR
        Camera.onPreCull -= MyOnPreCull;
#else
        Camera.onPreRender -= MyOnPreCull;
#endif
    }
    
    #endregion Unity Events
}