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

    static void OnEntryEnabledChanged(bool newValue, bool oldValue)
    {
        if (AvatarScaleManager.LocalAvatar != null)
            AvatarScaleManager.LocalAvatar.enabled = newValue;
    }

    static void OnEntryUseScaleGestureChanged(bool newValue, bool oldValue)
    {
        AvatarScaleGesture.GestureEnabled = newValue;
    }
}