using System.Reflection;
using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.XRManagement;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.DynamicNearClip;

public class DynamicNearClipMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(OnPlayerSetupStart);
        CVRGameEventSystem.Avatar.OnLocalAvatarHeightScale.AddListener(OnLocalAvatarHeightScale);
        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener(OnLocalAvatarLoaded);
        XRDeviceEvents.OnPostXRModeSwitch.AddListener(OnPostXRModeSwitch);
        
        HarmonyInstance.Patch(
        typeof(CVRTools).GetMethod(nameof(CVRTools.ApplyForcedCameraSettings),
            BindingFlags.Public | BindingFlags.Static), 
        postfix: new HarmonyMethod(typeof(DynamicNearClipMod).GetMethod(
            nameof(OnCVRToolsApplyForcedCameraSettings),
            BindingFlags.NonPublic | BindingFlags.Static)
        ));
    }

    private static void OnPlayerSetupStart()
        => NearClipController.Bind(PlayerSetup.Instance.activeCam);

    private static void OnPostXRModeSwitch(XRModeSwitchEventArgs eventArgs) 
        => NearClipController.Bind(PlayerSetup.Instance.activeCam);

    private static void OnLocalAvatarHeightScale(float avatarHeight, float avatarScaleRatio) 
        => NearClipController.OnAvatarHeightChanged(avatarHeight);

    private static void OnCVRToolsApplyForcedCameraSettings(Camera destCamera) 
        => NearClipController.OnCameraPropertiesChanged(destCamera.farClipPlane);

    private static void OnLocalAvatarLoaded(CVRAvatar _)
        => NearClipController.OnAvatarHeightChanged(PlayerSetup.Instance.AvatarHeight);
}

public static class NearClipController
{
    // TODO: test if fucked near/far clip combos / extreme differences explodes in worlds that have
    // baked occlusion culling (no idea how to even detect that, no runtime api).
    // (vrc patched this crash with limits)
    
    private const float NearMin = 0.001f;
    private const float NearMax = 0.05f;
    
    private const float MaxNearFarRatio = 1_000_000f;
    private const float FarPreservationThreshold = 5000f;

    private const float DefaultNear = 0.01f;
    private const float ReferenceHeight = 1.6f;
    
    public static Camera BoundCamera { get; private set; }

    private static float _worldFar = 1000f;
    private static float _avatarHeight = ReferenceHeight;

    public static void Bind(Camera cam)
    {
        BoundCamera = cam;
        _worldFar = cam.farClipPlane;
        Apply();
    }

    public static void OnAvatarHeightChanged(float height)
    {
        _avatarHeight = Mathf.Max(0.01f, height);
        Apply();
    }
    
    public static void OnCameraPropertiesChanged(float requestedFar)
    {
        _worldFar = requestedFar;
        Apply();
    }

    private static void Apply()
    {
        float near = ComputeNear();

        BoundCamera.nearClipPlane = near;
        BoundCamera.farClipPlane = Mathf.Min(_worldFar, near * MaxNearFarRatio);
    }

    private static float ComputeNear()
    {
        float scaled = DefaultNear * (_avatarHeight / ReferenceHeight);
        return Mathf.Clamp(Mathf.Max(scaled, NearFloor()), NearMin, NearMax);
    }

    private static float NearFloor()
    {
        return _worldFar >= FarPreservationThreshold
            ? Mathf.Max(NearMin, _worldFar / MaxNearFarRatio)
            : NearMin;
    }
}