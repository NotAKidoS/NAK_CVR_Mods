using ABI_RC.Core.Base;
using ABI_RC.Core.Player;
using MelonLoader;
using System.Reflection;
using System.Web.Services.Description;
using ABI_RC.Systems.Camera;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace NAK.NoDepthOnlyFlat;

public class NoDepthOnlyFlat : MelonMod
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(NoDepthOnlyFlat));

    public static readonly MelonPreferences_Entry<bool> EntryUseDepthOnPlayerCamera =
        Category.CreateEntry("Use Depth On Player Camera", false, description: "Toggle depth texture on player cameras.");

    public static readonly MelonPreferences_Entry<bool> EntryUseDepthOnPortableCamera =
        Category.CreateEntry("Use Depth On Portable Camera", false, description: "Toggle depth texture on portable camera.");

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.Start)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(NoDepthOnlyFlat).GetMethod(nameof(OnPlayerSetupStart_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );

        HarmonyInstance.Patch(
            typeof(PortableCamera).GetMethod(nameof(PortableCamera.Start)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(NoDepthOnlyFlat).GetMethod(nameof(OnPortableCameraStart_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        foreach (MelonPreferences_Entry setting in Category.Entries)
            setting.OnEntryValueChangedUntyped.Subscribe(OnSettingsChanged);
    }

    public static void OnSettingsChanged(object oldValue = null, object newValue = null)
    {
        SetPlayerSetupCameraDepthTextureMode(EntryUseDepthOnPlayerCamera.Value);
        SetPortableCameraDepthTextureMode(EntryUseDepthOnPortableCamera.Value);
    }

    private static void SetPlayerSetupCameraDepthTextureMode(bool useDepth)
    {
        if (PlayerSetup.Instance != null)
        {
            PlayerSetup.Instance.desktopCamera.GetComponent<Camera>().depthTextureMode = useDepth ? DepthTextureMode.Depth : DepthTextureMode.None;
            PlayerSetup.Instance.vrCamera.GetComponent<Camera>().depthTextureMode = useDepth ? DepthTextureMode.Depth : DepthTextureMode.None;
        }
    }

    private static void SetPortableCameraDepthTextureMode(bool useDepth)
    {
        if (PortableCamera.Instance != null)
        {
            PortableCamera.Instance._camera.depthTextureMode = useDepth ? DepthTextureMode.Depth : DepthTextureMode.None;
        }
    }

    // Lazy way to set settings on start

    private static void OnPlayerSetupStart_Postfix()
    {
        SetPlayerSetupCameraDepthTextureMode(EntryUseDepthOnPlayerCamera.Value);
    }

    private static void OnPortableCameraStart_Postfix()
    {
        SetPortableCameraDepthTextureMode(EntryUseDepthOnPortableCamera.Value);
    }
}