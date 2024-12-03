using MelonLoader;
using NAK.ShareBubbles.Networking;
using UnityEngine;

namespace NAK.ShareBubbles;

public class ShareBubblesMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    #region Melon Mod Overrides

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        ShareBubbleManager.Initialize();
        TempShareManager.Initialize();
        
        ModNetwork.Initialize();
        ModNetwork.Subscribe();
        
        //ModSettings.Initialize();
        
        ApplyPatches(typeof(Patches.PlayerSetup_Patches));
        ApplyPatches(typeof(Patches.ControllerRay_Patches));
        ApplyPatches(typeof(Patches.ViewManager_Patches));
        
        LoadAssetBundle();
    }

    public override void OnApplicationQuit()
    {
        ModNetwork.Unsubscribe();
    }
    
    #endregion Melon Mod Overrides
    
    #region Melon Mod Utilities

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

    private const string SharingBubbleAssets = "ShareBubbles.Resources.sharingbubble.assets";
    private const string SharingBubblePrefabPath = "Assets/Mods/SharingBubble/SharingBubble.prefab";

    internal static GameObject SharingBubblePrefab;
    
    private void LoadAssetBundle()
    {
        LoggerInstance.Msg($"Loading required asset bundle...");
        using Stream resourceStream = MelonAssembly.Assembly.GetManifestResourceStream(SharingBubbleAssets);
        using MemoryStream memoryStream = new();
        if (resourceStream == null) {
            LoggerInstance.Error($"Failed to load {SharingBubbleAssets}!");
            return;
        }
        
        resourceStream.CopyTo(memoryStream);
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
        if (assetBundle == null) {
            LoggerInstance.Error($"Failed to load {SharingBubbleAssets}! Asset bundle is null!");
            return;
        }
        
        SharingBubblePrefab = assetBundle.LoadAsset<GameObject>(SharingBubblePrefabPath);
        if (SharingBubblePrefab == null) {
            LoggerInstance.Error($"Failed to load {SharingBubblePrefab}! Prefab is null!");
            return;
        }
        SharingBubblePrefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        LoggerInstance.Msg($"Loaded {SharingBubblePrefab}!");
        
        // load
        
        LoggerInstance.Msg("Asset bundle successfully loaded!");
    }

    #endregion Asset Bundle Loading
}