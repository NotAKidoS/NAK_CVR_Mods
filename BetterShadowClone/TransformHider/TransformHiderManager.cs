﻿using ABI_RC.Core.Player;
using ABI_RC.Systems.VRModeSwitch;
using MagicaCloth;
using UnityEngine;

namespace NAK.BetterShadowClone;

// Built on top of Koneko's BoneHider but to mimic the ShadowCloneManager

public class TransformHiderManager : MonoBehaviour
{
    public static ComputeShader shader;
    
    #region Singleton Implementation

    private static TransformHiderManager _instance;
    public static TransformHiderManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = new GameObject("Koneko.TransformHiderManager").AddComponent<TransformHiderManager>();
            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }

    #endregion
    
    // Game cameras
    private static Camera s_MainCamera;
    private static Camera s_UiCamera;
    
    // Settings
    internal static bool s_DebugHeadHide;
    internal static bool s_DisallowFprExclusions = true;
    
    // Implementation
    private bool _hasRenderedThisFrame;
    
    // Shadow Clones
    private readonly List<ITransformHider> s_TransformHider = new();
    public void AddTransformHider(ITransformHider clone)
        => s_TransformHider.Add(clone);
    
    // Debug
    private bool _debugHeadHiderProcessingTime;
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
        
        s_DisallowFprExclusions = ModSettings.EntryDontRespectFPR.Value;
        s_DebugHeadHide = ModSettings.EntryDebugHeadHide.Value;
        
        VRModeSwitchEvents.OnCompletedVRModeSwitch.AddListener(OnVRModeSwitchCompleted);
    }

    private void OnEnable()
    {
        Camera.onPreRender += MyOnPreRender;
        Camera.onPostRender += MyOnPostRender;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= MyOnPreRender;
        Camera.onPostRender -= MyOnPostRender;
    }

    private void OnDestroy()
    {
        VRModeSwitchEvents.OnCompletedVRModeSwitch.RemoveListener(OnVRModeSwitchCompleted);
        OnAvatarCleared();
    }

    #endregion
    
    #region Transform Hider Managment

    private void Update()
    {
        _hasRenderedThisFrame = false;
    }
    
    private void MyOnPreRender(Camera cam)
    {
        if (_hasRenderedThisFrame) 
            return; // can only hide head once per frame
        
        if (cam != s_MainCamera // only hide in player cam, or if debug is on
            && !s_DebugHeadHide)
            return;
     
        if (!CheckPlayerCamWithinRange())
            return; // player is too far away (likely HoloPort or Sitting)
        
        if (!ShadowCloneMod.CheckWantsToHideHead(cam))
            return; // listener said no (Third Person, etc)
        
        _hasRenderedThisFrame = true;
        
        _stopWatch.Start();
        
        for (int i = s_TransformHider.Count - 1; i >= 0; i--)
        {
            ITransformHider hider = s_TransformHider[i];
            if (hider is not { IsValid: true })
            {
                hider?.Dispose();
                s_TransformHider.RemoveAt(i);
                continue; // invalid or dead
            }
        
            if (!hider.Process()) continue; // not ready yet or disabled

            hider.HideTransform(s_DisallowFprExclusions);
        }
        
        _stopWatch.Stop();
        if (_debugHeadHiderProcessingTime) Debug.Log($"TransformHiderManager.MyOnPreRender({s_DebugHeadHide}) took {_stopWatch.ElapsedMilliseconds}ms");
    }

    private void MyOnPostRender(Camera cam)
    {
        if (cam != s_UiCamera) return; // ui camera is expected to render last
        
        for (int i = s_TransformHider.Count - 1; i >= 0; i--)
        {
            ITransformHider hider = s_TransformHider[i];
            if (hider is not { IsValid: true })
            {
                hider?.Dispose();
                s_TransformHider.RemoveAt(i);
                continue; // invalid or dead
            }
        
            if (!hider.PostProcess()) continue; // does not need post processing

            hider.ShowTransform();
        }
    }

    #endregion

    #region Game Events

    public void OnAvatarCleared()
    {
        // Dispose all shadow clones BEFORE game unloads avatar
        // Otherwise we memory leak the shadow clones mesh & material instances!!!
        foreach (ITransformHider hider in s_TransformHider)
            hider.Dispose();
        s_TransformHider.Clear();
    }

    private void OnVRModeSwitchCompleted(bool _)
        => UpdatePlayerCameras();

    #endregion

    #region Private Methods

    private static void UpdatePlayerCameras()
    {
        s_MainCamera = PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
        s_UiCamera = s_MainCamera.transform.Find("_UICamera").GetComponent<Camera>();
    }

    private static bool CheckPlayerCamWithinRange()
    {
        if (PlayerSetup.Instance == null) 
            return false; // hack
        
        const float MinHeadHidingRange = 0.5f;
        Vector3 playerHeadPos = PlayerSetup.Instance.GetViewWorldPosition();
        Vector3 playerCamPos = s_MainCamera.transform.position;
        float scaleModifier = PlayerSetup.Instance.GetPlaySpaceScale();
        return (Vector3.Distance(playerHeadPos, playerCamPos) < (MinHeadHidingRange * scaleModifier));
    }

    #endregion

    #region Static Helpers
    
    internal static bool IsLegacyFPRExcluded(Component renderer)
        => renderer.gameObject.name.Contains("[FPR]");
    
    internal static ITransformHider CreateTransformHider(Component renderer, IReadOnlyDictionary<Transform, FPRExclusion> exclusions)
    {
        if (IsLegacyFPRExcluded(renderer))
            return null;
        
        return renderer switch
        {
            SkinnedMeshRenderer skinnedMeshRenderer => new SkinnedTransformHider(skinnedMeshRenderer, exclusions),
            MeshRenderer meshRenderer => new MeshTransformHider(meshRenderer, exclusions),
            _ => null
        };
    }

    #endregion
}