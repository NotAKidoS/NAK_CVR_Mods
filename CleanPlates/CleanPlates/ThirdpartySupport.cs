using ABI_RC.Core.Player;
using JetBrains.Annotations;
using NAK.CleanPlates.UI;
using TMPro;
using UnityEngine;

namespace NAK.CleanPlates;

// Example:
//   Type api = Type.GetType("NAK.CleanPlates.ThirdpartySupport, CleanPlates");
//   api.GetEvent("PlateAttached").AddEventHandler(null, handler);
//   api.GetMethod("GetPlateCorner").Invoke(null, new object[] { player, 7 });

[PublicAPI]
public static class ThirdpartySupport
{
    public const int CornerTopLeft = 0;
    public const int CornerTopRight = 1;
    public const int CornerBottomLeft = 2;
    public const int CornerBottomRight = 3;
    public const int CornerTop = 4;
    public const int CornerBottom = 5;
    public const int CornerLeft = 6;
    public const int CornerRight = 7;

    // A plate now exists for this player (player join or plate rebuild).
    public static event Action<PlayerBase, GameObject> PlateAttached;

    // The plate is about to die (player leave or plater rebuild).
    public static event Action<PlayerBase, GameObject> PlateDetached;

    // The plate has crossed between its near and far layouts.
    public static event Action<PlayerBase, bool> PlateCollapsedChanged;

    public static GameObject GetPlate(PlayerBase player)
        => PlateManager.TryGetPlate(player, out NameplateView plate) ? plate.gameObject : null;

    // Anchors on every corner of the plate you can parent onto. Null for a
    // corner outside the constants above.
    public static Transform GetPlateCorner(PlayerBase player, int corner)
        => PlateManager.TryGetPlate(player, out NameplateView plate)
           && plate.TryGetCornerAnchor((NameplateCorner)corner, out RectTransform anchor)
            ? anchor
            : null;

    // Plate local units, the same ones the anchors are laid out in. Multiply by
    // the plate transform lossyScale for world size.
    public static float GetPlateBodyWidth(PlayerBase player)
        => PlateManager.TryGetPlate(player, out NameplateView plate) ? plate.BodyWidth : 0f;
    public static float GetPlateBodyHeight(PlayerBase player)
        => PlateManager.TryGetPlate(player, out NameplateView plate) ? plate.BodyHeight : 0f;

    // How far the plate reaches below its own origin, the far name hangs well
    // under the body so the body alone is nowhere near the real bottom edge.
    public static float GetPlateBottomExtent(PlayerBase player)
        => PlateManager.TryGetPlate(player, out NameplateView plate) ? plate.BottomExtent : 0f;

    // Material replaces all graphics & TMP with the same as the plates.
    // Set localPlayer so the local plate in first-person is scaled properly.
    public static void ApplyPlateTheme(Transform root, bool localPlayer)
        => NameplateTheme.Apply(root, localPlayer);

    // Can use as reference to steal properties or clone from.
    // Please do not modify these, you'll eat my plates...
    public static Material GetPlateGraphicMaterial(bool localPlayer)
        => NameplateTheme.GraphicMaterialFor(localPlayer);
    public static Material GetPlateTextMaterial(bool localPlayer)
        => NameplateTheme.TextMaterialFor(localPlayer);
    public static TMP_FontAsset GetPlateFont() => NameplateTheme.Font;

    #region Internal

    internal static void RaisePlateAttached(PlayerBase player, NameplateView plate)
        => Raise(PlateAttached, player, plate, nameof(PlateAttached));
    internal static void RaisePlateDetached(PlayerBase player, NameplateView plate)
        => Raise(PlateDetached, player, plate, nameof(PlateDetached));

    internal static void RaisePlateCollapsedChanged(PlayerBase player, bool collapsed)
    {
        if (PlateCollapsedChanged == null) return;
        try
        {
            PlateCollapsedChanged(player, collapsed);
        }
        catch (Exception e)
        {
            CleanPlatesMod.Logger.Warning($"A {nameof(PlateCollapsedChanged)} listener threw: {e}");
        }
    }

    private static void Raise(Action<PlayerBase, GameObject> listeners,
        PlayerBase player, NameplateView plate, string name)
    {
        if (listeners == null || plate == null) return;
        try
        {
            listeners(player, plate.gameObject);
        }
        catch (Exception e)
        {
            CleanPlatesMod.Logger.Warning($"A {name} listener threw: {e}");
        }
    }

    #endregion Internal
}