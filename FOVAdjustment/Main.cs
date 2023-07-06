using ABI.CCK.Components;
using ABI_RC.Core.Player;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace NAK.FOVAdjustment;

public class FOVAdjustment : MelonMod
{
    internal const string SettingsCategory = nameof(FOVAdjustment);

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(SettingsCategory);

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle FOVAdjustment entirely.");

    public static readonly MelonPreferences_Entry<float> EntryFOV =
        Category.CreateEntry("FOV", 60f, description: "Target Desktop FOV. This is ignored if the world specifies a custom FOV!");

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(CVR_DesktopCameraController).GetMethod(nameof(CVR_DesktopCameraController.UpdateFov)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(FOVAdjustment).GetMethod(nameof(OnUpdateFov_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
        );

        EntryEnabled.OnEntryValueChanged.Subscribe(OnEntryEnabledChanged);
        EntryFOV.OnEntryValueChanged.Subscribe(OnEntryFovChanged);
    }

    private void OnEntryEnabledChanged(bool oldValue, bool newValue)
    {
        UpdateDesktopCameraControllerFov(newValue ? EntryFOV.Value : 60f);
        CVR_DesktopCameraController.UpdateFov();
    }

    private void OnEntryFovChanged(float oldValue, float newValue)
    {
        if (!EntryEnabled.Value)
            return;

        UpdateDesktopCameraControllerFov(newValue);
        CVR_DesktopCameraController.UpdateFov();
    }

    private static void OnUpdateFov_Prefix()
    {
        if (!EntryEnabled.Value) 
            return;

        UpdateDesktopCameraControllerFov(EntryFOV.Value);
    }

    private static void UpdateDesktopCameraControllerFov(float value)
    {
        if (CVRWorld.Instance != null && Mathf.Approximately(CVRWorld.Instance.fov, 60f))
        {
            CVR_DesktopCameraController.defaultFov = Mathf.Clamp(value, 60f, 120f);
            CVR_DesktopCameraController.zoomFov = CVR_DesktopCameraController.defaultFov * 0.5f;
        }
    }
}