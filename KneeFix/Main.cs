using MelonLoader;

namespace NAK.Melons.KneeFix;

public class KneeFixMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.BodySystemPatches));
        ApplyPatches(typeof(HarmonyPatches.VRIKPatches));
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