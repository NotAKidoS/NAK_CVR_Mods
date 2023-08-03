
using MelonLoader;
using NAK.DesktopVRSwitch.VRModeTrackers;
using UnityEngine;

namespace NAK.DesktopVRSwitch;

public class DesktopVRSwitch : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
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
        
        // fuck you
        ApplyPatches(typeof(HarmonyPatches.CohtmlUISystemPatches));

        // prevent steamvr behaviour from closing game
        ApplyPatches(typeof(HarmonyPatches.SteamVRBehaviourPatches));
        
        InitializeIntegration("BTKUILib", Integrations.BTKUIAddon.Initialize);
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F6) && Input.GetKey(KeyCode.LeftControl))
        {
            VRModeSwitchManager.Instance?.AttemptSwitch();
        }
    }

    private static void RegisterVRModeTrackers()
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

        // CVRWorld tracker - Must come after PlayerSetupTracker
        VRModeSwitchManager.RegisterVRModeTracker(new CVRWorldTracker());
    }
    
    private static void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        Logger.Msg($"Initializing {modName} integration.");
        integrationAction.Invoke();
    }

    private void ApplyPatches(Type type)
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