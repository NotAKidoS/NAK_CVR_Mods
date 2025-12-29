using ABI_RC.Core.Player;
using ABI_RC.Core.Util.AnimatorManager;
using ABI_RC.Systems.GameEventSystem;
using ABI.CCK.Components;
using MelonLoader;
using UnityEngine;

namespace NAK.IFUCKINGHATECAMERAS;

public class IFUCKINGHATECAMERASMod : MelonMod
{
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(IFUCKINGHATECAMERAS));

    private static readonly MelonPreferences_Entry<bool> EntryRunHack =
        Category.CreateEntry(
            identifier: "run_hack",
            true,
            display_name: "Run Camera Hack (Avatars Only)?",
            description: "Should the camera hack run? Btw I fucking hate cameras.");

    public override void OnInitializeMelon()
    {
        CVRGameEventSystem.Avatar.OnRemoteAvatarLoad.AddListener(OnRemoteAvatarLoad);
    }
    
    private static void OnRemoteAvatarLoad(CVRPlayerEntity playerEntity, CVRAvatar avatar)
    {
        if (!EntryRunHack.Value) return;
        
        // HACK: Fixes a native crash (animating camera off on first frame) due to culling in specific worlds.
        // I am unsure the root cause, but the local player doesn't crash, and this is similar to what that does.

        AvatarAnimatorManager AnimatorManager = playerEntity.PuppetMaster.AnimatorManager;
        AnimatorManager.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // Set culling mode to always animate
        AnimatorManager.Animator.Update(0f); // Update the animator to force it to do the first frame
        AnimatorManager.Animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; // Set to cull update transforms
    }
}