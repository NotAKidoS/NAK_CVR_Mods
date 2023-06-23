namespace NAK.AvatarScaleMod;

// Another thing i stole from Kafe, this organizes stuff so much moreee
// Should I move the entries here too?
static class ModSettings
{
    public static void InitializeModSettings()
    {
        AvatarScaleMod.EntryEnabled.OnEntryValueChanged.Subscribe(OnEntryEnabledChanged);
        AvatarScaleMod.EntryUseScaleGesture.OnEntryValueChanged.Subscribe(OnEntryUseScaleGestureChanged);
    }

    static void OnEntryEnabledChanged(bool oldVal, bool newVal)
    {
        if (AvatarScaleManager.LocalAvatar != null)
            AvatarScaleManager.LocalAvatar.enabled = newVal;
    }

    static void OnEntryUseScaleGestureChanged(bool oldVal, bool newVal)
    {
        AvatarScaleGesture.GestureEnabled = newVal;
    }
}