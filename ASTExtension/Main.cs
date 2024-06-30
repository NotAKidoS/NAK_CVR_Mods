using System.Collections;
using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.AnimatorManager;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.InputManagement;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using NAK.ASTExtension.Extensions;
using UnityEngine;

namespace NAK.ASTExtension;

public class ASTExtensionMod : MelonMod
{
    private static MelonLogger.Instance Logger;

    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(ASTExtension));

    private static readonly MelonPreferences_Entry<bool> EntryUseScaleGesture =
        Category.CreateEntry("use_scale_gesture", true,
            "Use Scale Gesture", "Use the scale gesture to adjust your avatar's height.");

    private static readonly MelonPreferences_Entry<bool> EntryUseCustomParameter =
        Category.CreateEntry("use_custom_parameter", false,
            "Use Custom Parameter", "Use a custom parameter to adjust your avatar's height.");

    private static readonly MelonPreferences_Entry<string> EntryCustomParameterName =
        Category.CreateEntry("custom_parameter_name", "AvatarScale",
            "Custom Parameter Name", "The name of the custom parameter to use for height adjustment.");

    private static readonly MelonPreferences_Entry<bool> EntryPersistentHeight =
        Category.CreateEntry("persistent_height", false,
            "Persistent Height", "Should the avatar height persist between avatar switches?");

    private static readonly MelonPreferences_Entry<bool> EntryPersistThroughRestart =
        Category.CreateEntry("persistent_height_through_restart", false,
            "Persist Through Restart", "Should the avatar height persist between game restarts?");

    private static readonly MelonPreferences_Entry<bool> EntryPersistFromUnsupported
        = Category.CreateEntry("persist_from_unsupported", false,
            "Persist From Unsupported", "Should the avatar height persist when the avatar is unsupported?");

    // stores the last avatar height as a melon pref
    private static readonly MelonPreferences_Entry<float> EntryHiddenAvatarHeight =
        Category.CreateEntry("hidden_avatar_height", -2f, is_hidden: true);

    private void InitializeSettings()
    {
        EntryUseCustomParameter.OnEntryValueChangedUntyped.Subscribe(OnUseCustomParameterChanged);
        EntryCustomParameterName.OnEntryValueChangedUntyped.Subscribe(OnCustomParameterNameChanged);
        OnUseCustomParameterChanged();
    }

    private void OnUseCustomParameterChanged(object oldValue = null, object newValue = null)
    {
        SetupCustomParameter();
    }

    private void OnCustomParameterNameChanged(object oldValue = null, object newValue = null)
    {
        SetupCustomParameter();
    }

    private void SetupCustomParameter()
    {
        // use custom parameter
        if (EntryUseCustomParameter.Value)
        {
            _parameterName = EntryCustomParameterName.Value;
            CalibrateCustomParameter();
            return;
        }

        // reset to default
        _parameterName = "AvatarScale";
        _minHeight = GlobalMinHeight;
        _maxHeight = GlobalMaxHeight;
    }

    #endregion Melon Preferences

    #region Melon Events

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        InitializeSettings();

        CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener(OnLocalAvatarLoad);
        CVRGameEventSystem.Avatar.OnLocalAvatarClear.AddListener(OnLocalAvatarClear);
        MelonCoroutines.Start(WaitForGestureRecogniser()); // todo: once stable, use initialization game event
    }

    private IEnumerator WaitForGestureRecogniser()
    {
        yield return new WaitUntil(() => CVRGestureRecognizer.Instance);
        InitializeScaleGesture();
    }

    #endregion Melon Events

    #region Game Events

    private void OnLocalAvatarLoad(CVRAvatar _)
    {
        _currentAvatarSupported = IsAvatarSupported();
        if (!_currentAvatarSupported)
            return;

        if (EntryUseCustomParameter.Value
            && !string.IsNullOrEmpty(_parameterName))
            CalibrateCustomParameter();

        if (EntryPersistThroughRestart.Value
            && _lastHeight < 0) // has not been set
        {
            var lastHeight = EntryHiddenAvatarHeight.Value;
            if (lastHeight > 0) SetAvatarHeight(lastHeight, true);
            return;
        }

        if (EntryPersistentHeight.Value
            && _lastHeight > 0) // has been set
            SetAvatarHeight(_lastHeight);
    }

    private void OnLocalAvatarClear(CVRAvatar _)
    {
        _currentAvatarSupported = false;

        if (!EntryPersistentHeight.Value)
            return;

        if (!IsAvatarSupported()
            && !EntryPersistFromUnsupported.Value)
            return;

        // update the last height
        var height = PlayerSetup.Instance.GetCurrentAvatarHeight();
        _lastHeight = height;
        EntryHiddenAvatarHeight.Value = height;
    }

    #endregion Game Events

    #region Avatar Scale Tool

    // todo: tool needs a dedicated parameter name
    //private const string ASTParameterName = "ASTHeight";
    //private const string ASTMotionParameterName = "#MotionScale";

    //https://github.com/NotAKidoS/AvatarScaleTool/blob/eaa6d343f916b9bb834bb30989fc6987680492a2/AvatarScaleTool/Editor/Scripts/AvatarScaleTool.cs#L13-L14
    private const float GlobalMinHeight = 0.25f;
    private const float GlobalMaxHeight = 2.5f;

    private string _parameterName = "AvatarScale";

    private float _lastHeight = -1f;
    private float _minHeight = GlobalMinHeight;
    private float _maxHeight = GlobalMaxHeight;
    private bool _currentAvatarSupported;

    private float GetValueFromHeight(float height)
    {
        return Mathf.Clamp01((height - _minHeight) / (_maxHeight - _minHeight));
    }

    private float GetHeightFromValue(float value)
    {
        return Mathf.Lerp(_minHeight, _maxHeight, value);
    }

    private void SetAvatarHeight(float height, bool immediate = false)
    {
        if (!IsAvatarSupported())
            return;

        AvatarAnimatorManager animatorManager = PlayerSetup.Instance.animatorManager;
        if (!animatorManager.IsInitialized)
        {
            Logger.Error("AnimatorManager is not initialized!");
            return;
        }

        if (!animatorManager.HasParameter(_parameterName))
        {
            Logger.Error($"Parameter '{_parameterName}' does not exist!");
            return;
        }

        Animator animator = animatorManager.Animator;
        var value = GetValueFromHeight(height);
        animator.SetFloat(_parameterName, value);
        if (immediate) animator.Update(0f); // apply

        _lastHeight = height; // session
        EntryHiddenAvatarHeight.Value = height; // persistent

        // update in menus
        CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(_parameterName, value);
    }

    private bool IsAvatarSupported()
    {
        // check if avatar has the parameter
        AvatarAnimatorManager animatorManager = PlayerSetup.Instance.animatorManager;
        if (!animatorManager.IsInitialized)
        {
            Logger.Error("AnimatorManager is not initialized!");
            return false;
        }

        if (!animatorManager.HasParameter(_parameterName))
        {
            Logger.Error($"Parameter '{_parameterName}' does not exist!");
            return false;
        }

        return true;
    }

    #endregion Avatar Scale Tool

    #region Custom Parameter Calibration

    private void CalibrateCustomParameter()
    {
        AvatarAnimatorManager animatorManager = PlayerSetup.Instance.animatorManager;
        if (!animatorManager.IsInitialized)
        {
            Logger.Error("AnimatorManager is not initialized!");
            return;
        }

        if (!animatorManager.HasParameter(_parameterName))
        {
            Logger.Error($"Parameter '{_parameterName}' does not exist!");
            return;
        }

        Animator animator = animatorManager.Animator; // we get from animator manager to ensure we have *profile* param
        animatorManager.GetParameter(_parameterName, out float initialValue);

        // set min height to 0
        animator.SetFloat(_parameterName, 0f);
        animator.Update(0f); // apply
        var minHeight = PlayerSetup.Instance.GetCurrentAvatarHeight();

        // set max height to 1
        animator.SetFloat(_parameterName, 1f);
        animator.Update(0f); // apply
        var maxHeight = PlayerSetup.Instance.GetCurrentAvatarHeight();

        // reset the parameter to its initial value
        animator.SetFloat(_parameterName, initialValue);
        animator.Update(0f); // apply

        Logger.Msg(
            $"Calibrated custom parameter '{_parameterName}' with min height {minHeight} and max height {maxHeight}");
    }

    #endregion Custom Parameter Calibration

    #region Scale Reconizer

    // Require triggers to be down while doing fist - Exteratta
    private readonly bool RequireTriggers = true;

    // Initial values when scale gesture is started
    private float _initialModifier;
    private float _initialTargetHeight;

    private void InitializeScaleGesture()
    {
        // This requires arms far outward- pull inward with fist and triggers.
        // Release triggers while still holding fist to readjust.

        CVRGesture gesture = new()
        {
            name = "astExtensionIn",
            type = CVRGesture.GestureType.Hold
        };
        gesture.steps.Add(new CVRGestureStep
        {
            firstGesture = CVRGestureStep.Gesture.Fist,
            secondGesture = CVRGestureStep.Gesture.Fist,
            startDistance = 1f,
            endDistance = 0.25f,
            direction = CVRGestureStep.GestureDirection.MovingIn,
            needsToBeInView = true
        });
        gesture.onStart.AddListener(OnScaleStart);
        gesture.onStay.AddListener(OnScaleStay);
        CVRGestureRecognizer.Instance.gestures.Add(gesture);

        gesture = new CVRGesture
        {
            name = "astExtensionOut",
            type = CVRGesture.GestureType.Hold
        };
        gesture.steps.Add(new CVRGestureStep
        {
            firstGesture = CVRGestureStep.Gesture.Fist,
            secondGesture = CVRGestureStep.Gesture.Fist,
            startDistance = 0.25f,
            endDistance = 1f,
            direction = CVRGestureStep.GestureDirection.MovingOut,
            needsToBeInView = true
        });
        gesture.onStart.AddListener(OnScaleStart);
        gesture.onStay.AddListener(OnScaleStay);
        CVRGestureRecognizer.Instance.gestures.Add(gesture);
    }

    private void OnScaleStart(float modifier, Transform transform1, Transform transform2)
    {
        if (!_currentAvatarSupported)
            return;

        if (!EntryUseScaleGesture.Value)
            return;

        // Store initial modifier so we can get difference later
        _initialModifier = Mathf.Max(modifier, 0.01f); // no zero
        _initialTargetHeight = PlayerSetup.Instance.GetCurrentAvatarHeight();
    }

    private void OnScaleStay(float modifier, Transform transform1, Transform transform2)
    {
        if (!_currentAvatarSupported)
            return;

        if (!EntryUseScaleGesture.Value)
            return;

        modifier = Mathf.Max(modifier, 0.01f); // no zero

        // Allow user to release triggers to reset "world grip"
        if (RequireTriggers && !AreBothTriggersDown())
        {
            _initialModifier = modifier;
            _initialTargetHeight = PlayerSetup.Instance.GetCurrentAvatarHeight();
            return;
        }

        // Invert so the gesture is more of a world squish instead of happy hug
        var modifierRatio = 1f / (modifier / _initialModifier);

        // Determine the adjustment factor for the height, this will be >1 if scaling up, <1 if scaling down.
        var heightAdjustmentFactor = modifierRatio > 1 ? 1 + (modifierRatio - 1) : 1 - (1 - modifierRatio);

        // Apply the adjustment to the target height
        var targetHeight = _initialTargetHeight * heightAdjustmentFactor;
        targetHeight = Mathf.Clamp(targetHeight, _minHeight, _maxHeight);
        SetAvatarHeight(targetHeight);
    }

    private static bool AreBothTriggersDown()
    {
        // Maybe it should be one trigger? Imagine XSOverlay scaling but for player.
        return CVRInputManager.Instance.interactLeftValue > 0.75f &&
               CVRInputManager.Instance.interactRightValue > 0.75f;
    }

    #endregion Scale Reconizer
}