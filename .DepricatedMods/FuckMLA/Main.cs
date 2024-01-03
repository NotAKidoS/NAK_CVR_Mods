using MelonLoader;

namespace NAK.FuckMLA;

public class FuckMLA : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.RootLogicPatches));
        ApplyPatches(typeof(HarmonyPatches.CVRInputModulePatches));
        ApplyPatches(typeof(HarmonyPatches.MOUSELOCKALPHAPatches));
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