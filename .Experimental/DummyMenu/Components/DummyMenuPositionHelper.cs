using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Savior;
using UnityEngine;

namespace NAK.DummyMenu;

[DefaultExecutionOrder(16000)] // just before ControllerRay
public class DummyMenuPositionHelper : MenuPositionHelperBase
{
    public static DummyMenuPositionHelper Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public override bool IsMenuOpen => DummyMenuManager.Instance.IsViewShown;
    public override float MenuScaleModifier => MetaPort.Instance.isUsingVr ? ModSettings.EntryVrMenuScaleModifier.Value : ModSettings.EntryDesktopMenuScaleModifier.Value;
    public override float MenuDistanceModifier => MetaPort.Instance.isUsingVr ? ModSettings.EntryVrMenuDistanceModifier.Value : ModSettings.EntryDesktopMenuDistanceModifier.Value;
    
    public void UpdateAspectRatio(float width, float height)
    {
        if (width <= 0f || height <= 0f)
            return;

        _menuAspectRatio = width / height;
        
        float normalizedWidth = width / Mathf.Max(width, height);
        float normalizedHeight = height / Mathf.Max(width, height);
        menuTransform.localScale = new Vector3(normalizedWidth, normalizedHeight, 1f);
    }
}