using System.Collections.Generic;
using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using MagicaCloth;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.BetterShadowClone;

public class ShadowCloneManager : MonoBehaviour
{
    #region Singleton Implementation

    private static ShadowCloneManager _instance;
    public static ShadowCloneManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = new GameObject("NAK.ShadowCloneManager").AddComponent<ShadowCloneManager>();
            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }

    #endregion
    
    private const string ShadowClonePostfix = "_ShadowClone";
    //public const string CVRIgnoreForUiCulling = "CVRIgnoreForUiCulling"; // TODO: Shader Tag to ignore for UI culling?
    
    // Game cameras
    private static Camera s_MainCamera;
    private static Camera s_UiCamera;
    
    // Settings
    internal static bool s_CopyMaterialsToShadow = true;
    internal static bool s_DebugShowShadow = false;
    internal static bool s_DebugShowInFront = false;
    private static bool s_UseShadowToCullUi;
    private const string ShadowCullUiSettingName = "ExperimentalAvatarOverrenderUI";
    
    // Implementation
    private bool _hasRenderedThisFrame;
    public static readonly List<FPRExclusion> s_Exclusions = new();
    
    // Shadow Clones
    private readonly List<IShadowClone> s_ShadowClones = new();
    public void AddShadowClone(IShadowClone clone)
        => s_ShadowClones.Add(clone);
    
    // Debug
    private bool _debugShadowProcessingTime;
    private readonly StopWatch _stopWatch = new();
    
    #region Unity Events

    private void Start()
    {
        if (Instance != null
            && Instance != this)
        {
            Destroy(this);
            return;
        }
        
        UpdatePlayerCameras();
        
        s_CopyMaterialsToShadow = ModSettings.EntryCopyMaterialToShadow.Value;
        s_DebugShowShadow = ModSettings.EntryDebugShowShadow.Value;
        s_DebugShowInFront = ModSettings.EntryDebugShowInFront.Value;
        s_UseShadowToCullUi = MetaPort.Instance.settings.GetSettingsBool(ShadowCullUiSettingName);
        MetaPort.Instance.settings.settingBoolChanged.AddListener(OnSettingsBoolChanged);
    }
    
    private void OnEnable()
        => Camera.onPreRender += MyOnPreCull;
    
    private void OnDisable()
        => Camera.onPreRender -= MyOnPreCull;

    private void OnDestroy()
    {
        MetaPort.Instance.settings.settingBoolChanged.RemoveListener(OnSettingsBoolChanged);
    }
    
    #endregion
    
    #region Shadow Clone Managment

    private void Update()
    {
        _hasRenderedThisFrame = false;
        
        for (int i = s_ShadowClones.Count - 1; i >= 0; i--)
        {
            IShadowClone clone = s_ShadowClones[i];
            if (clone is not { IsValid: true })
            {
                clone?.Dispose();
                s_ShadowClones.RemoveAt(i);
                continue; // invalid or dead
            }
            
            clone.ResetMainMesh();
        }
    }
    
    private void MyOnPreCull(Camera cam)
    {
        //bool forceRenderForUiCull = s_UseShadowToCullUi && cam == s_UiCamera;
        if (cam != s_MainCamera)
            return;
    
        _hasRenderedThisFrame = true;
    
        _stopWatch.Start();
        
        for (int i = s_ShadowClones.Count - 1; i >= 0; i--)
        {
            IShadowClone clone = s_ShadowClones[i];
            if (clone is not { IsValid: true })
            {
                clone?.Dispose();
                s_ShadowClones.RemoveAt(i);
                continue; // invalid or dead
            }
        
            if (!clone.Process()) continue; // not ready yet or disabled
            
            clone.RenderForShadow(); // first cam to render
        }
        
        _stopWatch.Stop();
    }
    
    #endregion

    #region Game Events

    public void OnAvatarCleared()
    {
        // Dispose all shadow clones BEFORE game unloads avatar
        // Otherwise we memory leak the shadow clones mesh & material instances!!!
        foreach (IShadowClone clone in s_ShadowClones)
            clone.Dispose();
        s_ShadowClones.Clear();
    }
    
    private void OnSettingsBoolChanged(string settingName, bool settingValue)
    {
        if (settingName == ShadowCullUiSettingName)
        {
            s_UseShadowToCullUi = settingValue;
            s_UiCamera.cullingMask = settingValue // make UI camera not see CVRLayers.PlayerClone
                ? s_UiCamera.cullingMask | (1 << CVRLayers.PlayerClone)
                : s_UiCamera.cullingMask & ~(1 << CVRLayers.PlayerClone);
        }
    }

    private void OnVRModeSwitchCompleted(bool _, Camera __)
    {
        UpdatePlayerCameras();
    }

    #endregion

    #region Private Methods

    private static void UpdatePlayerCameras()
    {
        s_MainCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
        s_UiCamera = s_MainCamera.transform.Find("_UICamera").GetComponent<Camera>();
        
        //s_PortableCamera = PortableCamera.Instance.cameraComponent;
    }

    #endregion

    #region Static Helpers
    
    internal static IShadowClone CreateShadowClone(Renderer renderer, FPRExclusion exclusion)
    {
        return renderer switch
        {
            SkinnedMeshRenderer skinnedMeshRenderer => new SkinnedShadowClone(skinnedMeshRenderer, exclusion),
            MeshRenderer meshRenderer => new MeshShadowClone(meshRenderer),
            _ => null
        };
    }
    
    internal static (MeshRenderer, MeshFilter) InstantiateShadowClone(SkinnedMeshRenderer meshRenderer)
    {
        GameObject shadowClone = new (meshRenderer.name + ShadowClonePostfix) { layer = CVRLayers.PlayerClone };
        shadowClone.transform.SetParent(meshRenderer.transform, false);
        shadowClone.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        shadowClone.transform.localScale = Vector3.one;

        if (s_DebugShowShadow && s_DebugShowInFront)
        {
            float scale = PlayerSetup.Instance.GetPlaySpaceScale();
            Transform playerTransform = PlayerSetup.Instance.transform;
            shadowClone.transform.position += playerTransform.forward * scale * 1f;
            shadowClone.transform.rotation = Quaternion.AngleAxis(180f, playerTransform.up) * shadowClone.transform.rotation;
        }
        
        MeshRenderer newMesh = shadowClone.AddComponent<MeshRenderer>();
        MeshFilter newMeshFilter = shadowClone.AddComponent<MeshFilter>();

        ShadowCloneHelper.ConfigureRenderer(newMesh, true);
        
        // only shadow clone should cast shadows
        newMesh.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        
        // copy mesh and materials
        newMeshFilter.sharedMesh = meshRenderer.sharedMesh;
        newMesh.sharedMaterials = meshRenderer.sharedMaterials;

        // copy probe anchor
        newMesh.probeAnchor = meshRenderer.probeAnchor;
        
        return (newMesh, newMeshFilter);
    }
    
    internal static (MeshRenderer, MeshFilter) InstantiateShadowClone(MeshRenderer meshRenderer)
    {
        GameObject shadowClone = new (meshRenderer.name + ShadowClonePostfix) { layer = CVRLayers.PlayerClone };
        shadowClone.transform.SetParent(meshRenderer.transform, false);
        shadowClone.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        shadowClone.transform.localScale = Vector3.one;
        
        MeshRenderer newMesh = shadowClone.AddComponent<MeshRenderer>();
        MeshFilter newMeshFilter = shadowClone.AddComponent<MeshFilter>();

        ShadowCloneHelper.ConfigureRenderer(newMesh, true);
        
        // only shadow clone should cast shadows
        newMesh.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        
        // copy mesh and materials
        newMeshFilter.sharedMesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
        newMesh.sharedMaterials = meshRenderer.sharedMaterials;
        
        // copy probe anchor
        newMesh.probeAnchor = meshRenderer.probeAnchor;

        return (newMesh, newMeshFilter);
    }
    
    #endregion
}