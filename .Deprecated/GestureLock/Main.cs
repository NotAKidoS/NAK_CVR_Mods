using MelonLoader;

namespace NAK.GestureLock;

public class GestureLock : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.CVRInputModule_XRPatches));
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