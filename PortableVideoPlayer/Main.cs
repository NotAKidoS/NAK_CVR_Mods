using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using MelonLoader;
using UnityEngine;

namespace NAK.PortableVideoPlayer;

public class PortableVideoPlayerMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    private static readonly MelonPreferences_Category Preferences =
        MelonPreferences.CreateCategory(nameof(PortableVideoPlayer));

    internal static readonly MelonPreferences_Entry<bool> EntryOverrenderWorld =
        Preferences.CreateEntry("overrender_world", false, display_name: "Overrender World", description: "Render the portable video player over the world.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        if (!LoadAssetBundle())
            return;

        ApplyPatches(typeof(Patches.CVR_MenuManager_Patches));
        ApplyPatches(typeof(Patches.ViewManagerVideoPlayer_Patches));
        ApplyPatches(typeof(Patches.PickupUiElement_Patches));

        EntryOverrenderWorld.OnEntryValueChanged.Subscribe((_, value) => PortableVideoPlayerController.SetOverrenderWorld(value));
    }

    internal static void SetupQuickMenu()
    {
        QuickMenuAPI.PrepareIcon(nameof(PortableVideoPlayer), "screen",
            typeof(PortableVideoPlayerMod).Assembly.GetManifestResourceStream("PortableVideoPlayer.Resources.screen.png"));

        Category category = QuickMenuAPI.MiscTabPage.AddCategory("Portable Video Player", nameof(PortableVideoPlayer));

        Button spawnButton = category.AddButton("Spawn Portable Player", "screen", "Spawns a portable video player infront of you");
        spawnButton.OnPress += () =>
        {
            bool newState = PortableVideoPlayerController.Toggle();
            QuickMenuAPI.ShowAlertToast("Portable Video Player " + (newState ? "Spawned!" : "Removed!"));
        };

        ToggleButton overrenderToggle = category.AddToggle("Overrender World", "Toggles if the portable video player overrenders the world", EntryOverrenderWorld.Value);
        overrenderToggle.OnValueUpdated += b => EntryOverrenderWorld.Value = b;
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

    #region Asset Bundle Loading

    private const string PortableVideoPlayerAssets = "PortableVideoPlayer.Resources.portablevideoplayer.assets";
    private const string PortableVideoPlayerPrefabPath = "Packages/com.nak.portablevideoplayer/Runtime/Prefabs/PortableVideoPlayer.prefab";

    internal static GameObject PortableVideoPlayerPrefab;

    private bool LoadAssetBundle()
    {
        LoggerInstance.Msg("Loading required asset bundle...");
        using Stream resourceStream = MelonAssembly.Assembly.GetManifestResourceStream(PortableVideoPlayerAssets);
        using MemoryStream memoryStream = new();
        if (resourceStream == null)
        {
            LoggerInstance.Error($"Failed to load {PortableVideoPlayerAssets}!");
            return false;
        }

        resourceStream.CopyTo(memoryStream);
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
        if (assetBundle == null)
        {
            LoggerInstance.Error($"Failed to load {PortableVideoPlayerAssets}! Asset bundle is null!");
            return false;
        }

        PortableVideoPlayerPrefab = assetBundle.LoadAsset<GameObject>(PortableVideoPlayerPrefabPath);
        if (PortableVideoPlayerPrefab == null)
        {
            LoggerInstance.Error($"Failed to load {PortableVideoPlayerPrefabPath} from bundle!");
            return false;
        }

        PortableVideoPlayerPrefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;

        LoggerInstance.Msg("Asset bundle successfully loaded!");
        return true;
    }

    #endregion Asset Bundle Loading
}
