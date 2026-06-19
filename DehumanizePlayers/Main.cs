using System.Reflection;
using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.DehumanizePlayers;

public class DehumanizePlayersMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(DehumanizePlayers));
    
    private static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("enabled", true, 
            "Dehumanize Players", description: "When enabled creates a dummy animator above the avatar root which handles muscle application.");
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        HarmonyInstance.Patch(
            typeof(NetIKController).GetMethod(nameof(NetIKController.SetupAvatar),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(DehumanizePlayersMod).GetMethod(nameof(OnPreNetIKControllerSetupAvatar),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    // ReSharper disable once RedundantAssignment
    private static void OnPreNetIKControllerSetupAvatar(GameObject remoteAvatar, ref Animator remoteAnimator)
    {
        if (!EntryEnabled.Value) return;
        remoteAnimator = HumanoidAvatarRebinder.RebindToParentHumanoidAnimator(remoteAvatar);
    }
}