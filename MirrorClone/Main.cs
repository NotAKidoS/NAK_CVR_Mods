using MelonLoader;
using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using ABI_RC.Systems.IK;
using UnityEngine;

namespace NAK.BetterShadowClone;

public class MirrorCloneMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        ModSettings.Initialize();
        
        try
        {
            InitializePatches();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }

    #region Harmony Patches
    
    private void InitializePatches()
    {
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.Awake), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyLib.HarmonyMethod(typeof(MirrorCloneMod).GetMethod(nameof(OnPlayerSetup_Awake_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.SetupAvatar)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(MirrorCloneMod).GetMethod(nameof(OnPlayerSetup_SetupAvatar_Prefix), BindingFlags.NonPublic | BindingFlags.Static)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(MirrorCloneMod).GetMethod(nameof(OnPlayerSetup_SetupAvatar_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.ClearAvatar)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(MirrorCloneMod).GetMethod(nameof(OnPlayerSetup_ClearAvatar_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(IKSystem).GetMethod(nameof(IKSystem.OnPostSolverUpdateGeneral), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyLib.HarmonyMethod(typeof(MirrorCloneMod).GetMethod(nameof(OnIKSystem_OnPostSolverUpdateGeneral_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );

        HarmonyInstance.Patch(
            typeof(TransformHiderForMainCamera).GetMethod(nameof(TransformHiderForMainCamera.ProcessHierarchy)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(MirrorCloneMod).GetMethod(nameof(OnTransformHiderForMainCamera_ProcessHierarchy_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static void OnPlayerSetup_Awake_Postfix()
        => MirrorCloneManager.OnPlayerSetupAwake();
    
    private static void OnPlayerSetup_SetupAvatar_Prefix(GameObject inAvatar)
        => MirrorCloneManager.Instance.OnAvatarInitialized(inAvatar);
    
    private static void OnPlayerSetup_SetupAvatar_Postfix()
        => MirrorCloneManager.Instance.OnAvatarConfigured();
    
    private static void OnPlayerSetup_ClearAvatar_Prefix()
        => MirrorCloneManager.Instance.OnAvatarDestroyed();
    
    private static void OnIKSystem_OnPostSolverUpdateGeneral_Postfix()
        => MirrorCloneManager.Instance.OnPostSolverUpdateGeneral();
    
    private static void OnTransformHiderForMainCamera_ProcessHierarchy_Prefix(ref bool __runOriginal)
        => __runOriginal = !ModSettings.EntryEnabled.Value;
    
    #endregion
}