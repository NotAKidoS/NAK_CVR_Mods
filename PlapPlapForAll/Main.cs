using ABI_RC.Core;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.IO.Social;
using MelonLoader;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.GameEventSystem;
using ABI.CCK.Components;
using UnityEngine;
using UnityEngine.Animations;

namespace NAK.PlapPlapForAll;

public class PlapPlapForAllMod : MelonMod
{
    public static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(OnPlayerSetupStart);
        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener(OnLocalAvatarLoaded);
        CVRGameEventSystem.Avatar.OnRemoteAvatarLoad.AddListener(OnRemoteAvatarLoaded);
        LoadAssetBundle();
    }

    private static void OnPlayerSetupStart()
    {
        PlapPlapPrefab.SetActive(false);
        
        // Remove ParentConstraint so we can reparent later
        ParentConstraint parentConstraint = PlapPlapPrefab.GetComponent<ParentConstraint>();
        if (parentConstraint) UnityEngine.Object.DestroyImmediate(parentConstraint);
        
        // Remove lights to avoid interfering with avatar lights
        Light[] lights = PlapPlapPrefab.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights) UnityEngine.Object.DestroyImmediate(light);
        
        // Register the audio sources underneath to the Avatar mixer group
        AudioSource[] audioSources = PlapPlapPrefab.GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource audioSource in audioSources) audioSource.outputAudioMixerGroup = RootLogic.Instance.avatarSfx;
        
        // Add StopHighlightPropagation to prevent plap plap from being highlighted
        StopHighlightPropagation stopHighlight = PlapPlapPrefab.AddComponent<StopHighlightPropagation>();
        stopHighlight.enabled = false; // marker only
        
        Logger.Msg("Patched PlapPlap prefab!");
    }
    
    private static void OnLocalAvatarLoaded(CVRAvatar avatar) 
        => OnAvatarLoaded(PlayerSetup.Instance, avatar);
    private static void OnRemoteAvatarLoaded(CVRPlayerEntity playerEntity, CVRAvatar avatar)
        => OnAvatarLoaded(playerEntity.PuppetMaster, avatar);

    private static void OnAvatarLoaded(PlayerBase player, CVRAvatar avatar)
    {
        // Enforcing friends with benefits
        if (!Friends.FriendsWith(player.PlayerId))
            return;
        
        // Ensure the avatar is NSFW
        UgcContentTags tags = player.AvatarMetadata.TagsData;
        if (tags is { Suggestive: false, Explicit: false } // Main tags
            && !avatar.TagHandledByAdvancedTagging(CVRAvatarAdvancedTaggingEntry.Tags.Suggestive) // Advanced tags
            && !avatar.TagHandledByAdvancedTagging(CVRAvatarAdvancedTaggingEntry.Tags.Explicit))
            return;
        
        // Ensure mature content is allowed by user settings
        if (!MetaPort.Instance.matureContentAllowed)
            return;
        
        GameObject avatarObject = avatar.gameObject;
        
        // Scan for DPS setups
        if (!DPS.ScanForDPS(avatarObject, out List<DPSOrifice> dpsOrifices, out bool foundPenetrator))
            return;
        
        // If no penetrator found, attempt to find one via TPS
        if (!foundPenetrator) DPS.AttemptTPSHack(avatarObject);
        
        // Setup PlapPlap for each found orifice
        if (dpsOrifices.Count != 0)
        {
            // Log found orifices
            // Logger.Msg($"Found {dpsOrifices.Count} DPS orifices on avatar '{avatarObject.name}' for player '{player.PlayerUsername}':");
            // foreach (DPSOrifice dpsOrifice in dpsOrifices) Logger.Msg($"- Orifice Type: {dpsOrifice.type}, DPS Light: {dpsOrifice.dpsLight.name}, Normal Light: {(dpsOrifice.normalLight != null ? dpsOrifice.normalLight.name : "None")}");
        
            // Configure PlapPlap for each orifice
            Animator avatarAnimator = player.Animator;
            foreach (DPSOrifice dpsOrifice in dpsOrifices)
            {
                // Skip if this is already a plap plap setup
                if (PlapPlapTap.IsBuiltInPlapPlapSetup(dpsOrifice))
                    continue;
            
                PlapPlapTap.CreateFromOrifice(
                    dpsOrifice,
                    avatarAnimator,
                    PlapPlapPrefab
                );
            }
        }
    }
    
    /* Asset Bundle Loading */
    
    private const string PlapPlapAssetsName = "PlapPlapForAll.Resources.plap plap.assets";
    private const string PlapPlapPrefabName = "Assets/Noachi/Plap Plap/plap plap.prefab";

    private static GameObject PlapPlapPrefab;
    
    private void LoadAssetBundle()
    {
        LoggerInstance.Msg($"Loading required asset bundle...");
        using Stream resourceStream = MelonAssembly.Assembly.GetManifestResourceStream(PlapPlapAssetsName);
        using MemoryStream memoryStream = new();
        if (resourceStream == null) {
            LoggerInstance.Error($"Failed to load {PlapPlapAssetsName}!");
            return;
        }
        
        resourceStream.CopyTo(memoryStream);
        AssetBundle assetBundle = AssetBundle.LoadFromStream(memoryStream);
        if (assetBundle == null) {
            LoggerInstance.Error($"Failed to load {PlapPlapAssetsName}! Asset bundle is null!");
            return;
        }
        
        PlapPlapPrefab = assetBundle.LoadAsset<GameObject>(PlapPlapPrefabName);
        if (PlapPlapPrefab == null) {
            LoggerInstance.Error($"Failed to load {PlapPlapPrefabName}! Prefab is null!");
            return;
        }
        PlapPlapPrefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        LoggerInstance.Msg($"Loaded {PlapPlapPrefabName}!");
        
        LoggerInstance.Msg("Asset bundle successfully loaded!");
    }
}