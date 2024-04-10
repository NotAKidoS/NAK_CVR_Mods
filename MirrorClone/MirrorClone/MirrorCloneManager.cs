using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using ABI.CCK.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.BetterShadowClone;

public class MirrorCloneManager : MonoBehaviour
{
    #region Static Instance

    public static MirrorCloneManager Instance { get; private set; }

    #endregion
    
    private bool _isAvatarConfigured;

    private GameObject _avatar;
    private GameObject _mirrorClone;
    private GameObject _initializationTarget;
    
    private CVRAnimatorManager _animatorManager;
    private Animator _mirrorAnimator;
    
    #region Unity Events
    
    private void Awake()
    {
        if (Instance != null
            && Instance != this)
        {
            DestroyImmediate(this);
            return;
        }
        
        Instance = this;
        
        MirrorCloneMod.Logger.Msg("Mirror Clone Manager initialized.");

        _animatorManager = PlayerSetup.Instance.animatorManager;
        
        // Create initialization target (so no components are initialized before we're ready)
        _initializationTarget = new GameObject(nameof(MirrorCloneManager) + " Initialization Target");
        _initializationTarget.transform.SetParent(transform);
        _initializationTarget.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        _initializationTarget.transform.localScale = Vector3.one;
        _initializationTarget.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    #endregion

    #region Game Events

    public static void OnPlayerSetupAwake()
    {
        if (Instance != null)
            return;
        
        GameObject manager = new (nameof(MirrorCloneManager), typeof(MirrorCloneManager));
        DontDestroyOnLoad(manager);
    }

    public void OnAvatarInitialized(GameObject avatar)
    {
        if (!ModSettings.EntryEnabled.Value)
            return;
        
        if (avatar == null 
            || _isAvatarConfigured)
            return;
        
        _isAvatarConfigured = true;
        
        _avatar = avatar;
        _mirrorClone = InstantiateMirrorCopy(_avatar);
    }
    
    public void OnAvatarConfigured()
    {
        if (!_isAvatarConfigured)
            return;

        Animator baseAnimator = _avatar.GetComponent<Animator>();
        
        if (!_mirrorClone.TryGetComponent(out _mirrorAnimator))
            _mirrorAnimator = gameObject.AddComponent<Animator>();
        _mirrorAnimator.runtimeAnimatorController = baseAnimator.runtimeAnimatorController;

        _animatorManager._copyAnimator = _mirrorAnimator; // thank you for existing
        
        var cameras = PlayerSetup.Instance.GetComponentsInChildren<Camera>(true);
        foreach (var camera in cameras)
        {
            // hide PlayerClone layer from all cameras
            camera.cullingMask &= ~(1 << CVRLayers.PlayerClone);
        }
        
        var mirrors = Resources.FindObjectsOfTypeAll<CVRMirror>();
        foreach (CVRMirror mirror in mirrors)
        {
            // hide PlayerLocal layer from all mirrors
            mirror.m_ReflectLayers &= ~(1 << CVRLayers.PlayerLocal);
        }
        
        // scale avatar head bone to 0 0 0
        Transform headBone = baseAnimator.GetBoneTransform(HumanBodyBones.Head);
        headBone.localScale = Vector3.zero;

        CleanupAvatar();
        CleanupMirrorClone();
        SetupHumanPoseHandler();
        
        _initializationTarget.SetActive(true);
    }

    public void OnAvatarDestroyed()
    {
        if (!_isAvatarConfigured)
            return;
        
        _avatar = null;
        _mirrorAnimator = null;
        if (_mirrorClone != null)
            Destroy(_mirrorClone);
        
        _initializationTarget.SetActive(false);
        
        _isAvatarConfigured = false;
    }

    public void OnPostSolverUpdateGeneral()
    {
        if (!_isAvatarConfigured)
            return;
        
        StealTransforms();
    }

    #endregion

    #region Private Methods

    private GameObject InstantiateMirrorCopy(GameObject original)
    {
        GameObject clone = Instantiate(original, _initializationTarget.transform);
        clone.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        clone.name = original.name + " (Mirror Clone)";
        clone.SetLayerRecursive(CVRLayers.PlayerClone);
        return clone;
    }

    private void CleanupAvatar()
    {
        // set local avatar mesh to shadow off
        var avatarMeshes = _avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer avatarMesh in avatarMeshes)
        {
            avatarMesh.shadowCastingMode = ShadowCastingMode.Off;
            avatarMesh.forceMatrixRecalculationPerRender = false;
        }
    }

    private void CleanupMirrorClone()
    {
        // destroy unneeded components
        // only keep Animator
        
        var components = _mirrorClone.GetComponentsInChildren<Component>(true);
        foreach (Component component in components)
        {
            if (component == null)
                continue;
            
            // skip basic unity components
            if (component is Animator 
                or Transform
                or SkinnedMeshRenderer
                or MeshRenderer
                or MeshFilter)
                continue;
            
            // skip basic CVR components
            if (component is CVRAvatar or CVRAssetInfo)
            {
                (component as MonoBehaviour).enabled = false;
                continue;
            }
            
            Destroy(component);
        }
    }

    #endregion

    #region Job System

    private HumanPoseHandler _humanPoseHandler;
    private Transform _hipTransform;
    
    private void SetupHumanPoseHandler()
    {
        _hipTransform = _mirrorAnimator.GetBoneTransform(HumanBodyBones.Hips);

        _humanPoseHandler?.Dispose();
        _humanPoseHandler = new HumanPoseHandler(_mirrorAnimator.avatar, _mirrorAnimator.transform);
    }
    
    private void StealTransforms()
    {
        // copy transforms from avatar to mirror clone
        // var avatarTransforms = _avatar.GetComponentsInChildren<Transform>(true);
        // var mirrorCloneTransforms = _mirrorClone.GetComponentsInChildren<Transform>(true);
        // for (int i = 0; i < avatarTransforms.Length; i++)
        // {
        //     Transform avatarTransform = avatarTransforms[i];
        //     Transform mirrorCloneTransform = mirrorCloneTransforms[i];
        //     
        //     mirrorCloneTransform.SetLocalPositionAndRotation(
        //         avatarTransform.localPosition,
        //         avatarTransform.localRotation);
        // }

        if (!IKSystem.Instance.IsAvatarCalibrated())
            return;
        
        IKSystem.Instance._humanPoseHandler.GetHumanPose(ref IKSystem.Instance._humanPose);
        _humanPoseHandler.SetHumanPose(ref IKSystem.Instance._humanPose);

        if (!MetaPort.Instance.isUsingVr)
            _mirrorAnimator.transform.SetPositionAndRotation(PlayerSetup.Instance.GetPlayerPosition(), PlayerSetup.Instance.GetPlayerRotation());
        else
            _mirrorAnimator.transform.SetPositionAndRotation(_avatar.transform.position, _avatar.transform.rotation);
        
        _hipTransform.SetPositionAndRotation(IKSystem.Instance._hipTransform.position, IKSystem.Instance._hipTransform.rotation);
    }

    #endregion
}