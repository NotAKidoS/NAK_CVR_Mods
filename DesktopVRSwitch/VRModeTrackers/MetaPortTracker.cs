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

    private void OnPostSwitch(bool intoVR)
    {
        MetaPort _metaPort = MetaPort.Instance;
        if (_metaPort == null)
        {
            DesktopVRSwitch.Logger.Error("Error while getting MetaPort!");
            return;
        }
        DesktopVRSwitch.Logger.Msg($"Setting MetaPort isUsingVr to {intoVR}.");

        // Main thing most of the game checks for if using VR
        _metaPort.isUsingVr = intoVR;

        // replace
        UpdateRichPresence(_metaPort);
        ResetSteamVROverrides(intoVR);
    }

    private void UpdateRichPresence(MetaPort _metaPort)
    {
        // Hacky way of updating rich presence
        if (_metaPort.settings.GetSettingsBool("ImplementationRichPresenceDiscordEnabled", true))
        {
            DesktopVRSwitch.Logger.Msg("Forcing Discord Rich Presence update.");
            _metaPort.settings.SetSettingsBool("ImplementationRichPresenceDiscordEnabled", false);
            _metaPort.settings.SetSettingsBool("ImplementationRichPresenceDiscordEnabled", true);
        }
        if (_metaPort.settings.GetSettingsBool("ImplementationRichPresenceSteamEnabled", true))
        {
            DesktopVRSwitch.Logger.Msg("Forcing Steam Rich Presence update.");
            _metaPort.settings.SetSettingsBool("ImplementationRichPresenceSteamEnabled", false);
            _metaPort.settings.SetSettingsBool("ImplementationRichPresenceSteamEnabled", true);
        }
    }

    private void ResetSteamVROverrides(bool intoVR)
    {
        if (intoVR)
        {
            // Testing
            XRSettings.eyeTextureResolutionScale = 1;
            XRSettings.gameViewRenderMode = DesktopVRSwitch.EntryRenderVRGameView.Value ? GameViewRenderMode.LeftEye : GameViewRenderMode.None;
            SteamVR_Settings.instance.pauseGameWhenDashboardVisible = false;

            if (MetaPort.Instance.settings.GetSettingsBool("InteractionTobiiEyeTracking", false))
            {
                MetaPort.Instance.TobiiXrInitializer.Initialize();
            }

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