using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Core.UI.UIRework.Managers;
using ABI_RC.Systems.VRModeSwitch;
using ABI_RC.VideoPlayer.Scripts;
using HarmonyLib;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NAK.Tinyboard;

public class TinyboardMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(Tinyboard));

    private static readonly MelonPreferences_Entry<bool> EntrySmartAlignToMenu =
        Category.CreateEntry(
            identifier: "smart_align_to_menu",
            true,
            display_name: "Smart Align To Menu",
            description: "Should the keyboard align to the menu it was opened from? (Main Menu, World-Anchored Quick Menu)");
    
    private static readonly MelonPreferences_Entry<bool> EntryEnforceTitle =
        Category.CreateEntry(
            identifier: "enforce_title",
            true,
            display_name: "Enforce Title",
            description: "Should the keyboard enforce a title when opened from an input field or main menu?");
    
    private static readonly MelonPreferences_Entry<bool> EntryResizeKeyboard =
        Category.CreateEntry(
            identifier: "resize_keyboard",
            true,
            display_name: "Resize Keyboard",
            description: "Should the keyboard be resized to match XSOverlays width?");
    
    private static readonly MelonPreferences_Entry<bool> EntryUseModifiers =
        Category.CreateEntry(
            identifier: "use_scale_distance_modifiers",
            true,
            display_name: "Use Scale/Distance/Offset Modifiers",
            description: "Should the scale/distance/offset modifiers be used?");
    
    private static readonly MelonPreferences_Entry<float> EntryDesktopScaleModifier =
        Category.CreateEntry(
            identifier: "desktop_scale_modifier",
            0.75f,
            display_name: "Desktop Scale Modifier",
            description: "Scale modifier for desktop mode.");
    
    private static readonly MelonPreferences_Entry<float> EntryDesktopDistance =
        Category.CreateEntry(
            identifier: "desktop_distance_modifier",
            0f,
            display_name: "Desktop Distance Modifier",
            description: "Distance modifier for desktop mode.");
    
    private static readonly MelonPreferences_Entry<float> EntryDesktopVerticalAdjustment =
        Category.CreateEntry(
            identifier: "desktop_vertical_adjustment",
            0.1f,
            display_name: "Desktop Vertical Adjustment",
            description: "Vertical adjustment for desktop mode.");
    
    private static readonly MelonPreferences_Entry<float> EntryVRScaleModifier =
        Category.CreateEntry(
            identifier: "vr_scale_modifier",
            0.85f,
            display_name: "VR Scale Modifier",
            description: "Scale modifier for VR mode.");

    private static readonly MelonPreferences_Entry<float> EntryVRDistance =
        Category.CreateEntry(
            identifier: "vr_distance_modifier",
            0.2f,
            display_name: "VR Distance Modifier",
            description: "Distance modifier for VR mode.");
    
    private static readonly MelonPreferences_Entry<float> EntryVRVerticalAdjustment =
        Category.CreateEntry(
            identifier: "vr_vertical_adjustment",
            0f,
            display_name: "VR Vertical Adjustment",
            description: "Vertical adjustment for VR mode.");

    #endregion Melon Preferences
    
    private static Transform _tinyBoardOffset;
    private static void ApplyTinyBoardOffsetsForVRMode()
    {
        if (!EntryUseModifiers.Value)
        {
            _tinyBoardOffset.localScale = Vector3.one;
            _tinyBoardOffset.localPosition = Vector3.zero;
            return;
        }
        float distanceModifier;
        float scaleModifier;
        float verticalAdjustment;
        if (MetaPort.Instance.isUsingVr)
        {
            scaleModifier = EntryVRScaleModifier.Value;
            distanceModifier = EntryVRDistance.Value;
            verticalAdjustment = EntryVRVerticalAdjustment.Value;
        }
        else
        {
            scaleModifier = EntryDesktopScaleModifier.Value;
            distanceModifier = EntryDesktopDistance.Value;
            verticalAdjustment = EntryDesktopVerticalAdjustment.Value;
        }
        _tinyBoardOffset.localScale = Vector3.one * scaleModifier;
        _tinyBoardOffset.localPosition = new Vector3(0f, verticalAdjustment, distanceModifier);
    }

    private static void ApplyTinyBoardWidthResize()
    {
        KeyboardManager km = KeyboardManager.Instance;
        CohtmlControlledView cohtmlView = km.cohtmlView;
        Transform keyboardTransform = cohtmlView.transform;
        
        int targetWidthPixels = EntryResizeKeyboard.Value ? 1330 : 1520;
        float targetScaleX = EntryResizeKeyboard.Value ? 1.4f : 1.6f;
        
        cohtmlView.Width = targetWidthPixels;
        Vector3 currentScale = keyboardTransform.localScale;
        currentScale.x = targetScaleX;
        keyboardTransform.localScale = currentScale;
    }
    
    public override void OnInitializeMelon()
    {
        // add our shim transform to scale the menu down by 0.75
        HarmonyInstance.Patch(
            typeof(CVRKeyboardPositionHelper).GetMethod(nameof(CVRKeyboardPositionHelper.Awake),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(TinyboardMod).GetMethod(nameof(OnCVRKeyboardPositionHelperAwake),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // reposition the keyboard when it is opened to match the menu position if it is opened from a menu
        HarmonyInstance.Patch(
            typeof(MenuPositionHelperBase).GetMethod(nameof(MenuPositionHelperBase.OnMenuOpen),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(TinyboardMod).GetMethod(nameof(OnMenuPositionHelperBaseOnMenuOpen),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // enforces a title for the keyboard in cases it did not already have one
        HarmonyInstance.Patch(
            typeof(KeyboardManager).GetMethod(nameof(KeyboardManager.ShowKeyboard),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(TinyboardMod).GetMethod(nameof(OnKeyboardManagerShowKeyboard),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // resize keyboard to match XSOverlays width
        HarmonyInstance.Patch(
            typeof(KeyboardManager).GetMethod(nameof(KeyboardManager.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(TinyboardMod).GetMethod(nameof(OnKeyboardManagerStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        // update offsets when switching VR modes
        VRModeSwitchEvents.OnPostVRModeSwitch.AddListener((_) => ApplyTinyBoardOffsetsForVRMode());
        
        // listen for setting changes
        EntryUseModifiers.OnEntryValueChanged.Subscribe((_,_) => ApplyTinyBoardOffsetsForVRMode());
        EntryDesktopScaleModifier.OnEntryValueChanged.Subscribe((_,_) => ApplyTinyBoardOffsetsForVRMode());
        EntryVRScaleModifier.OnEntryValueChanged.Subscribe((_,_) => ApplyTinyBoardOffsetsForVRMode());
        EntryDesktopDistance.OnEntryValueChanged.Subscribe((_,_) => ApplyTinyBoardOffsetsForVRMode());
        EntryVRDistance.OnEntryValueChanged.Subscribe((_,_) => ApplyTinyBoardOffsetsForVRMode());
        EntryResizeKeyboard.OnEntryValueChanged.Subscribe((_,_) => ApplyTinyBoardWidthResize());
    }
    
    private static void OnCVRKeyboardPositionHelperAwake(CVRKeyboardPositionHelper __instance)
    {
        _tinyBoardOffset = new GameObject("NAKTinyBoard").transform;
        
        Transform offsetTransform = __instance.transform.GetChild(0);
        _tinyBoardOffset.SetParent(offsetTransform, false);

        ApplyTinyBoardOffsetsForVRMode();
        
        Transform menuTransform = __instance.menuTransform;
        menuTransform.SetParent(_tinyBoardOffset, false);
    }
    
    private static void OnMenuPositionHelperBaseOnMenuOpen(MenuPositionHelperBase __instance)
    {
        if (!EntrySmartAlignToMenu.Value) return;
        if (__instance is not CVRKeyboardPositionHelper { IsMenuOpen: true }) return;
        
        // Check if the open source was an open menu
        KeyboardManager.OpenSource? openSource = KeyboardManager.Instance._keyboardOpenSource;

        MenuPositionHelperBase menuPositionHelper;
        switch (openSource)
        {
            case KeyboardManager.OpenSource.MainMenu:
                menuPositionHelper = CVRMainMenuPositionHelper.Instance;
                break;
            case KeyboardManager.OpenSource.QuickMenu:
                menuPositionHelper = CVRQuickMenuPositionHelper.Instance;
                if (!menuPositionHelper.IsUsingWorldAnchoredMenu) return; // hand anchored quick menu, don't touch 
                break;
            default: return;
        }
        
        // get modifiers
        float rootScaleModifier = __instance.transform.lossyScale.x;
        float keyboardDistanceModifier = __instance.MenuDistanceModifier;
        float menuDistanceModifier = menuPositionHelper.MenuDistanceModifier;
        
        // get difference between modifiers
        float distanceModifier = keyboardDistanceModifier - menuDistanceModifier;
        
        // place keyboard at menu position + difference in modifiers
        Transform menuOffsetTransform = menuPositionHelper._offsetTransform;
        Quaternion keyboardRotation = menuOffsetTransform.rotation;
        Vector3 keyboardPosition = menuOffsetTransform.position +
                                   menuOffsetTransform.forward * (rootScaleModifier * distanceModifier);
        
        // place keyboard as if it was opened with player camera in same place as menu was
        __instance._offsetTransform.SetPositionAndRotation(keyboardPosition, keyboardRotation);
    }
    
    private static void OnKeyboardManagerStart() => ApplyTinyBoardWidthResize();
    
    /*
         public void ShowKeyboard(
            string currentText,
            Action<string> callback,
            string placeholder = null,
            string successText = "Success",
            int maxCharacterCount = 0,
            bool hidden = false,
            bool multiLine = false,
            string title = null,
            OpenSource openSource = OpenSource.Other)
     */
    
    // using mix of index and args params because otherwise explodes with invalid IL ?
    private static void OnKeyboardManagerShowKeyboard(ref string __7, ref string __2, object[] __args)
    {
        if (!EntryEnforceTitle.Value) return;
        
        // ReSharper disable thrice InlineTemporaryVariable
        ref string title = ref __7;
        ref string placeholder = ref __2;
        if (!string.IsNullOrWhiteSpace(title)) return;

        Action<string> callback = __args[1] as Action<string>;
        KeyboardManager.OpenSource? openSource = __args[8] as KeyboardManager.OpenSource?;

        if (callback?.Target != null)
        {
            var target = callback.Target;
            switch (openSource)
            {
                case KeyboardManager.OpenSource.CVRInputFieldKeyboardHandler:
                    TrySetPlaceholderFromKeyboardHandler(target, ref title, ref placeholder);
                    break;
                case KeyboardManager.OpenSource.MainMenu:
                    title = TryExtractTitleFromMainMenu(target);
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(placeholder))
        {
            // fallback to placeholder if no title found
            if (string.IsNullOrWhiteSpace(title)) title = placeholder;
            
            // clear placeholder if it is longer than 10 characters
            if (placeholder.Length > 10) placeholder = string.Empty;
        }
    }

    private static void TrySetPlaceholderFromKeyboardHandler(object target, ref string title, ref string placeholder)
    {
        Type type = target.GetType();
        
        TMP_InputField tmpInput = type.GetField("input", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target) as TMP_InputField;
        if (tmpInput != null)
        {
            if (tmpInput.GetComponentInParent<ViewManagerVideoPlayer>()) title = "VideoPlayer URL or Search";
            if (tmpInput.placeholder is TMP_Text ph)
            {
                placeholder = ph.text;
                return;
            }
            placeholder = PrettyString(tmpInput.gameObject.name);
            return;
        }
        
        InputField legacyInput = type.GetField("inputField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target) as InputField;
        if (legacyInput != null)
        {
            if (legacyInput.placeholder is Text ph)
            {
                placeholder = ph.text;
                return;
            }
            placeholder = PrettyString(legacyInput.gameObject.name);
            return;
        }
    }

    private static string TryExtractTitleFromMainMenu(object target)
    {
        Type type = target.GetType();
        string targetId = type.GetField("targetId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target) as string;
        return string.IsNullOrWhiteSpace(targetId) ? null : PrettyString(targetId);
    }
    
    private static string PrettyString(string str)
    {
        int len = str.Length;
        Span<char> buffer = stackalloc char[len * 2];
        int pos = 0;
        bool newWord = true;
        for (int i = 0; i < len; i++)
        {
            char c = str[i];
            if (c is '_' or '-')
            {
                buffer[pos++] = ' ';
                newWord = true;
                continue;
            }
            if (char.IsUpper(c) && i > 0 && !newWord) buffer[pos++] = ' ';
            buffer[pos++] = newWord ? char.ToUpperInvariant(c) : c;
            newWord = false;
        }
        return new string(buffer[..pos]);
    }
}