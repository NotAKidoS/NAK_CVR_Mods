using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using NAK.AvatarScaleMod.AvatarScaling;
using System.Collections.Generic; // Added for list support

namespace NAK.AvatarScaleMod.Integrations
{
    public static partial class BtkUiAddon
    {
        private static readonly List<QMUIElement> USM_QmUiElements = new();

        private static void Setup_UniversalScalingSettings(Page page)
        {
            Category uniScalingCategory = AddMelonCategory(ref page, ModSettings.Hidden_Foldout_USM_SettingsCategory);

            SliderFloat scaleSlider = AddMelonSlider(ref uniScalingCategory, ModSettings.EntryHiddenAvatarHeight, AvatarScaleManager.DefaultMinHeight, AvatarScaleManager.DefaultMaxHeight);

            Button resetHeightButton = uniScalingCategory.AddButton(ModSettings.EntryUseUniversalScaling.DisplayName, "icon", ModSettings.EntryUseUniversalScaling.Description, ButtonStyle.TextOnly);
            resetHeightButton.OnPress += () =>
            {
                if (ModSettings.EntryUseUniversalScaling.Value)
                {
                    ModSettings.EntryUseUniversalScaling.Value = false;
                    return;
                }
                
                QuickMenuAPI.ShowConfirm("Use Universal Scaling?",
                    "Universal scaling only works when other users also have the mod! Are you sure you want to use Universal Scaling?",
                    () => ModSettings.EntryUseUniversalScaling.Value = true);
            };

            // Elements that should be disabled when universal scaling is disabled
            USM_QmUiElements.AddRange(new QMUIElement[]
            {
                scaleSlider,
                AddMelonToggle(ref uniScalingCategory, ModSettings.EntryScaleComponents),
                AddMelonToggle(ref uniScalingCategory, ModSettings.EntryAnimationScalingOverride)
            });

            // Events for the slider
            scaleSlider.OnValueUpdated += OnAvatarHeightSliderChanged;
            scaleSlider.OnSliderReset += OnAvatarHeightSliderReset;
            AvatarScaleEvents.OnLocalAvatarAnimatedHeightChanged.AddListener((scaler) =>
            {
                scaleSlider.SetSliderValue(scaler.GetTargetHeight());
                scaleSlider.DefaultValue = scaler.GetAnimatedHeight();
            });
            
            // Initial values
            OnUniversalScalingChanged(ModSettings.EntryUseUniversalScaling.Value);
            ModSettings.EntryUseUniversalScaling.OnEntryValueChanged.Subscribe((_, newValue) => OnUniversalScalingChanged(newValue));
        }

        private static void OnUniversalScalingChanged(bool value)
        {
            foreach (QMUIElement uiElement in USM_QmUiElements)
                uiElement.Disabled = !value;
        }

        #region Slider Events

        private static void OnAvatarHeightSliderChanged(float height)
        {
            AvatarScaleManager.Instance.SetTargetHeight(height);
        }

        private static void OnAvatarHeightSliderReset()
        {
            AvatarScaleManager.Instance.Setting_UniversalScaling = false;
        }

        #endregion
    }
}