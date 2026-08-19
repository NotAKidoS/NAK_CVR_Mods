using ABI_RC.Core.Util.AssetFiltering;
using MelonLoader;
using NAK.CleanPlates.Helpers;
using NAK.CleanPlates.Network;
using NAK.CleanPlates.UI;
using UnityEngine;

namespace NAK.CleanPlates;

public class CleanPlatesMod : MelonMod
{
    public static MelonLogger.Instance Logger;

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(CleanPlates));

    internal static readonly MelonPreferences_Entry<bool> SpeakingIndicator =
        Category.CreateEntry("speaking_indicator", true, display_name: "Speaking Indicator", description: "Show a speaker or TTS icon while a player is talking.");

    internal static readonly MelonPreferences_Entry<bool> ChatBubbleHistory =
        Category.CreateEntry("chat_bubble_history", true, display_name: "Chat Bubble History", description: "Keep older messages stacked above the newest chat bubble.");

    internal static readonly MelonPreferences_Entry<bool> HideNearPlates =
        Category.CreateEntry("hide_near_plates", true, display_name: "Hide Near Plates", description: "Hide a plate once you are close enough for it to be in your face.");

    internal static readonly MelonPreferences_Entry<RoundedHexGraphic.Shape> PlateShape =
        Category.CreateEntry("plate_shape", RoundedHexGraphic.Shape.Hexagonal, display_name: "Plate Shape", description: "Hexagon caps, softly rounded corners, or fully round ends.");

    internal static readonly MelonPreferences_Entry<NameplateStyle> PlateStyle =
        Category.CreateEntry("plate_style", NameplateStyle.Full, display_name: "Nameplate Style", description: "Full plate, a compact one line plate, or the compact plate without the profile image.");

    internal static readonly MelonPreferences_Entry<NameplateScale> PlateScale =
        Category.CreateEntry("plate_scale", NameplateScale.Normal, display_name: "Nameplate Scale", description: "Overall size of the plates.");

    internal static readonly MelonPreferences_Entry<int> PlateOpacity =
        Category.CreateEntry("plate_opacity", 50, display_name: "Nameplate Opacity", description: "Background opacity in percent. Text and icons are unaffected.");

    // Debug settings

    internal static readonly MelonPreferences_Entry<bool> Debug_NetworkInbound =
        Category.CreateEntry("debug_inbound", false, display_name: "Debug Net Inbound", description: "Log inbound Mod Network updates.");

    internal static readonly MelonPreferences_Entry<bool> Debug_NetworkOutbound =
        Category.CreateEntry("debug_outbound", false, display_name: "Debug Net Outbound", description: "Log outbound Mod Network updates.");

    internal static readonly MelonPreferences_Entry<bool> Debug_ProfilePlates =
        Category.CreateEntry("debug_profile_plates", false, display_name: "Debug Profile Plates", description: "Log timings for the nameplate processing loop.");

    internal static readonly MelonPreferences_Entry<bool> Debug_ProfileCameraPlates =
        Category.CreateEntry("debug_profile_camera_plates", false, display_name: "Debug Profile Camera Plates", description: "Log timings for the camera indicator plate loop.");

    internal static readonly MelonPreferences_Entry<bool> Debug_ProfileAnchor =
        Category.CreateEntry("debug_profile_anchor", false, display_name: "Debug Profile Anchor", description: "Log how long measuring an avatar for its nameplate height takes.");

    internal static readonly MelonPreferences_Entry<bool> Debug_AnchorContributions =
        Category.CreateEntry("debug_anchor_contributions", false, display_name: "Debug Anchor Contributions", description: "Log which renderers are pushing an avatar's nameplate height up.");
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        if (!LoadAssetBundle())
        {
            LoggerInstance.Error("Asset bundle failed to load, the game's own nameplates are still in use.");
            return;
        }

        ApplyPatches(typeof(Patches.CameraIndicatorPlate_Patches));
        ApplyPatches(typeof(Patches.PlayerNameplate_Patches));
        ApplyPatches(typeof(Patches.CVRObjectLoader_Patches));
        ApplyPatches(typeof(Patches.OverheadController_Patches));
        ApplyPatches(typeof(Patches.NetIKController_Patches));
        ApplyPatches(typeof(Patches.ChatBoxBubbleBehavior_Patches));
        ApplyPatches(typeof(Patches.CVRPlayerManager_Patches));
        ApplyPatches(typeof(Patches.ViewManager_Patches));

        // Added at bundle load to the prefab, don't let asset filer strip it.
        SharedFilter.AvatarWhitelist.Add(typeof(NameplateAnchor));

        SetupPreferences();

        PlateManager.Init();
        CleanPlatesNetwork.Init();
        
        ApplyPlateSettings();
    }

    private static void ApplyPlateSettings()
    {
        PlateManager.NearHide = HideNearPlates.Value;
        PlateManager.UserScale = PlateScale.Value switch
        {
            NameplateScale.Tiny => 0.6f,
            NameplateScale.Small => 0.8f,
            NameplateScale.Medium => 1.25f,
            NameplateScale.Large => 1.5f,
            _ => 1f
        };
        NameplateView.BackgroundOpacity = Mathf.Clamp01(PlateOpacity.Value / 100f);
        PlateManager.Style = PlateStyle.Value;
        NameplateView.ShowIconSlot = PlateStyle.Value != NameplateStyle.Minimal;
    }

    private static void RefreshPlateSettings(bool rebind)
    {
        NameplateStyle style = PlateManager.Style;
        ApplyPlateSettings();

        if (style != PlateManager.Style)
        {
            // Compact and Minimal share a prefab, only the image toggles.
            if ((style == NameplateStyle.Full) != (PlateManager.Style == NameplateStyle.Full))
                PlateManager.RebuildAll();
            else
                PlateManager.RebindAll();

            // Images are only requested while they are being shown, so coming
            // back from Minimal has to go and fetch them again.
            PlateManager.ResyncAll();
        }
        else if (rebind) PlateManager.RebindAll();

        PlateManager.RefreshSettings();
    }

    private static void SetupPreferences()
    {
        HideNearPlates.OnEntryValueChanged.Subscribe((_, _) => RefreshPlateSettings(false));
        PlateScale.OnEntryValueChanged.Subscribe((_, _) => RefreshPlateSettings(false));
        PlateOpacity.OnEntryValueChanged.Subscribe((_, _) => RefreshPlateSettings(true));
        PlateStyle.OnEntryValueChanged.Subscribe((_, _) => RefreshPlateSettings(false));
        
        RoundedHexGraphic.SetPreferredShape(PlateShape.Value);
        PlateShape.OnEntryValueChanged.Subscribe((_, value) =>
        {
            RoundedHexGraphic.SetPreferredShape(value);
            PlateManager.RebindAll();
            PlateManager.RebindCameraIndicators();
        });

        NameplateChat.ShowSpeakerIndicator = SpeakingIndicator.Value;
        NameplateChat.ShowHistory = ChatBubbleHistory.Value;

        PlateManager.Profile = Debug_ProfilePlates.Value;
        PlateManager.CameraProfile = Debug_ProfileCameraPlates.Value;
        NameplateAnchorUtility.Profile = Debug_ProfileAnchor.Value;
        NameplateAnchorUtility.LogContributions = Debug_AnchorContributions.Value;

        Debug_ProfilePlates.OnEntryValueChanged.Subscribe((_, value) 
            => PlateManager.Profile = value);
        Debug_ProfileCameraPlates.OnEntryValueChanged.Subscribe((_, value)
            => PlateManager.CameraProfile = value);
        Debug_ProfileAnchor.OnEntryValueChanged.Subscribe((_, value) 
            => NameplateAnchorUtility.Profile = value);
        Debug_AnchorContributions.OnEntryValueChanged.Subscribe((_, value)
            => NameplateAnchorUtility.LogContributions = value);

        SpeakingIndicator.OnEntryValueChanged.Subscribe((_, value)
            => NameplateChat.ShowSpeakerIndicator = value);

        ChatBubbleHistory.OnEntryValueChanged.Subscribe((_, value) =>
        {
            NameplateChat.ShowHistory = value;
            PlateManager.ForEachChat(static chat => chat.RefreshHistory());
        });
    }

    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Warning("The mod has failed initial patching and will unpatch itself!");
            LoggerInstance.Error($"Failed while patching {type.Name}:", e);
            HarmonyInstance.UnpatchSelf();
            throw;
        }
    }

    #region Asset Bundle Loading

    private const string CleanPlatesAssets = "CleanPlates.Resources.cleanplates.assets";
    private const string CleanPlatesPrefabPath = "Packages/com.nak.cleanplates/Runtime/Prefabs/FullPlate.prefab";
    private const string CleanPlatesSimplePrefabPath = "Packages/com.nak.cleanplates/Runtime/Prefabs/SimplePlate.prefab";
    private const string CleanPlatesMiniPrefabPath = "Packages/com.nak.cleanplates/Runtime/Prefabs/CameraPlate.prefab";

    internal static GameObject CleanPlatesPrefab;
    internal static GameObject CleanPlatesSimplePrefab;
    internal static GameObject CleanPlatesMiniPrefab;

    private bool LoadAssetBundle()
    {
        LoggerInstance.Msg("Loading required asset bundle...");
        using Stream resourceStream = MelonAssembly.Assembly.GetManifestResourceStream(CleanPlatesAssets);
        using MemoryStream memoryStream = new();
        if (resourceStream == null)
        {
            LoggerInstance.Error($"Failed to load {CleanPlatesAssets}!");
            return false;
        }

        resourceStream.CopyTo(memoryStream);
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
        if (assetBundle == null)
        {
            LoggerInstance.Error($"Failed to load {CleanPlatesAssets}! Asset bundle is null!");
            return false;
        }

        if (!TryLoadPrefab(assetBundle, CleanPlatesPrefabPath, out CleanPlatesPrefab)
            || !TryLoadPrefab(assetBundle, CleanPlatesSimplePrefabPath, out CleanPlatesSimplePrefab)
            || !TryLoadPrefab(assetBundle, CleanPlatesMiniPrefabPath, out CleanPlatesMiniPrefab))
            return false;

        // We are parenting under an existing canvas, so we need to remove ours.
        // This is so the CanvasGroup hierarchy processing doesn't fucking die.
        // The camera indicator keeps its own, it parents beside the wrapper.
        UnityEngine.Object.Destroy(CleanPlatesPrefab.GetComponent<Canvas>());
        UnityEngine.Object.Destroy(CleanPlatesSimplePrefab.GetComponent<Canvas>());

        LoggerInstance.Msg("Asset bundle successfully loaded!");
        return true;
    }

    private bool TryLoadPrefab(AssetBundle bundle, string path, out GameObject prefab)
    {
        prefab = bundle.LoadAsset<GameObject>(path);
        if (prefab == null)
        {
            LoggerInstance.Error($"Failed to load {path} from bundle!");
            return false;
        }

        prefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        return true;
    }

    #endregion Asset Bundle Loading
}