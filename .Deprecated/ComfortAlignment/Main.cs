using System.Reflection;
using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.Movement;
using ABI_RC.Systems.PersonalPen;
using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using ABI_RC.Systems.XRManagement;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.ComfortAlignment;

public class ComfortAlignmentMod : MelonMod
{
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(ComfortAlignment));

    private static readonly MelonPreferences_Entry<bool> HiddenAcceptedMotionSicknessWarning =
        Category.CreateEntry(
            identifier: "accepted_warning",
            false,
            display_name: "Motion Sickness Warning",
            description: string.Empty,
            is_hidden: true);
    
    private static readonly MelonPreferences_Entry<bool> EntrySnapAlignment =
        Category.CreateEntry(
            identifier: "snap_alignment",
            false,
            display_name: "Snap Alignment",
            description: "Should the alignment be locked to 90 degrees.");
    
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(PenManager).GetMethod(nameof(PenManager.SetupUILib),
                BindingFlags.Public | BindingFlags.Static),
            postfix: new HarmonyMethod(typeof(ComfortAlignmentMod).GetMethod(nameof(OnSetupUILib),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        XRDeviceEvents.OnPostXRModeSwitch.AddListener(OnPostXRModeSwitch);
    }

    private static Category _category;
    private static ToggleButton _enableToggle;
    private static Button _alignToHorizon;
    private static Button _resetHorizon;
    private static ToggleButton _snappingToggle;
    
    private static void OnSetupUILib()
    {
        _category = QuickMenuAPI.CVRUtilsPage.AddCategory("Comfort Alignment", true, true);
        _category.Hidden = !MetaPort.Instance.isUsingVr;
        
        _enableToggle = _category.AddToggle("Accepted Warning", "Enables the comfort alignment feature.", HiddenAcceptedMotionSicknessWarning.Value);
        _enableToggle.OnValueUpdated += OnEnableToggled;

        _alignToHorizon = _category.AddButton("Align To Horizon", "Visibility", "Aligns your view to the horizon");
        _alignToHorizon.OnPress += () => RootLogic.RunInMainThread(AlignHorizon); // scheduled to apply early next frame, to avoid visual jitter
        _alignToHorizon.Disabled = !HiddenAcceptedMotionSicknessWarning.Value;

        _resetHorizon = _category.AddButton("Reset Horizon", "Visibility", "Resets your view");
        _resetHorizon.OnPress += () => RootLogic.RunInMainThread(ResetHorizon); // scheduled to apply early next frame, to avoid visual jitter
        _resetHorizon.Disabled = true;

        _snappingToggle = _category.AddToggle(EntrySnapAlignment.DisplayName, EntrySnapAlignment.Description, EntrySnapAlignment.Value);
        _snappingToggle.OnValueUpdated += (t) => EntrySnapAlignment.Value = t;
        _snappingToggle.Disabled = !HiddenAcceptedMotionSicknessWarning.Value;
    }
    
    private static void OnEnableToggled(bool enabled)
    {
        if (enabled)
        {
            QuickMenuAPI.ShowConfirm(
                "Motion Sickness Warning",
                "This feature adjusts your playspace orientation to align your view with the world horizon, improving accessibility when playing while reclining or lying down. It may cause motion sickness, dizziness, disorientation, or vertigo, particularly for users sensitive to motion or those new to virtual reality. Enable anyway?",
                () => SetFeatureEnabled(true), 
                () => _enableToggle.ToggleValue = false);
        }
        else
        {
            SetFeatureEnabled(false);
        }
    }

    private static void SetFeatureEnabled(bool enabled)
    {
        _alignToHorizon.Disabled = !enabled;
        _snappingToggle.Disabled = !enabled;
        HiddenAcceptedMotionSicknessWarning.Value = enabled;
        if (!enabled && !_resetHorizon.Disabled) ResetHorizon();
    }
    
    private static void OnPostXRModeSwitch(XRModeSwitchEventArgs events)
    {
        _category.Hidden = !events.IsUsingVr;
        if (events.WasUsingVr) ResetHorizonWithoutTeleport();
    }

    private static void AlignHorizon()
    {
        _resetHorizon.Disabled = false;

        // cache original pos to reapply after offsetting vr rig
        Vector3 playerPos = PlayerSetup.Instance.GetPlayerPosition();
        Quaternion playerRot = PlayerSetup.Instance.GetPlayerRotation();

        bool snapAlignment = EntrySnapAlignment.Value;

        // pivot point
        Vector3 camPos = PlayerSetup.Instance.vrCam.transform.position;

        // what we are aligning from
        Vector3 camUp = PlayerSetup.Instance.vrCam.transform.up;
        Vector3 camForward = PlayerSetup.Instance.vrCam.transform.forward;

        // what we're aligning to
        Vector3 playerUp = PlayerSetup.Instance.transform.up;
        Vector3 playerForward = PlayerSetup.Instance.GetPlayerForward();
        
        Quaternion correction = Quaternion.FromToRotation(camUp, playerUp);

        if (snapAlignment)
        {
            Vector3 refForward = Vector3.ProjectOnPlane(playerForward, playerUp);
            Vector3 flatForward = Vector3.ProjectOnPlane(correction * camForward, playerUp);
            if (refForward.sqrMagnitude > 1e-6f && flatForward.sqrMagnitude > 1e-6f)
            {
                float yaw = Vector3.SignedAngle(refForward, flatForward, playerUp);
                float deltaYaw = Mathf.DeltaAngle(yaw, Mathf.Round(yaw / 90f) * 90f);
                correction = Quaternion.AngleAxis(deltaYaw, playerUp) * correction;
            }
        }

        correction.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle != 0f) PlayerSetup.Instance.vrCameraRig.transform.RotateAround(camPos, axis, angle);

        // reapply player positions (internally re-centers and whatever)
        BetterBetterCharacterController.Instance.TeleportPlayerTo(playerPos, playerRot.eulerAngles, false, false);
    }

    private static void ResetHorizon()
    {
        _resetHorizon.Disabled = true;

        // cache original pos to reapply after offsetting vr rig
        Vector3 playerPos = PlayerSetup.Instance.GetPlayerPosition();
        Quaternion playerRot = PlayerSetup.Instance.GetPlayerRotation();
        
        PlayerSetup.Instance.vrCameraRig.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        
        // reapply player positions (internally re-centers and whatever)
        BetterBetterCharacterController.Instance.TeleportPlayerTo(playerPos, playerRot.eulerAngles, false, false);
    }

    private static void ResetHorizonWithoutTeleport()
    {
        _resetHorizon.Disabled = true;
        PlayerSetup.Instance.vrCameraRig.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
}