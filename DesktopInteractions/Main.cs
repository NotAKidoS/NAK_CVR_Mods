using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.ChatBox;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.IK;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using RootMotion.FinalIK;
using UnityEngine;

namespace NAK.DesktopInteractions;

public class DesktopInteractionsMod : MelonMod
{
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(DesktopInteractions));

    private static readonly MelonPreferences_Entry<bool> EntryTypingGesture =
        Category.CreateEntry("enable_typing_gesture", true, 
            "Typing Gesture", description: "When enabled you will place your arm up to your ear when typing in ChatBox.");

    private static readonly MelonPreferences_Entry<bool> EntryZoomGesture =
        Category.CreateEntry("enable_zoom_gesture", false, 
            "Zoom Gesture", description: "When enabled you will cup your hands around your eyes while zooming.");
    
    public override void OnInitializeMelon()
    {
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(OnPlayerSetupStart);
        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener(OnLocalAvatarLoad);
        CVRGameEventSystem.Avatar.OnLocalAvatarHeightScale.AddListener(OnLocalAvatarHeightScale);
        
        HarmonyInstance.Patch(
            typeof(IKSystem).GetMethod(nameof(IKSystem.OnPreSolverUpdateGeneral), 
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(DesktopInteractionsMod).GetMethod(nameof(OnPreSolverUpdateGeneral),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static Transform _cameraTargetContainerTransform;
    private static Transform _earIKTargetTransform;
    private static Transform _leftEyeIKTargetTransform;
    private static Transform _rightEyeIKTargetTransform;
    private static Transform _calibratedEarIKTargetTransform;
    private static Transform _calibratedLeftEyeIKTargetTransform;
    private static Transform _calibratedRightEyeIKTargetTransform;
    
    private static IKLimbController _leftArmController;
    private static IKLimbController _rightArmController;

    private static void OnPlayerSetupStart()
    {
        Transform cameraTransform = PlayerSetup.Instance.desktopCamera.transform;

        _cameraTargetContainerTransform = new GameObject("ScaledTargetsContainer").transform;
        _cameraTargetContainerTransform.SetParent(cameraTransform, false);
        _cameraTargetContainerTransform.localScale = Vector3.one * PlayerSetup.Instance.GetPlaySpaceScale() * 1.8f;
        
        _earIKTargetTransform = new GameObject("LeftEarIKTarget").transform;
        _earIKTargetTransform.SetParent(_cameraTargetContainerTransform);
        _earIKTargetTransform.localPosition = new Vector3(-0.1141031f, -0.05610896f, 0.01008159f);
        _earIKTargetTransform.localRotation = Quaternion.Euler(new Vector3(-63.037f, -108.763f, 141.237f));
        _calibratedEarIKTargetTransform = new GameObject("CalibratedOffset").transform;
        _calibratedEarIKTargetTransform.SetParent(_earIKTargetTransform, false);
        
        _leftEyeIKTargetTransform = new GameObject("LeftEyeIKTarget").transform;
        _leftEyeIKTargetTransform.SetParent(_cameraTargetContainerTransform);
        _leftEyeIKTargetTransform.localPosition = new Vector3(-0.0768f, -0.0551f, 0.0278f);
        _leftEyeIKTargetTransform.localRotation = Quaternion.Euler(new Vector3(-47.436f, -34.336f, 84.604f));
        _calibratedLeftEyeIKTargetTransform = new GameObject("CalibratedOffset").transform;
        _calibratedLeftEyeIKTargetTransform.SetParent(_leftEyeIKTargetTransform, false);
        
        _rightEyeIKTargetTransform = new GameObject("RightEyeIKTarget").transform;
        _rightEyeIKTargetTransform.SetParent(_cameraTargetContainerTransform);
        _rightEyeIKTargetTransform.localPosition = new Vector3(0.0768f, -0.0551f, 0.0278f);
        _rightEyeIKTargetTransform.localRotation = Quaternion.Euler(new Vector3(-132.564f, -145.664f, 95.396f));
        _calibratedRightEyeIKTargetTransform = new GameObject("CalibratedOffset").transform;
        _calibratedRightEyeIKTargetTransform.SetParent(_rightEyeIKTargetTransform, false);

        _leftArmController = new IKLimbController(8f);
        _rightArmController = new IKLimbController(8f);
    }
    
    private static void OnLocalAvatarLoad(CVRAvatar _)
    {
        if (!IKSystem.Instance.IsAvatarCalibrated()) return; // IKSystem did not consider for setup
        
        IKSystem.Instance.SetAvatarPose(IKSystem.AvatarPose.Default);
        
        VRIK vrik = IKSystem.vrik;
        Quaternion localHandRotationLeft = IKCalibrator.CalculateLocalRotation(vrik.references.root, vrik.references.leftHand);
        _calibratedEarIKTargetTransform.localRotation = localHandRotationLeft;
        _calibratedLeftEyeIKTargetTransform.localRotation = localHandRotationLeft;
        
        Quaternion localHandRotationRight = IKCalibrator.CalculateLocalRotation(vrik.references.root, vrik.references.rightHand);
        _calibratedRightEyeIKTargetTransform.localRotation = localHandRotationRight;
        
        IKSystem.Instance.SetAvatarPose(IKSystem.AvatarPose.LastSaved);
    }
    
    // I fucked the offsets and didn't account for scale until now, so we have to adjust it
    private static void OnLocalAvatarHeightScale(float height, float scale)
        => _cameraTargetContainerTransform.localScale = Vector3.one * scale * 1.8f;

    private static void OnPreSolverUpdateGeneral()
    {
        if (MetaPort.Instance.isUsingVr) return;
        
        bool isTyping = EntryTypingGesture.Value && ChatBoxManager.Instance.LocalPlayerBubble.IsTypingIndicatorActive;
        bool isZooming = EntryZoomGesture.Value && CVR_DesktopCameraController.GetCurrentZoomModifier() > 0.25f;

        _leftArmController.SetInfluence(10, isTyping, _calibratedEarIKTargetTransform);
        _leftArmController.SetInfluence(5, isZooming, _calibratedLeftEyeIKTargetTransform);
        _leftArmController.Update(Time.deltaTime);

        IKSolverVR solver = IKSystem.vrik.solver;
        
        IKSolverVR.Arm leftArm = solver.leftArm;
        leftArm.positionWeight = _leftArmController.Weight;
        leftArm.rotationWeight = _leftArmController.Weight;
        leftArm.IKPosition = _leftArmController.Position;
        leftArm.IKRotation = _leftArmController.Rotation;

        _rightArmController.SetInfluence(5, isZooming, _calibratedRightEyeIKTargetTransform);
        _rightArmController.Update(Time.deltaTime);
        
        IKSolverVR.Arm rightArm = solver.rightArm;
        rightArm.positionWeight = _rightArmController.Weight;
        rightArm.rotationWeight = _rightArmController.Weight;
        rightArm.IKPosition = _rightArmController.Position;
        rightArm.IKRotation = _rightArmController.Rotation;
    }
}

public class IKLimbController(float blendSpeed)
{
    private struct Influence
    {
        public int Priority;
        public bool IsActive;
        public Transform Target;
    }

    private readonly Influence[] _influences = new Influence[8];
    private int _count;

    private Vector3 _position;
    private Quaternion _rotation = Quaternion.identity;
    private float _weight;
    
    private Transform _fromTarget;
    private Transform _toTarget;
    /*private Vector3 _fromPosition;
    private Quaternion _fromRotation;*/
    private float _blendT;
    
    public Vector3 Position => _position;
    public Quaternion Rotation => _rotation;
    public float Weight => _weight;

    public void SetInfluence(int priority, bool isActive, Transform target)
    {
        int index = -1;
        for (int i = 0; i < _count; i++)
        {
            if (_influences[i].Priority == priority)
            {
                index = i;
                break;
            }
            if (_influences[i].Priority < priority)
            {
                index = i;
                for (int j = _count; j > i; j--) _influences[j] = _influences[j - 1];
                _count++;
                break;
            }
        }

        if (index == -1)
        {
            if (_count >= _influences.Length) return;
            index = _count++;
        }

        _influences[index] = new Influence { Priority = priority, IsActive = isActive, Target = target };
    }

    public void Update(float deltaTime)
    {
        Transform target = null;
        for (int i = 0; i < _count; i++)
        {
            if (!_influences[i].IsActive) continue;
            target = _influences[i].Target;
            break;
        }

        float targetWeight;
        if (target != null)
        {
            targetWeight = 1f;
            
            if (_toTarget != target)
            {
                /*if (_toTarget != null)
                {
                    _fromPosition = _toTarget.position;
                    _fromRotation = _toTarget.rotation;
                }
                else
                {
                    _fromPosition = target.position;
                    _fromRotation = target.rotation;
                }*/
                _fromTarget = _toTarget ?? target;
                _toTarget = target;
                _blendT = 0f;
            }
            
            _blendT = Mathf.Min(_blendT + blendSpeed * deltaTime, 1f);
            _position = Vector3.Lerp(_fromTarget.position, _toTarget.position, _blendT);
            _rotation = Quaternion.Slerp(_fromTarget.rotation, _toTarget.rotation, _blendT);
        }
        else
        {
            targetWeight = 0f;
            _toTarget = null;
        }
        
        _weight = Mathf.MoveTowards(_weight, targetWeight, blendSpeed * deltaTime);
    }
}