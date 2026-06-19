using System.Reflection;
using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Systems.ContentClones;
using ABI_RC.Systems.GameEventSystem;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.MirroredReflection;

public class MirroredReflectionMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener(OnLocalAvatarLoad);
        CVRGameEventSystem.Avatar.OnLocalAvatarClear.AddListener(OnLocalAvatarClear);
        CVRGameEventSystem.Avatar.OnLocalAvatarHeightScale.AddListener(OnLocalAvatarHeightScale);
        HarmonyInstance.Patch(
            typeof(CVRMirror).GetMethod(nameof(CVRMirror.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(MirroredReflectionMod).GetMethod(nameof(OnPostCVRMirrorStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static readonly ContentCloneManager.CloneOptions PlayerClone = new()
        {
            SyncTransforms = true,
            SyncRootPosition = true,
            SyncRootScale = false,
            SyncMaterialSwaps = true,
            SyncProbeAnchors = false,
            OverrideLayers = true,
            CloneLayer = CVRLayers.PlayerClone,
            OnlyRenderIfOriginalNotInCamera = true,
            ShowCameraTypes = CVRCameraTypeFlags.UserGeneratedContent | CVRCameraTypeFlags.PortableCamera | CVRCameraTypeFlags.MirrorCamera | CVRCameraTypeFlags.CaptureCamera
        };

    private static ContentCloneManager.CloneData _mirrorClone;
    
    private static void OnLocalAvatarLoad(CVRAvatar avatar)
    {
        if (!avatar) return;
        _mirrorClone = ContentCloneManager.CreateClone(avatar.gameObject, PlayerClone);
        if (_mirrorClone != null)
        {
            _mirrorClone.CloneRootTransform.localScale = new Vector3(-1f, 1f, 1f);
            _mirrorClone.LastSourceRootScale = avatar.transform.localScale;
        }
    }

    private static void OnLocalAvatarClear(CVRAvatar avatar)
    {
        if (_mirrorClone is { IsDestroyed: false })
        {
            UnityEngine.Object.Destroy(_mirrorClone.CloneRoot);
            _mirrorClone = null;
        }
    }

    private static void OnLocalAvatarHeightScale(float height, float scale)
    {
        if (_mirrorClone is { IsDestroyed: false })
        {
            Vector3 localScale = PlayerSetup.Instance.AvatarTransform.localScale;
            localScale.x *= -1f;
            _mirrorClone.CloneRootTransform.localScale = localScale;
        }
    }

    private static void OnPostCVRMirrorStart(CVRMirror __instance)
    {
        __instance.m_ReflectLayers &= ~(1 << CVRLayers.PlayerLocal);  // remove PlayerLocal
        __instance.m_ReflectLayers |=  (1 << CVRLayers.PlayerClone);  // add PlayerClone
    }
}