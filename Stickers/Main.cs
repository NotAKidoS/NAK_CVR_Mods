using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.InputManagement;
using MelonLoader;
using NAK.Stickers.Integrations;
using NAK.Stickers.Networking;
using UnityEngine;

namespace NAK.Stickers;

public class StickerMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    #region Melon Mod Overrides

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        ModNetwork.Subscribe();
        ModSettings.Initialize();
        StickerSystem.Initialize();
        
        ApplyPatches(typeof(Patches.PlayerSetupPatches));
        ApplyPatches(typeof(Patches.ControllerRayPatches));
        ApplyPatches(typeof(Patches.ShaderFilterHelperPatches));
        ApplyPatches(typeof(Patches.CVRToolsPatches));
        
        LoadAssetBundle();
        
        InitializeIntegration(nameof(BTKUILib), BTKUIAddon.Initialize); // quick menu ui
    }
    
    public override void OnUpdate()
    {
        if (StickerSystem.Instance == null) 
            return;

        if (!MetaPort.Instance.isUsingVr 
            && StickerSystem.Instance.IsInStickerMode)
        {
            if (Input.mouseScrollDelta.y != 0f
                && Cursor.lockState == CursorLockMode.Locked // prevent scrolling while in menus
                && !CVRInputManager.Instance.zoom) // prevent scrolling while using scroll zoom
            {
                StickerSystem.Instance.SelectedStickerSlot += (int)Input.mouseScrollDelta.y;
            }
            StickerSystem.Instance.PlaceStickerFromControllerRay(PlayerSetup.Instance.activeCam.transform, CVRHand.Left, true);
        }
        
        StickerSystem.Instance.UpdateStickerPreview(); // flashy flash
        
        if (!ModSettings.Entry_UsePlaceBinding.Value) 
            return;
        
        if (!Input.GetKeyDown((KeyCode)ModSettings.Entry_PlaceBinding.Value)) 
            return;
        
        StickerSystem.Instance.PlaceStickerFromControllerRay(PlayerSetup.Instance.activeCam.transform);
    }
    
    public override void OnApplicationQuit()
    {
        StickerSystem.Instance.CleanupAll();
    }
    
    #endregion Melon Mod Overrides
    
    #region Melon Mod Utilities

    private static void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        Logger.Msg($"Initializing {modName} integration.");
        integrationAction.Invoke();
    }
    
    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }

    #endregion Melon Mod Utilities
    
    #region Asset Bundle Loading

    private const string DecaleryShaderAssets = "decalery_shaders.assets";
    private const string DecalerySimpleShader = "Assets/Decalery/Shaders/DecalerySimple.shader";
    private const string GPUDecalWriteShader = "Assets/Decalery/fGPUDecalWriteShader.shader";
    
    private const string SourceSFX_PlayerSprayer = "Assets/Mods/Stickers/Source_sound_player_sprayer.wav";
    private const string LittleBigPlanetSFX_StickerPlace = "Assets/Mods/Stickers/LBP_Sticker_Place.wav";
    private const string FactorioSFX_AlertDestroyed = "Assets/Mods/Stickers/Factorio_alert_destroyed.wav";
    
    internal static Shader DecalSimpleShader;
    internal static Shader DecalWriterShader;
    internal static AudioClip SourceSFXPlayerSprayer;
    internal static AudioClip LittleBigPlanetSFXStickerPlace;
    internal static AudioClip FactorioSFXAlertDestroyed;
    
    private void LoadAssetBundle()
    {
        LoggerInstance.Msg($"Loading required asset bundle...");
        using Stream resourceStream = MelonAssembly.Assembly.GetManifestResourceStream(DecaleryShaderAssets);
        using MemoryStream memoryStream = new();
        if (resourceStream == null) {
            LoggerInstance.Error($"Failed to load {DecaleryShaderAssets}!");
            return;
        }
        
        resourceStream.CopyTo(memoryStream);
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
        if (assetBundle == null) {
            LoggerInstance.Error($"Failed to load {DecaleryShaderAssets}! Asset bundle is null!");
            return;
        }
        
        DecalSimpleShader = assetBundle.LoadAsset<Shader>(DecalerySimpleShader);
        if (DecalSimpleShader == null) {
            LoggerInstance.Error($"Failed to load {DecalerySimpleShader}! Prefab is null!");
            return;
        }
        DecalSimpleShader.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        LoggerInstance.Msg($"Loaded {DecalerySimpleShader}!");
        
        DecalWriterShader = assetBundle.LoadAsset<Shader>(GPUDecalWriteShader);
        if (DecalWriterShader == null) {
            LoggerInstance.Error($"Failed to load {GPUDecalWriteShader}! Prefab is null!");
            return;
        }
        DecalWriterShader.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        LoggerInstance.Msg($"Loaded {GPUDecalWriteShader}!");
        
        SourceSFXPlayerSprayer = assetBundle.LoadAsset<AudioClip>(SourceSFX_PlayerSprayer);
        if (SourceSFXPlayerSprayer == null) {
            LoggerInstance.Error($"Failed to load {SourceSFX_PlayerSprayer}! Prefab is null!");
            return;
        }
        SourceSFXPlayerSprayer.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        LoggerInstance.Msg($"Loaded {SourceSFX_PlayerSprayer}!");
        
        LittleBigPlanetSFXStickerPlace = assetBundle.LoadAsset<AudioClip>(LittleBigPlanetSFX_StickerPlace);
        if (LittleBigPlanetSFXStickerPlace == null) {
            LoggerInstance.Error($"Failed to load {LittleBigPlanetSFX_StickerPlace}! Prefab is null!");
            return;
        }
        LittleBigPlanetSFXStickerPlace.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        LoggerInstance.Msg($"Loaded {LittleBigPlanetSFX_StickerPlace}!");
        
        FactorioSFXAlertDestroyed = assetBundle.LoadAsset<AudioClip>(FactorioSFX_AlertDestroyed);
        if (FactorioSFXAlertDestroyed == null) {
            LoggerInstance.Error($"Failed to load {FactorioSFX_AlertDestroyed}! Prefab is null!");
            return;
        }
        FactorioSFXAlertDestroyed.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        LoggerInstance.Msg($"Loaded {FactorioSFX_AlertDestroyed}!");
        
        // load
        
        LoggerInstance.Msg("Asset bundle successfully loaded!");
    }

    #endregion Asset Bundle Loading
}