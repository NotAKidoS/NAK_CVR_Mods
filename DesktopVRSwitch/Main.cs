
using MelonLoader;
using NAK.DesktopVRSwitch.VRModeTrackers;
using UnityEngine;

/**
    I know the TryCatchHell thing might be a bit exessive, but it is
    built so if a user that happens to have access to a build I do not,
    I will have a good idea of what broke and where, and what to look out
    for when updates/experimentals release. (which has happened a few times)

    It is also just in case other mods break or tweak functionality that
    could fuck with switching. Or if they try to detect switching and break...

    The VRModeSwitchTracker system is also built so I can easily & quickly make adjustments to
    components that may or may not change between builds without breaking the rest of the mod.
**/

namespace NAK.DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(DesktopVRSwitch));

    public static readonly MelonPreferences_Entry<bool> EntryEnterCalibrationOnSwitch =
        Category.CreateEntry("Enter Calibration on Switch", true, description: "Should you automatically be placed into calibration after switch if FBT is available? Overridden by Save Calibration IK setting.");

    public static readonly MelonPreferences_Entry<bool> EntryUseTransitionOnSwitch =
        Category.CreateEntry("Use Transition on Switch", true, description: "Should the world transition play on VRMode switch?");

    public static readonly MelonPreferences_Entry<bool> EntryRenderVRGameView =
        Category.CreateEntry("Render VR Game View", true, description: "Should the VR view be displayed in the game window after VRMode switch?");

    public static readonly MelonPreferences_Entry<bool> EntrySwitchToDesktopOnExit =
        Category.CreateEntry("Switch to Desktop on SteamVR Exit", true, description: "Should the game switch to Desktop when SteamVR quits?");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        RegisterVRModeTrackers();

        // main manager
        ApplyPatches(typeof(HarmonyPatches.CheckVRPatches));
        // nameplate fixes
        ApplyPatches(typeof(HarmonyPatches.CameraFacingObjectPatches));
        // pickup fixes
        ApplyPatches(typeof(HarmonyPatches.CVRPickupObjectPatches));
        // lazy fix to reset iksystem
        ApplyPatches(typeof(HarmonyPatches.IKSystemPatches));
        // post processing fixes
        ApplyPatches(typeof(HarmonyPatches.CVRWorldPatches));
        // cohtml gamepad handling nuke
        ApplyPatches(typeof(HarmonyPatches.CohtmlUISystemPatches));

        // prevent steamvr behaviour from closing game
        ApplyPatches(typeof(HarmonyPatches.SteamVRBehaviourPatches));
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F6) && Input.GetKey(KeyCode.LeftControl))
        {
            VRModeSwitchManager.Instance?.AttemptSwitch();
        }
    }

    void RegisterVRModeTrackers()
    {
        // Core trackers
        VRModeSwitchManager.RegisterVRModeTracker(new CheckVRTracker());
        VRModeSwitchManager.RegisterVRModeTracker(new MetaPortTracker());

        // HUD trackers
        VRModeSwitchManager.RegisterVRModeTracker(new CohtmlHudTracker());
        VRModeSwitchManager.RegisterVRModeTracker(new HudOperationsTracker());

        // Player trackers
        VRModeSwitchManager.RegisterVRModeTracker(new PlayerSetupTracker());
        VRModeSwitchManager.RegisterVRModeTracker(new MovementSystemTracker());
        VRModeSwitchManager.RegisterVRModeTracker(new IKSystemTracker());

        // Menu trackers
        VRModeSwitchManager.RegisterVRModeTracker(new CVR_MenuManagerTracker());
        VRModeSwitchManager.RegisterVRModeTracker(new ViewManagerTracker());

        // Interaction trackers
        VRModeSwitchManager.RegisterVRModeTracker(new CVRInputManagerTracker());
        VRModeSwitchManager.RegisterVRModeTracker(new CVR_InteractableManagerTracker());
        VRModeSwitchManager.RegisterVRModeTracker(new CVRGestureRecognizerTracker());

        // Portable camera tracker
        VRModeSwitchManager.RegisterVRModeTracker(new PortableCameraTracker());
    }

    void ApplyPatches(Type type)
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
}