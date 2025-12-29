/**
    MIT License

    Copyright (c) 2021 Kinsi, NotAKidoS

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
**/

using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.InputManagement;
using MelonLoader;
using UnityEngine;
using Valve.VR;

namespace NAK.SmootherRay;

public class SmootherRayer : MonoBehaviour
{
    #region Variables

    public ControllerRay ray;

    //settings
    private bool _isEnabled;
    private bool _menuOnly;
    private float _positionSmoothingValue;
    private float _rotationSmoothingValue;
    private float _smallMovementThresholdAngle;

    //internal
    private Vector3 _smoothedPosition = Vector3.zero;
    private Quaternion _smoothedRotation = Quaternion.identity;
    private float _angleVelocitySnap = 1f;

    //native & trackedcontrollerfix stuff
    private SteamVR_Behaviour_Pose _behaviourPose;
    private SteamVR_TrackedObject _trackedObject;
    private SteamVR_Events.Action _newPosesAction;

    #endregion

    #region Unity Methods

    private void Start()
    {
        // Native ChilloutVR - OpenVR
        if (TryGetComponent(out _behaviourPose))
            UpdateTransformUpdatedEvent(true);

        // TrackedControllerFix support - OpenVR
        if (TryGetComponent(out _trackedObject))
        {
            _newPosesAction = SteamVR_Events.NewPosesAppliedAction(OnAppliedPoses);
            UpdatePosesAction(true);
        }

        foreach (MelonPreferences_Entry setting in SmootherRayMod.Category.Entries)
            setting.OnEntryValueChangedUntyped.Subscribe(OnUpdateSettings);
        
        MetaPort.Instance.settings.settingBoolChanged.AddListener(OnSettingsBoolChanged);

        OnUpdateSettings(null, null);
    }

    private void OnEnable()
    {
        Transform controller = transform;
        _smoothedPosition = controller.localPosition;
        _smoothedRotation = controller.localRotation;

        // desktopvrswitch support, start handles this for normal use
        UpdateTransformUpdatedEvent(true);
        UpdatePosesAction(true);
    }

    private void OnDisable()
    {
        Transform controller = transform;
        _smoothedPosition = controller.localPosition;
        _smoothedRotation = controller.localRotation;

        // desktopvrswitch support, normal use wont run this
        UpdateTransformUpdatedEvent(false);
        UpdatePosesAction(false);
    }

    #endregion

    #region Private Methods

    private void UpdatePosesAction(bool enable)
    {
        if (enable && CheckVR.Instance.forceOpenXr)
            return;

        if (_trackedObject != null && _newPosesAction != null)
            _newPosesAction.enabled = enable;
    }

    private void UpdateTransformUpdatedEvent(bool enable)
    {
        if (enable && CheckVR.Instance.forceOpenXr)
            return;

        if (_behaviourPose == null)
            return;

        if (enable)
            _behaviourPose.onTransformUpdatedEvent += OnTransformUpdated;
        else
            _behaviourPose.onTransformUpdatedEvent -= OnTransformUpdated;
    }

    private void OnUpdateSettings(object arg1, object arg2)
    {
        _isEnabled = SmootherRayMod.EntryEnabled.Value;
        _menuOnly = SmootherRayMod.EntryMenuOnly.Value;
        _smallMovementThresholdAngle = SmootherRayMod.EntrySmallMovementThresholdAngle.Value;
        
        // dont let value hit 0, itll freeze controllers
        _positionSmoothingValue = Mathf.Max(20f - Mathf.Clamp(SmootherRayMod.EntryPositionSmoothing.Value, 0f, 20f), 0.1f);
        _rotationSmoothingValue = Mathf.Max(20f - Mathf.Clamp(SmootherRayMod.EntryRotationSmoothing.Value, 0f, 20f), 0.1f);
        
        if (!_isEnabled)
            return; // only care about setting being enabled
        
        if (MetaPort.Instance.settings.GetSettingsBool("ControlSmoothRaycast")) 
            return; // disable saved setting once
        
        SmootherRayMod.Logger.Msg("Built-in SmootherRay setting found to be enabled. Disabling built-in SmootherRay implementation in favor of modded implementation.");
        MetaPort.Instance.settings.SetSettingsBool("ControlSmoothRaycast", false);
        // ViewManager.SetGameSettingBool("ControlSmoothRaycast", false); 
        // ^ did you know the game doesn't even use this method native...
    }
    
    private void OnSettingsBoolChanged(string key, bool value)
    {
        if (key != "ControlSmoothRaycast") 
            return; // only care about SmoothRaycast setting
        
        if (!value) 
            return; // only care about setting being enabled
        
        _isEnabled = false; // ensure modded SmootherRay is disabled
        
        if (!SmootherRayMod.EntryEnabled.Value) return; // disable saved setting once
        SmootherRayMod.Logger.Msg("Modded SmootherRay found to be enabled. Disabling modded SmootherRay implementation in favor of built-in implementation.");
        SmootherRayMod.EntryEnabled.Value = false;
    }

    private void OnAppliedPoses()
    {
        SmoothTransform();
    }

    private void OnTransformUpdated(SteamVR_Behaviour_Pose pose, SteamVR_Input_Sources inputSource)
    {
        SmoothTransform();
    }

    private void SmoothTransform()
    {
        Transform controller = transform;
        if (!CanSmoothRay())
        {
            _smoothedPosition = controller.localPosition;
            _smoothedRotation = controller.localRotation;
        }
        else
        {
            var angDiff = Quaternion.Angle(_smoothedRotation, controller.localRotation);
            _angleVelocitySnap = Mathf.Min(_angleVelocitySnap + angDiff, 90f);

            var snapMulti = Mathf.Clamp(_angleVelocitySnap / _smallMovementThresholdAngle, 0.1f, 2.5f);

            if (_angleVelocitySnap > 0.1f)
                _angleVelocitySnap -= Mathf.Max(0.4f, _angleVelocitySnap / 1.7f);

            if (_positionSmoothingValue < 20f)
            {
                _smoothedPosition = Vector3.Lerp(_smoothedPosition, controller.localPosition,
                    _positionSmoothingValue * Time.deltaTime * snapMulti);
                controller.localPosition = _smoothedPosition;
            }

            if (_rotationSmoothingValue < 20f)
            {
                _smoothedRotation = Quaternion.Lerp(_smoothedRotation, controller.localRotation,
                    _rotationSmoothingValue * Time.deltaTime * snapMulti);
                controller.localRotation = _smoothedRotation;
            }
        }
    }

    private bool CanSmoothRay()
    {
        bool canSmoothRay = _isEnabled && ray.lineRenderer != null && ray.lineRenderer.enabled;
        
        if (_menuOnly)
        {
            switch (ray.hand)
            {
                case CVRHand.Left when !CVRInputManager.Instance.leftControllerPointingMenu:
                case CVRHand.Right when !CVRInputManager.Instance.rightControllerPointingMenu:
                    canSmoothRay = false;
                    break;
            }
        }
        
        if (SmootherRayMod.EntrySmoothWhenHoldingPickup.Value
            && ray.grabbedObject)
            canSmoothRay = true;
        
        return canSmoothRay;
    }
    
    #endregion Private Methods
}