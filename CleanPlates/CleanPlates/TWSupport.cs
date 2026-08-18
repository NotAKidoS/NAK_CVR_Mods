using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.CleanPlates;

internal static class TWSupport
{
    private const string StatusObjectName = "NameplateStatus(Clone)";

    public static bool Enabled;
    public static int Anchor = ThirdpartySupport.CornerRight;

    public static float ReferenceBodyHeight = 90f;
    public static Vector3 LocalPosition = new(50f, 0f, 0f);
    public static Vector3 LocalScale = new(0.8f, 0.8f, 0.8f);

    internal static void Init()
    {
        ThirdpartySupport.PlateAttached += OnPlateAttached;
        ThirdpartySupport.PlateDetached += OnPlateDetached;
    }

    private static void OnPlateAttached(PlayerBase player, GameObject plate)
    {
        if (!Enabled) return;

        OverheadController overhead = player.overheadController;
        if (overhead == null || overhead.canvas == null) return;

        Transform status = overhead.canvas.transform.Find(StatusObjectName);
        if (status == null)
        {
            CleanPlatesMod.Logger.Msg($"Did not find TW plate!");
            return;
        }

        // Unregister original overhead as the icon will inherit our visibility rules.
        foreach (MonoBehaviour behaviour in status.GetComponents<MonoBehaviour>())
        {
            if (behaviour is not IOverhead overheadComponent) continue;
            overhead._overheads.Remove(overheadComponent);
            behaviour.enabled = false;
        }

        Transform target = ThirdpartySupport.GetPlateCorner(player, Anchor) ?? plate.transform;
        float fit = ThirdpartySupport.GetPlateBodyHeight(player) / ReferenceBodyHeight;

        status.SetParent(target, false);
        status.SetLocalPositionAndRotation(LocalPosition * fit, Quaternion.identity);
        status.localScale = LocalScale * fit;

        // Breaks the masking I guess, only doing to set the shader properties.
        ThirdpartySupport.ApplyPlateTheme(status, player.IsLocalPlayer);
    }

    // Placing plate back on root to account for style switching.
    private static void OnPlateDetached(PlayerBase player, GameObject plate)
    {
        if (player == null || plate == null) return;

        OverheadController overhead = player.overheadController;
        if (overhead == null || overhead.canvas == null) return;

        foreach (Transform child in plate.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != StatusObjectName) continue;

            child.SetParent(overhead.canvas.transform, false);
            
            foreach (MonoBehaviour behaviour in child.GetComponents<MonoBehaviour>())
            {
                if (behaviour is not IOverhead overheadComponent) continue;
                if (!overhead._overheads.Contains(overheadComponent))
                    overhead._overheads.Add(overheadComponent);
                behaviour.enabled = true;
            }
        }
    }
}