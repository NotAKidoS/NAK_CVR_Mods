using MelonLoader;
using NAK.LuaTTS.Patches;

namespace NAK.LuaTTS;

public class LuaTTSMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(LuaScriptFactoryPatches));
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