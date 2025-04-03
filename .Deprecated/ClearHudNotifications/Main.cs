using ABI_RC.Core.UI;
using MelonLoader;
using UnityEngine;

namespace NAK.ClearHudNotifications;

public class ClearHudNotifications : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.CohtmlHudPatches));
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ClearNotifications();
        }
    }

    public static void ClearNotifications()
    {
        // sending an immediate notification clears buffer
        CohtmlHud.Instance?.ViewDropTextImmediate("(Local) Client", "Notifications Cleared!", "Cleared Hud Notifications!");
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