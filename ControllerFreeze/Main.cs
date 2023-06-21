using MelonLoader;

namespace NAK.ControllerFreeze;

public class ControllerFreeze : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
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