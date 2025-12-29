using ABI_RC.Core.UI.UIRework;
using ABI_RC.Systems.InputManagement;
using UnityEngine;

namespace NAK.DummyMenu;

public class DummyMenuManager : CVRUIManagerBaseInput
{
    #region Boilerplate
    
    public static DummyMenuManager Instance { get; private set; }

    public override void Start()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        base.Start();
        
        ListenForModSettingChanges();
    }
    
    public override void ReloadView()
    {
        if (IsViewShown && !ModSettings.EntryReloadMenuEvenWhenOpen.Value) return;
        TrySetMenuPage();
        base.ReloadView();
    }

    public override void OnFinishedLoad(string _)
    {
        base.OnFinishedLoad(_);
        
        // Usually done in OnReadyForBindings, but that isn't called for my menu :)
        _cohtmlRenderMaterial = _uiRenderer.sharedMaterial;
        // ReSharper disable thrice Unity.PreferAddressByIdToGraphicsParams
        _cohtmlRenderMaterial.SetTexture("_DesolvePattern", pattern);
        _cohtmlRenderMaterial.SetTexture("_DesolveTiming", timing);
        _cohtmlRenderMaterial.SetTextureScale("_DesolvePattern", new Vector2(16, 9));
    }

    public void ToggleView()
    {
        bool invertShown = !IsViewShown;
        ToggleView(invertShown);
        CursorLockManager.Instance.SetUnlockWithId(invertShown, nameof(DummyMenuManager));
    }
    
    #endregion Boilerplate

    private void ListenForModSettingChanges()
    {
        ModSettings.EntryPageCouiPath.OnEntryValueChanged.Subscribe((oldValue, newValue) =>
        {
            DummyMenuMod.Logger.Msg($"Changing COUI page from {oldValue} to {newValue}");
            cohtmlView.Page = newValue;
        });
        
        ModSettings.EntryPageWidth.OnEntryValueChanged.Subscribe((oldValue, newValue) =>
        {
            DummyMenuMod.Logger.Msg($"Changing COUI width from {oldValue} to {newValue}");
            cohtmlView.Width = newValue;
            DummyMenuPositionHelper.Instance.UpdateAspectRatio(cohtmlView.Width, cohtmlView.Height);
        });
        
        ModSettings.EntryPageHeight.OnEntryValueChanged.Subscribe((oldValue, newValue) =>
        {
            DummyMenuMod.Logger.Msg($"Changing COUI height from {oldValue} to {newValue}");
            cohtmlView.Height = newValue;
            DummyMenuPositionHelper.Instance.UpdateAspectRatio(cohtmlView.Width, cohtmlView.Height);
        });
        
        ModSettings.EntryToggleMeToResetModifiers.OnEntryValueChanged.Subscribe((_, newValue) =>
        {
            if (!newValue) return;
            DummyMenuMod.Logger.Msg("Resetting modifiers to default because the toggle was enabled");
            ModSettings.EntryToggleMeToResetModifiers.Value = false;
            ModSettings.EntryDesktopMenuScaleModifier.ResetToDefault();
            ModSettings.EntryDesktopMenuDistanceModifier.ResetToDefault();
            ModSettings.EntryVrMenuScaleModifier.ResetToDefault();
            ModSettings.EntryVrMenuDistanceModifier.ResetToDefault();
        });
    }

    public void TrySetMenuPage()
    {
        string couiPath = ModSettings.EntryPageCouiPath.Value;
        string fullCouiPath = CreateDummyMenu.GetFullCouiPath(couiPath);
        if (File.Exists(fullCouiPath))
        {
            DummyMenuMod.Logger.Msg($"Found COUI page at {fullCouiPath}. Setting it as the controlledview page."); 
            cohtmlView.Page = "coui://" + couiPath;
        }
        else
        {
            DummyMenuMod.Logger.Error($"No COUI page found at {fullCouiPath}. Please create one at that path, or change the mod setting to point to an existing file. Using default example page instead.");
            cohtmlView.Page = "coui://" + CreateDummyMenu.ExampleDummyMenuPath;
        }
    }
}