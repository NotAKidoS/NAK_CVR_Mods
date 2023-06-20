using ABI_RC.Core.Savior;

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
}