using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.VRModeSwitch;
using MelonLoader;
using UnityEngine;

namespace NAK.MoreMenuOptions;

public class MoreMenuOptions : MelonMod
{
    // very lazy mod lol
    
    public override void OnInitializeMelon()
    {
        // Main Menu Scale & Distance Modifier Settings
        ModSettings.EntryMainMenuModiferUsage.OnEntryValueChanged.Subscribe(OnMMUsageTypeChanged);
        ModSettings.EntryMMScaleModifier.OnEntryValueChanged.Subscribe(OnMMFloatModifierChanged);
        ModSettings.EntryMMDistanceModifier.OnEntryValueChanged.Subscribe(OnMMFloatModifierChanged);
        
        // Quick Menu World Anchor In VR Setting
        ModSettings.EntryQMWorldAnchorInVR.OnEntryValueChanged.Subscribe(OnQMWorldAnchorInVRChanged);
        
        // Game Event Subscriptions
        CVRGameEventSystem.World.OnLoad.AddListener(OnGameStart);
        VRModeSwitchEvents.OnPostVRModeSwitch.AddListener(OnVRModeSwitched);
    }
    
    #region Game Events
    
    private void OnGameStart(string _)
    {
        UpdateMenuSettings();
        CVRGameEventSystem.World.OnLoad.RemoveListener(OnGameStart); // only need to run once
    }
    
    private void OnVRModeSwitched(bool switchToVr)
        => UpdateMenuSettings();

    private void UpdateMenuSettings()
    {
        UpdateMainMenuModifierSettings();
        UpdateQuickMenuModifierSettings();
    }
    
    #endregion Game Events

    #region Main Menu Scale & Distance Modifier Settings
    
    internal enum MainMenuModifierUsage
    {
        None,
        Desktop,
        VR,
        Both
    }
    
    private void OnMMUsageTypeChanged(MainMenuModifierUsage _, MainMenuModifierUsage __)
        => UpdateMainMenuModifierSettings();
    
    private void OnMMFloatModifierChanged(float _, float __)
        => UpdateMainMenuModifierSettings();
    
    private void UpdateMainMenuModifierSettings()
    {
        if (CVRMainMenuPositionHelper.Instance == null)
            return;
    
        MainMenuModifierUsage usage = ModSettings.EntryMainMenuModiferUsage.Value;
    
        bool isVrActive = MetaPort.Instance.isUsingVr;
        bool applyVrModifier = (isVrActive && usage is MainMenuModifierUsage.VR or MainMenuModifierUsage.Both);
        bool applyDesktopModifier = (!isVrActive && usage is MainMenuModifierUsage.Desktop or MainMenuModifierUsage.Both);
        
        if (applyVrModifier || applyDesktopModifier)
        {
            CVRMainMenuPositionHelper.Instance.menuTransform.localPosition = new Vector3(0f, 0f, ModSettings.EntryMMDistanceModifier.Value);
            CVRMainMenuPositionHelper.Instance.menuTransform.parent.localScale = Vector3.one * ModSettings.EntryMMScaleModifier.Value;
        }
        else // None or invalid usage
        {
            CVRMainMenuPositionHelper.Instance.menuTransform.localPosition = Vector3.zero;
            CVRMainMenuPositionHelper.Instance.menuTransform.parent.localScale = Vector3.one;
        }
    }
    
    #endregion Main Menu Scale & Distance Modifier Settings

    #region Quick Menu World Anchor In VR Setting
    
    private void OnQMWorldAnchorInVRChanged(bool _, bool __)
        => UpdateQuickMenuModifierSettings();

    private void UpdateQuickMenuModifierSettings()
    {
        if (CVRQuickMenuPositionHelper.Instance == null)
            return;
        
        CVRQuickMenuPositionHelper.Instance.worldAnchorMenu =
            ModSettings.EntryQMWorldAnchorInVR.Value && MetaPort.Instance.isUsingVr;
    }

    #endregion Quick Menu World Anchor In VR Setting
}