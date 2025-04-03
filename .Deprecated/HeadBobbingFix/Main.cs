using ABI_RC.Core.Player;
using MelonLoader;

namespace NAK.HeadBobbingFix;

public class HeadBobbingFix : MelonMod
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(HeadBobbingFix));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle HeadBobbingFix entirely. You need full headbobbing enabled for the mod to take effect.");

    public override void OnInitializeMelon()
    {
        EntryEnabled.OnEntryValueChanged.Subscribe(OnEntryEnabledChanged);
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
    }

    void OnEntryEnabledChanged(bool newValue, bool oldValue)
    {
        if (newValue) PlayerSetup.Instance?.SetViewPointOffset();
    }

    void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }
}