using System.Collections;
using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.AnimatorManager;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.InputManagement;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using HarmonyLib;
using MelonLoader;
using NAK.ASTExtension.Extensions;
using UnityEngine;

namespace NAK.ASTExtension;

public class ASTExtensionMod : MelonMod
{
    internal static ASTExtensionMod Instance; // lazy
    internal static MelonLogger.Instance Logger;

    #region Melon Preferences

    internal const string ModName = nameof(ASTExtension);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);

    private static readonly MelonPreferences_Entry<bool> EntryUseScaleGesture =
        Category.CreateEntry("use_scale_gesture", true,
            "Use Scale Gesture", "Use the scale gesture to adjust your avatar's height.");
    
    private static readonly MelonPreferences_Entry<bool> EntryInvertGesture =
        Category.CreateEntry("invert_scale_gesture", false,
            "Invert Scale Gesture", "Invert the scale gesture to adjust your avatar's height.");
    
    private static readonly MelonPreferences_Entry<bool> EntryRequireTriggersDuringGesture =
        Category.CreateEntry("require_triggers", true,
            "Require Triggers", "Require triggers to be down while doing the scale gesture.");

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

    #endregion Melon Preferences

    #region Melon Events

    public override void OnInitializeMelon()
    {
        Instance = this;
        Logger = LoggerInstance;

        //InitializeSettings();
        //CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener(OnLocalAvatarLoad);
        //CVRGameEventSystem.Avatar.OnLocalAvatarClear.AddListener(OnLocalAvatarClear);
        
        HarmonyInstance.Patch(
            typeof(CVRGestureRecognizer).GetMethod(nameof(CVRGestureRecognizer.Start),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(ASTExtensionMod).GetMethod(nameof(OnGestureRecogniserInitialized),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.SetupAvatar),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(ASTExtensionMod).GetMethod(nameof(OnSetupAvatar),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.ClearAvatar),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(ASTExtensionMod).GetMethod(nameof(OnClearAvatar),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        InitializeIntegration("BTKUILib", Integrations.BtkUiAddon.Initialize);
    }

    private static void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        Logger.Msg($"Initializing {modName} integration.");
        integrationAction.Invoke();
    }
    
    #endregion Melon Events

    #region Harmony Patches

    private static void OnGestureRecogniserInitialized()
        => Instance.InitializeScaleGesture();
    
    private static void OnSetupAvatar(ref CVRAvatar ____avatarDescriptor)
        => Instance.OnLocalAvatarLoad(____avatarDescriptor);
    
    private static void OnClearAvatar(ref CVRAvatar ____avatarDescriptor)
        => Instance.OnLocalAvatarClear(____avatarDescriptor);

    #endregion Harmony Patches
    
    #region Game Events

    private void OnLocalAvatarLoad(CVRAvatar _)
    {
        if (!FindSupportedParameter(out string parameterName))
            return;
        
        if (!AttemptCalibrateParameter(parameterName, out float minHeight, out float maxHeight, out float modifier))
            return;
        
        SetupParameter(parameterName, minHeight, maxHeight, modifier);

        if (EntryPersistThroughRestart.Value
            && _lastHeight < 0) // has not been set
        {
            var lastHeight = EntryHiddenAvatarHeight.Value;
            if (lastHeight > 0) SetAvatarHeight(lastHeight);
            return;
        }

        if (EntryPersistentHeight.Value
            && _lastHeight > 0) // has been set
            SetAvatarHeight(_lastHeight);
    }

    private void OnLocalAvatarClear(CVRAvatar avatar)
    {
        if (!EntryPersistentHeight.Value)
        {
            ResetParameter();
            return;
        }

        if (!_currentAvatarSupported
            && !EntryPersistFromUnsupported.Value)
            return;

        // update the last height
        if (avatar != null) StoreLastHeight(PlayerSetup.Instance.GetCurrentAvatarHeight());
    }
    
    #endregion Game Events

    #region Avatar Scale Tool Extension
    
    private static HashSet<string> SUPPORTED_PARAMETERS = new()
    {
        "AvatarScale", // default
        "Scale", // most common
        "Scale/Scale", // kafe
        "Scaler", // momo
        "Height", // loliwurt
        "LoliModifier", // avatar
        "AvatarSize", // froggo
        "Size", // lily
        "SizeScale", // tactical
        "Scaling", // dark gamer
    };

    //https://github.com/NotAKidoS/AvatarScaleTool/blob/eaa6d343f916b9bb834bb30989fc6987680492a2/AvatarScaleTool/Editor/Scripts/AvatarScaleTool.cs#L13-L14
    private const float DEFAULT_MIN_HEIGHT = 0.25f;
    private const float DEFAULT_MAX_HEIGHT = 2.5f;

    private bool _currentAvatarSupported;
    private string _parameterName = SUPPORTED_PARAMETERS.First();

    private float _minHeight = DEFAULT_MIN_HEIGHT;
    private float _maxHeight = DEFAULT_MAX_HEIGHT;
    private float _modifier = 1f;
    private float _lastHeight = -1f;
    
    private void SetupParameter(string parameterName, float minHeight, float maxHeight, float modifier)
    {
        _parameterName = parameterName;
        _minHeight = minHeight;
        _maxHeight = maxHeight;
        _modifier = modifier;
        _currentAvatarSupported = true;
    }
    
    private void ResetParameter()
    {
        _parameterName = SUPPORTED_PARAMETERS.First();
        _minHeight = DEFAULT_MIN_HEIGHT;
        _maxHeight = DEFAULT_MAX_HEIGHT;
        _modifier = 1f;
        _currentAvatarSupported = false;
    }
    
    private void StoreLastHeight(float height)
    {
        _lastHeight = height;
        EntryHiddenAvatarHeight.Value = height;
    }

    private float GetValueFromHeight(float height)
    {
        return Mathf.Sign(_modifier) > 0 // negative means min & max heights were swapped (because i said so)
            ? Mathf.Clamp01((height - _minHeight) / (_maxHeight - _minHeight)) * Mathf.Abs(_modifier)
            : 1 - Mathf.Clamp01((height - _minHeight) / (_maxHeight - _minHeight)) * Mathf.Abs(_modifier);
    }

    // private float GetHeightFromValue(float value)
    //     => Mathf.Lerp(_minHeight, _maxHeight, value * Mathf.Abs(_modifier));
    
    private static bool FindSupportedParameter(out string parameterName)
    {
        parameterName = null;
        
        AvatarAnimatorManager animatorManager = PlayerSetup.Instance.animatorManager;
        if (!animatorManager.IsInitialized)
        {
            Logger.Error("AnimatorManager is not initialized!");
            return false;
        }

        var parameterSet = new HashSet<string>(animatorManager.Parameters.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in SUPPORTED_PARAMETERS)
        {
            if (!parameterSet.Contains(parameter)) continue;
            parameterName = parameterSet.First(p => p.Equals(parameter, StringComparison.OrdinalIgnoreCase));
            Logger.Msg($"Found supported parameter '{parameterName}'");
            return true;
        }
        
        Logger.Error("No supported parameter found!");
        return false;
    }
    
    private static bool AttemptCalibrateParameter(string parameterName, 
        out float minHeight, out float maxHeight, out float modifier)
    {
        minHeight = 0f;
        maxHeight = 0f;
        modifier = 1f;
        
        AvatarAnimatorManager animatorManager = PlayerSetup.Instance.animatorManager;
        if (!animatorManager.IsInitialized)
        {
            Logger.Error("AnimatorManager is not initialized!");
            return false;
        }

        if (string.IsNullOrEmpty(parameterName))
        {
            Logger.Error("Parameter name is empty!");
            return false;
        }

        if (!animatorManager.HasParameter(parameterName))
        {
            Logger.Error($"Parameter '{parameterName}' does not exist!");
            return false;
        }

        Animator animator = animatorManager.Animator;
        animatorManager.GetParameter(parameterName, out float initialValue);
        
        // set min height to 0
        animator.SetFloat(parameterName, 0f);
        animator.Update(0f); // apply
        minHeight = PlayerSetup.Instance.GetCurrentAvatarHeight();
        
        // set max height to 1++
        for (int i = 1; i <= 100; i++)
        {
            animator.SetFloat(parameterName, i);
            animator.Update(0f); // apply
            var height = PlayerSetup.Instance.GetCurrentAvatarHeight();
            if (height <= maxHeight) break; // stop if height is not increasing
            modifier = i;
            maxHeight = height;
        }
        
        // reset the parameter to its initial value
        animator.SetFloat(parameterName, initialValue);
        animator.Update(0f); // apply
        
        // check if there was no change
        if (Math.Abs(minHeight - maxHeight) < float.Epsilon)
        {
            Logger.Error("Calibration failed: min height is equal to max height!");
            return false;
        }
        
        // swap if needed
        if (minHeight > maxHeight)
        {
            (minHeight, maxHeight) = (maxHeight, minHeight);
            modifier = -modifier; // invert
        }
        
        Logger.Msg($"Calibrated custom parameter '{parameterName}' with min height {minHeight} and max height {maxHeight} using modifier {modifier}");
        return true;
    }
    
    internal void SetAvatarHeight(float height)
    {
        if (!_currentAvatarSupported)
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

        StoreLastHeight(height);
        
        var value = GetValueFromHeight(height);
        animatorManager.SetParameter(_parameterName, value);
        animatorManager.Animator.Update(0f); // apply
        CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(_parameterName, value); // update AAS menus
        PlayerSetup.Instance.CheckUpdateAvatarScaleToPlaySpaceRelation(); // update play space
    }

    #endregion Avatar Scale Tool Extension

    #region Scale Reconizer
    
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
        gesture.onEnd.AddListener(OnScaleEnd);
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
        gesture.onEnd.AddListener(OnScaleEnd);
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
        
        if (EntryRequireTriggersDuringGesture.Value) 
            CVR_InteractableManager.enableInteractions = false;
    }
    
    private void OnScaleStay(float modifier, Transform transform1, Transform transform2)
    {
        if (!_currentAvatarSupported)
            return;

        if (!EntryUseScaleGesture.Value)
            return;

        modifier = Mathf.Max(modifier, 0.01f); // no zero

        // Allow user to release triggers to reset "world grip"
        if (EntryRequireTriggersDuringGesture.Value && !AreBothTriggersDown())
        {
            _initialModifier = modifier;
            _initialTargetHeight = PlayerSetup.Instance.GetCurrentAvatarHeight();
            return;
        }

        // Calculate modifier ratio
        var modifierRatio = modifier / _initialModifier;

        // If inversion is toggled, invert the modifier ratio
        if (!EntryInvertGesture.Value) modifierRatio = 1f / modifierRatio;

        // Determine the adjustment factor for the height, this will be >1 if scaling up, <1 if scaling down.
        var heightAdjustmentFactor = modifierRatio > 1 ? 1 + (modifierRatio - 1) : 1 - (1 - modifierRatio);

        // Apply the adjustment to the target height
        var targetHeight = _initialTargetHeight * heightAdjustmentFactor;
        targetHeight = Mathf.Clamp(targetHeight, _minHeight, _maxHeight);
        SetAvatarHeight(targetHeight);
    }
    
    private void OnScaleEnd(float modifier, Transform transform1, Transform transform2)
    {
        if (!_currentAvatarSupported)
            return;

        if (!EntryUseScaleGesture.Value)
            return;

        if (EntryRequireTriggersDuringGesture.Value) 
            CVR_InteractableManager.enableInteractions = true;
    }

    private static bool AreBothTriggersDown()
    {
        // Maybe it should be one trigger? Imagine XSOverlay scaling but for player.
        return CVRInputManager.Instance.interactLeftValue > 0.75f &&
               CVRInputManager.Instance.interactRightValue > 0.75f;
    }
    
    #endregion Scale Reconizer
}