using ABI_RC.Core.Player;
using ABI_RC.Systems.PlayerColors;
using NAK.CleanPlates.UI;
using NAK.CleanPlates.Helpers;
using Object = UnityEngine.Object;
using UnityEngine;

namespace NAK.CleanPlates;

// Camera and pen indicators
public static partial class PlateManager
{
    public static float CameraNearScale = 1f;
    public static float CameraFarScale = 1.4f;

    public static float CameraCollapsedMaxScale = 1.15f;
    public static float CameraMaxScale = 3.5f;

    // Tells us when the camera indicator is toggled, cause sadly native
    // there is no event for us to hook. >:(
    public class CameraPlateTracker : MonoBehaviour
    {
        internal Entry Entry;

        private void OnEnable() => indicators.Add(Entry);

        private void OnDisable()
        {
            indicators.Remove(Entry);
            Entry.Distance = float.MaxValue;
            Entry.Alpha = 0f;
            Entry.LodBlend = 0f;
            Entry.Scale = -1f;
            Entry.Mini.SetState(0f, 0f);
        }
    }

    private static readonly List<Entry> indicators = new();
    private static int indicatorScanIndex;

    private static readonly LoopProfiler CameraProfiler = new("Cameras", trackDrawn: false);

    public static bool CameraProfile
    {
        get => CameraProfiler.Enabled;
        set => CameraProfiler.Enabled = value;
    }

    internal static void OnIndicatorPlateStart(IndicatorPlate original)
    {
        PlayerBase player = original.playerBase;

        // Game hover constraint keeps indicator above camera or pen
        NameplateTheme.EnsureBuilt();
        GameObject go = Object.Instantiate(CleanPlatesMod.CleanPlatesMiniPrefab, original.transform, false);
        go.SetActive(false); // so the CameraPlateTracker registers with everything filled in

        go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        MiniNameplate plate = go.GetComponent<MiniNameplate>();
        if (original.nameplateIcon.sprite == original.penIcon) plate.SetIcon(original.penIcon);
        plate.SetState(0f, 0f);

        var entry = new Entry
        {
            Mini = plate,
            IsIndicator = true,
            IsLocal = player.IsLocalPlayer,
            Player = player,
            Anchor = go.transform,
            BaseScale = go.transform.localScale
        };
        go.AddComponent<CameraPlateTracker>().Entry = entry;

        Rebind(entry, player.IsLocalPlayer
            ? PlayerColorsManager.CurrentColors
            : PlayerColorsManager.GetPlayerColors(player.PlayerId));

        go.SetActive(true);
    }

    internal static void SetPlayerColors(PlayerBase player, PlayerColors colors)
    {
        // Camera and pen can both be out
        foreach (Entry e in indicators)
            if (e.Player == player) Rebind(e, colors);
    }

    internal static void RebindCameraIndicators()
    {
        foreach (Entry e in indicators)
            Rebind(e, e.IsLocal
                ? PlayerColorsManager.CurrentColors
                : PlayerColorsManager.GetPlayerColors(e.Player.PlayerId));
    }

    private static void Rebind(Entry entry, PlayerColors colors)
    {
        MiniNameplate plate = entry.Mini;
        plate.Bind(entry.Player.PlayerUsername, colors.PrimaryColor, colors.SecondaryColor);
        plate.SetBackgroundOpacity(NameplateView.BackgroundOpacity);
    }

    internal static void RefreshSettings()
    {
        float opacity = NameplateView.BackgroundOpacity;
        foreach (Entry e in indicators) e.Mini.SetBackgroundOpacity(opacity);
    }
}