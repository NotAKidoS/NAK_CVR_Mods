﻿using ABI.CCK.Components;
using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using NAK.PickupPushPull.InputModules.Info;
using System.Reflection;
using ABI_RC.Systems.InputManagement;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;

namespace NAK.PickupPushPull.InputModules;

public class PickupPushPull_Module : CVRInputModule
{
    public Vector2 objectRotation = Vector2.zero;

    //Global settings
    public float EntryPushPullSpeed = 100f;
    public float EntryRotationSpeed = 200f;
    public bool EntryEnableRotation = false;

    //Desktop settings
    public bool Desktop_UseZoomForRotate = true;

    //VR settings
    public BindingOptionsVR.BindHand VR_RotateHand = BindingOptionsVR.BindHand.LeftHand;
    public BindingOptionsVR.BindingOptions VR_RotateBind = BindingOptionsVR.BindingOptions.ButtonATouch;
    public SteamVR_Action_Boolean VR_RotateBind_Boolean;

    //Local stuff
    private ControllerRay desktopControllerRay;
    private float deadzoneRightValue;
    private bool controlGamepadEnabled;

    //SteamVR Input
    private SteamVR_Action_Vector2 vrLookAction;
    private SteamVR_Action_Boolean steamVrTriggerTouch;
    private SteamVR_Action_Boolean steamVrGripTouch;
    private SteamVR_Action_Boolean steamVrStickTouch;
    private SteamVR_Action_Boolean steamVrButtonATouch;
    private SteamVR_Action_Boolean steamVrButtonBTouch;

    public new void Start()
    {
        _inputManager = CVRInputManager.Instance;
        base.Start();

        //Get desktop controller ray
        desktopControllerRay = PlayerSetup.Instance.desktopCamera.GetComponent<ControllerRay>();

        //Touch Controllers
        InputModuleSteamVR inputModuleSteamVR = GetComponent<InputModuleSteamVR>();
        vrLookAction = (SteamVR_Action_Vector2)EI_SteamVR_Info.im_vrLookAction.GetValue(inputModuleSteamVR);
        steamVrTriggerTouch = (SteamVR_Action_Boolean)EI_SteamVR_Info.im_steamVrTriggerTouch.GetValue(inputModuleSteamVR);
        steamVrGripTouch = (SteamVR_Action_Boolean)EI_SteamVR_Info.im_steamVrGripTouch.GetValue(inputModuleSteamVR);
        steamVrStickTouch = (SteamVR_Action_Boolean)EI_SteamVR_Info.im_steamVrStickTouch.GetValue(inputModuleSteamVR);
        steamVrButtonATouch = (SteamVR_Action_Boolean)EI_SteamVR_Info.im_steamVrButtonATouch.GetValue(inputModuleSteamVR);
        steamVrButtonBTouch = (SteamVR_Action_Boolean)EI_SteamVR_Info.im_steamVrButtonBTouch.GetValue(inputModuleSteamVR);

        controlGamepadEnabled = (bool)MetaPort.Instance.settings.GetSettingsBool("ControlEnableGamepad", false);
        MetaPort.Instance.settings.settingBoolChanged.AddListener(new UnityAction<string, bool>(SettingsBoolChanged));

        deadzoneRightValue = (float)MetaPort.Instance.settings.GetSettingInt("ControlDeadZoneRight", 0) / 100f;
        MetaPort.Instance.settings.settingIntChanged.AddListener(new UnityAction<string, int>(SettingsIntChanged));

        UpdateVRBinding();
    }

    private void SettingsBoolChanged(string name, bool value)
    {
        if (name == "ControlEnableGamepad")
            controlGamepadEnabled = value;
    }

    private void SettingsIntChanged(string name, int value)
    {
        if (name == "ControlDeadZoneRight")
            deadzoneRightValue = (float)value / 100f;
    }

    public void UpdateVRBinding()
    {
        switch (VR_RotateBind)
        {
            case BindingOptionsVR.BindingOptions.ButtonATouch:
                VR_RotateBind_Boolean = steamVrButtonATouch;
                break;
            case BindingOptionsVR.BindingOptions.ButtonBTouch:
                VR_RotateBind_Boolean = steamVrButtonBTouch;
                break;
            case BindingOptionsVR.BindingOptions.StickTouch:
                VR_RotateBind_Boolean = steamVrStickTouch;
                break;
            case BindingOptionsVR.BindingOptions.TriggerTouch:
                VR_RotateBind_Boolean = steamVrTriggerTouch;
                break;
            case BindingOptionsVR.BindingOptions.GripTouch:
                VR_RotateBind_Boolean = steamVrGripTouch;
                break;
            default:
                break;
        }
    }

    //this will run while menu is being hovered
    public override void UpdateImportantInput()
    {
        objectRotation = Vector2.zero;
    }

    //this will only run outside of menus
    public override void UpdateInput()
    {
        objectRotation = Vector2.zero;

        CVRPickupObject desktopObject = (CVRPickupObject)_grabbedObject.GetValue(desktopControllerRay);
        if (desktopObject != null && desktopObject.gripType == CVRPickupObject.GripType.Free)
        {
            //Desktop Input
            DoDesktopInput();
            //Gamepad Input
            DoGamepadInput();
        }

        //VR Input
        if (!MetaPort.Instance.isUsingVr) return;
        DoSteamVRInput();
    }

    private void DoDesktopInput()
    {
        if (!Desktop_UseZoomForRotate) return;

        //mouse rotation when zoomed
        if (EntryEnableRotation && _inputManager.zoom)
        {
            objectRotation.x += EntryRotationSpeed * _inputManager.rawLookVector.x;
            objectRotation.y += EntryRotationSpeed * _inputManager.rawLookVector.y * -1;
            _inputManager.lookVector = Vector2.zero;
            _inputManager.zoom = false;
            return;
        }
    }

    private void DoGamepadInput()
    {
        if (!controlGamepadEnabled) return;

        //not sure how to make settings for this
        bool button1 = Input.GetButton("Controller Left Button");
        bool button2 = Input.GetButton("Controller Right Button");

        if (button1 || button2)
        {
            //Rotation
            if (EntryEnableRotation && button2)
            {
                objectRotation.x += EntryRotationSpeed * _inputManager.rawLookVector.x;
                objectRotation.y += EntryRotationSpeed * _inputManager.rawLookVector.y * -1;
                _inputManager.lookVector = Vector2.zero;
                return;
            }

            _inputManager.objectPushPull += _inputManager.rawLookVector.y * EntryPushPullSpeed * Time.deltaTime;
            _inputManager.lookVector = Vector2.zero;
        }
    }

    private void DoSteamVRInput()
    {
        CVRPickupObject leftObject = PlayerSetup.Instance.leftRay.grabbedObject;
        CVRPickupObject rightObject = PlayerSetup.Instance.rightRay.grabbedObject;
        if (leftObject == null && rightObject == null) 
            return;

        bool canRotate = (leftObject != null && leftObject.gripType == CVRPickupObject.GripType.Free) ||
                       (rightObject != null && rightObject.gripType == CVRPickupObject.GripType.Free);

        if (EntryEnableRotation && canRotate && VR_RotateBind_Boolean.GetState((SteamVR_Input_Sources)VR_RotateHand))
        {
            Vector2 rawLookVector = new Vector2(CVRTools.AxisDeadZone(vrLookAction.GetAxis(SteamVR_Input_Sources.Any).x, deadzoneRightValue, true),
                                                CVRTools.AxisDeadZone(vrLookAction.GetAxis(SteamVR_Input_Sources.Any).y, deadzoneRightValue, true));

            objectRotation.x += EntryRotationSpeed * rawLookVector.x;
            objectRotation.y += EntryRotationSpeed * rawLookVector.y * -1;

            _inputManager.lookVector = Vector2.zero;
        }
    }

}