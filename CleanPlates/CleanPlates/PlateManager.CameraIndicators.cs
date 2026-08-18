using ABI_RC.Core.Player;
using ABI_RC.Systems.PlayerColors;
using NAK.CleanPlates.UI;
using NAK.CleanPlates.Helpers;
using Object = UnityEngine.Object;
using UnityEngine;

namespace NAK.CleanPlates;

// Camera indicator subsystem.
public static partial class PlateManager
{
    private const string CameraVisibilitySettingName = "NameplateCameraIndicatorCustomizationVisibility";

    // We only want the vertical offset off the wrapper.
    public static Vector3 LocalOffset = new(0f, 0.15f, 0f);
    public static float CameraFadeSpeed = 6f;
    public static float CameraLodBlendSpeed = 3f;
    public static float CameraScaleSmoothing = 4f;
    public static float CameraFullDetailDistance = 5f;
    public static float CameraHideDistance = 30f;
    public static float CameraNearScale = 1f;
    public static float CameraFarScale = 1.4f;
    
    public static float CameraCollapsedMaxScale = 1.15f;
    public static float CameraMaxScale = 3.5f;

    // Tells us when the camera indicator is toggled, cause sadly native
    // there is no event for us to hook. >:(
    public class CameraPlateTracker : MonoBehaviour
    {
        internal MiniNameplate Plate;
        internal PlayerBase Player;
        internal string Username;
        internal Vector3 BaseScale;
        internal float Alpha;
        internal float Blend;
        internal float Scale = -1f;
        internal Transform Transform;

        private void OnEnable()
        {
            Transform = transform;
            Entries.Add(this);
            ByPlayer[Player] = this;
        }

        private void OnDisable()
        {
            Entries.Remove(this);
            ByPlayer.Remove(Player);
            Alpha = 0f;
            Blend = 0f;
            Scale = -1f;
            Plate.SetState(0f, 0f);
        }
    }

    private static readonly List<CameraPlateTracker> Entries = new();
    private static readonly Dictionary<PlayerBase, CameraPlateTracker> ByPlayer = new();

    private static readonly LoopProfiler CameraProfiler = new("Cameras", trackDrawn: false);

    public static bool CameraProfile
    {
        get => CameraProfiler.Enabled;
        set => CameraProfiler.Enabled = value;
    }

    internal static void OnCameraPlateStart(CameraIndicatorPlate original)
    {
        PlayerBase player = original.playerBase;
        Transform wrapper = original.nameplateGameObjectWrapper.transform;

        // The local plate does not have player serialized, so we will fix the reference!
        // This is another native bug and all that prevents the local indicator from having
        // a nameplate native.
        if (!player) player = PlayerSetup.Instance;

        NameplateTheme.EnsureBuilt();
        GameObject go = Object.Instantiate(CleanPlatesMod.CleanPlatesMiniPrefab, wrapper.parent, false);
        go.SetActive(false); // so the CameraPlateTracker registers with everything filled in

        go.transform.SetLocalPositionAndRotation(LocalOffset, Quaternion.identity);

        MiniNameplate plate = go.GetComponent<MiniNameplate>();
        plate.SetState(0f, 0f);

        CameraPlateTracker tracker = go.AddComponent<CameraPlateTracker>();
        tracker.Plate = plate;
        tracker.Player = player;
        tracker.Username = player.PlayerUsername;
        tracker.BaseScale = go.transform.localScale;

        Rebind(tracker, player.IsLocalPlayer
            ? PlayerColorsManager.CurrentColors
            : PlayerColorsManager.GetPlayerColors(player.PlayerId));

        go.SetActive(true);
    }

    private static void TickCameraIndicators()
    {
        float dt = _frame.DeltaTime;
        bool inspecting = _frame.Inspecting;
        int count = Entries.Count;
        if (count == 0) return;
        CameraProfiler.Begin();

        bool show = _frame.ShowCamera;
        float scaleLerp = 1f - Mathf.Exp(-CameraScaleSmoothing * dt);
        float fovScale = FovScale.Current;

        foreach (CameraPlateTracker e in Entries)
        {
            Transform plateTransform = e.Transform;
            float distance = Vector3.Distance(_frame.ObserverPosition, plateTransform.position);

            bool visible = show && (inspecting || distance <= CameraHideDistance);
            float alpha = Mathf.MoveTowards(e.Alpha, visible ? 1f : 0f, CameraFadeSpeed * dt);
            float blend = Mathf.MoveTowards(e.Blend, distance < CameraFullDetailDistance ? 1f : 0f, CameraLodBlendSpeed * dt);

            if (blend != e.Blend)
            {
                e.Alpha = alpha;
                e.Blend = blend;
                e.Plate.SetState(alpha, blend);
            }
            else if (alpha != e.Alpha)
            {
                e.Alpha = alpha;
                e.Plate.SetAlpha(alpha);
            }
            
            float growth = Mathf.Clamp(distance * fovScale / CameraFullDetailDistance,
                1f, inspecting ? CameraMaxScale : CameraCollapsedMaxScale);
            float targetScale = growth * Mathf.Lerp(CameraFarScale, CameraNearScale, blend);

            float scale = e.Scale < 0f ? targetScale : Mathf.Lerp(e.Scale, targetScale, scaleLerp);
            if (Mathf.Abs(scale - e.Scale) > 0.001f)
            {
                e.Scale = scale;
                plateTransform.localScale = e.BaseScale * scale;
            }
        }

        CameraProfiler.End(0, count);
    }

    internal static void SetPlayerColors(PlayerBase player, PlayerColors colors)
    {
        if (player != null && ByPlayer.TryGetValue(player, out CameraPlateTracker tracker))
            Rebind(tracker, colors);
    }

    internal static void RebindCameraIndicators()
    {
        foreach (CameraPlateTracker e in Entries)
            Rebind(e, e.Player.IsLocalPlayer
                ? PlayerColorsManager.CurrentColors
                : PlayerColorsManager.GetPlayerColors(e.Player.PlayerId));
    }

    private static void Rebind(CameraPlateTracker tracker, PlayerColors colors)
    {
        MiniNameplate plate = tracker.Plate;
        plate.Bind(tracker.Username, colors.PrimaryColor, colors.SecondaryColor);
        plate.SetBackgroundOpacity(NameplateView.BackgroundOpacity);
    }

    internal static void RefreshSettings()
    {
        float opacity = NameplateView.BackgroundOpacity;
        foreach (CameraPlateTracker e in Entries) e.Plate.SetBackgroundOpacity(opacity);
    }
}