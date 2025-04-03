using ABI_RC.Core.Savior;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace NAK.DesktopVRSwitch.VRModeTrackers;

public class MetaPortTracker : VRModeTracker
{
    public override void TrackerInit()
    {
        VRModeSwitchManager.OnPostVRModeSwitch += OnPostSwitch;
    }

    public override void TrackerDestroy()
    {
        VRModeSwitchManager.OnPostVRModeSwitch -= OnPostSwitch;
    }

    private void OnPostSwitch(object sender, VRModeSwitchManager.VRModeEventArgs args)
    {
        DesktopVRSwitch.Logger.Msg($"Setting MetaPort isUsingVr to {args.IsUsingVr}.");

        // Main thing most of the game checks for if using VR
        MetaPort.Instance.isUsingVr = args.IsUsingVr;

        // replace
        UpdateRichPresence();
        ResetSteamVROverrides(args.IsUsingVr);
    }

    private void UpdateRichPresence()
    {
        // Hacky way of updating rich presence
        if (MetaPort.Instance.settings.GetSettingsBool("ImplementationRichPresenceDiscordEnabled", true))
        {
            DesktopVRSwitch.Logger.Msg("Forcing Discord Rich Presence update.");
            MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceDiscordEnabled", false);
            MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceDiscordEnabled", true);
        }
        if (MetaPort.Instance.settings.GetSettingsBool("ImplementationRichPresenceSteamEnabled", true))
        {
            DesktopVRSwitch.Logger.Msg("Forcing Steam Rich Presence update.");
            MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceSteamEnabled", false);
            MetaPort.Instance.settings.SetSettingsBool("ImplementationRichPresenceSteamEnabled", true);
        }
    }

    private void ResetSteamVROverrides(bool intoVR)
    {
        if (intoVR)
        {
            // Testing
            //XRSettings.gameViewRenderMode = DesktopVRSwitch.EntryRenderVRGameView.Value ? GameViewRenderMode.LeftEye : GameViewRenderMode.None;
            XRSettings.eyeTextureResolutionScale = 1; // unsure if will cause issues with FSR?
            SteamVR_Settings.instance.pauseGameWhenDashboardVisible = false;

            if (MetaPort.Instance.settings.GetSettingsBool("InteractionTobiiEyeTracking", false))
                MetaPort.Instance.TobiiXrInitializer.Initialize();

            return;
        }

        // Reset physics time to Desktop default
        Time.fixedDeltaTime = 0.02f;

        // Reset queued frames
        QualitySettings.maxQueuedFrames = 2;

        // Reset framerate target
        int graphicsFramerateTarget = MetaPort.Instance.settings.GetSettingInt("GraphicsFramerateTarget", 0);
        ABI_RC.Core.CVRTools.SetFramerateTarget(graphicsFramerateTarget);

        // Reset VSync setting
        bool graphicsVSync = MetaPort.Instance.settings.GetSettingsBool("GraphicsVSync", false);
        QualitySettings.vSyncCount = graphicsVSync ? 1 : 0;

        // Reset anti-aliasing
        int graphicsMsaaLevel = MetaPort.Instance.settings.GetSettingInt("GraphicsMsaaLevel", 0);
        QualitySettings.antiAliasing = graphicsMsaaLevel;

        // Won't do anything if not already running
        MetaPort.Instance.TobiiXrInitializer.DeInitialize();
    }
}