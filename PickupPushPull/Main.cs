using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Valve.VR;
using PickupPushPull.InputModules;

namespace PickupPushPull;

public class PickupPushPull : MelonMod
{
    private static MelonPreferences_Category Category_PickupPushPull;
    private static MelonPreferences_Entry<float> Setting_PushPullSpeed, Setting_RotateSpeed;
    private static MelonPreferences_Entry<bool> Setting_EnableRotation, Setting_Desktop_UseZoomForRotate;
    private static MelonPreferences_Entry<BindingOptionsVR.BindHand> Setting_VR_RotateHand;
    private static MelonPreferences_Entry<BindingOptionsVR.BindingOptions> Setting_VR_RotateBind;

    public override void OnInitializeMelon()
    {
        Category_PickupPushPull = MelonPreferences.CreateCategory(nameof(PickupPushPull));
        Category_PickupPushPull.SaveToFile(false);

        //Global settings
        Setting_PushPullSpeed = Category_PickupPushPull.CreateEntry("Push Pull Speed", 2f, description: "Up/down on right joystick for VR. Left buSettingr + Up/down on right joystick for Gamepad.");
        Setting_RotateSpeed = Category_PickupPushPull.CreateEntry<float>("Rotate Speed", 6f);
        Setting_EnableRotation = Category_PickupPushPull.CreateEntry<bool>("Enable Rotation", false, description: "Hold left trigger in VR or right buSettingr on Gamepad.");

        //Desktop settings
        Setting_Desktop_UseZoomForRotate = Category_PickupPushPull.CreateEntry<bool>("Desktop Use Zoom For Rotate", true, description: "Use zoom bind for rotation while a prop is held.");

        //VR settings
        Setting_VR_RotateHand = Category_PickupPushPull.CreateEntry("VR Hand", BindingOptionsVR.BindHand.LeftHand);

        //bruh
        foreach (var setting in Category_PickupPushPull.Entries)
        {
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        }

        //special setting
        Setting_VR_RotateBind = Category_PickupPushPull.CreateEntry("VR Binding", BindingOptionsVR.BindingOptions.ButtonATouch);
        Setting_VR_RotateBind.OnEntryValueChangedUntyped.Subscribe(OnUpdateVRBinding);

        MelonLoader.MelonCoroutines.Start(WaitForLocalPlayer());
    }


    System.Collections.IEnumerator WaitForLocalPlayer()
    {
        while (PlayerSetup.Instance == null)
            yield return null;

        CVRInputManager.Instance.gameObject.AddComponent<PickupPushPull_Module>();

        //update BlackoutController settings after it initializes
        while (PickupPushPull_Module.Instance == null)
            yield return null;

        UpdateAllSettings();
    }

    private void OnUpdateSettings(object arg1, object arg2) => UpdateAllSettings();
    private void OnUpdateVRBinding(object arg1, object arg2) => UpdateVRBinding();

    private void UpdateAllSettings()
    {
        if (!PickupPushPull_Module.Instance) return;

        //Global settings
        PickupPushPull_Module.Instance.Setting_PushPullSpeed = Setting_PushPullSpeed.Value * 50;
        PickupPushPull_Module.Instance.Setting_RotationSpeed = Setting_RotateSpeed.Value * 50;
        PickupPushPull_Module.Instance.Setting_EnableRotation = Setting_EnableRotation.Value;
        //Desktop settings
        PickupPushPull_Module.Instance.Desktop_UseZoomForRotate = Setting_Desktop_UseZoomForRotate.Value;
        //VR settings
        PickupPushPull_Module.Instance.VR_RotateHand = Setting_VR_RotateHand.Value;
    }

    private void UpdateVRBinding()
    {
        //VR special settings
        PickupPushPull_Module.Instance.VR_RotateBind = Setting_VR_RotateBind.Value;
        PickupPushPull_Module.Instance.UpdateVRBinding();
    }
}

public class BindingOptionsVR
{
    public enum BindHand
    {
        Any,
        LeftHand,
        RightHand
    }
    public enum BindingOptions
    {
        //Only oculus bindings have by default
        ButtonATouch,
        ButtonBTouch,
        TriggerTouch,
        //doesnt work?
        StickTouch,
        //Index only
        GripTouch
    }
}