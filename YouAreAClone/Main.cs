using System;
using System.IO;
using MelonLoader;
using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using ABI_RC.Core.Util.AssetFiltering;
using ABI_RC.Systems.Camera;
using UnityEngine;

namespace NAK.YouAreAClone;

public class YouAreAClone : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        //VRModeSwitchEvents.OnCompletedVRModeSwitch.AddListener(_ => FindCameras());
        
        SharedFilter._avatarWhitelist.Add(typeof(FPRExclusion));
        SharedFilter._localComponentWhitelist.Add(typeof(FPRExclusion));
        
        try
        {
            LoadAssetBundle();
            InitializePatches();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }

    #region Asset Bundle Loading

    private const string YouAreACloneAssets = "YouAreAClone.assets";
    
    private const string BoneHiderComputePath = "Assets/Koneko/ComputeShaders/BoneHider.compute";
    //private const string MeshCopyComputePath = "Assets/Koneko/ComputeShaders/MeshCopy.compute";
    
    private const string ShadowCloneComputePath = "Assets/NotAKid/Shaders/ShadowClone.compute";
    private const string ShadowCloneShaderPath = "Assets/NotAKid/Shaders/ShadowClone.shader";
    private const string DummyCloneShaderPath = "Assets/NotAKid/Shaders/DummyClone.shader";

    private void LoadAssetBundle()
    {
        Logger.Msg($"Loading required asset bundle...");
        using Stream resourceStream = MelonAssembly.Assembly.GetManifestResourceStream(YouAreACloneAssets);
        using MemoryStream memoryStream = new();
        if (resourceStream == null) {
            Logger.Error($"Failed to load {YouAreACloneAssets}!");
            return;
        }
        
        resourceStream.CopyTo(memoryStream);
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
        if (assetBundle == null) {
            Logger.Error($"Failed to load {YouAreACloneAssets}! Asset bundle is null!");
            return;
        }

        // load shaders
        ComputeShader shader = assetBundle.LoadAsset<ComputeShader>(BoneHiderComputePath);
        shader.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        TransformHiderManager.shader = shader;
        Logger.Msg($"Loaded {BoneHiderComputePath}!");
        
        // load shadow clone shader
        ComputeShader shadowCloneCompute = assetBundle.LoadAsset<ComputeShader>(ShadowCloneComputePath);
        shadowCloneCompute.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        ShadowCloneHelper.shader = shadowCloneCompute;
        Logger.Msg($"Loaded {ShadowCloneComputePath}!");
        
        // load shadow clone material
        Shader shadowCloneShader = assetBundle.LoadAsset<Shader>(ShadowCloneShaderPath);
        shadowCloneShader.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        ShadowCloneHelper.shadowMaterial = new Material(shadowCloneShader);
        Logger.Msg($"Loaded {ShadowCloneShaderPath}!");
        
        Logger.Msg("Asset bundle successfully loaded!");
    }

    #endregion

    #region Harmony Patches
    
    private void InitializePatches()
    {
        HarmonyInstance.Patch(
            typeof(TransformHiderForMainCamera).GetMethod(nameof(TransformHiderForMainCamera.ProcessHierarchy)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(YouAreAClone).GetMethod(nameof(OnTransformHiderForMainCamera_ProcessHierarchy_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.ClearAvatar)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(YouAreAClone).GetMethod(nameof(OnPlayerSetup_ClearAvatar_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static void OnPlayerSetup_ClearAvatar_Prefix()
    {
        TransformHiderManager.Instance.OnAvatarCleared();
        ShadowCloneManager.Instance.OnAvatarCleared();
    }

    private static void OnTransformHiderForMainCamera_ProcessHierarchy_Prefix(ref bool __runOriginal)
    { 
        if (!__runOriginal || (__runOriginal = !ModSettings.EntryEnabled.Value)) 
            return; // if something else disabled, or we are disabled, don't run
        
        ShadowCloneHelper.SetupAvatar(PlayerSetup.Instance._avatar);
    }
    
    #endregion
}