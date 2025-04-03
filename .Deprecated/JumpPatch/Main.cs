using MelonLoader;

namespace NAK.JumpPatch;

public class JumpPatch : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.MovementSystemPatches));
    }

    private void ApplyPatches(Type type)
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