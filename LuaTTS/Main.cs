using ABI_RC.Scripting.ScriptNetwork;
using ABI_RC.Systems.ModNetwork;
using MelonLoader;
using NAK.LuaTTS.Patches;

namespace NAK.LuaTTS;

public class LuaTTSMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(LuaScriptFactoryPatches));
        
        ModNetworkMessage.AddConverter<ScriptID>(ReadScriptID, WriteScriptID);
        ModNetworkMessage.AddConverter<ScriptInstanceID>(ReadScriptInstanceID, WriteScriptInstanceID);
    }
    
    private static ScriptID ReadScriptID(ModNetworkMessage msg)
    {
        msg.Read(out byte[] value);
        ScriptID scriptID = new(value);
        return scriptID;
    }
    
    private static void WriteScriptID(ModNetworkMessage msg, ScriptID scriptID)
    {
        msg.Write(scriptID.value);
    }
    
    private static ScriptInstanceID ReadScriptInstanceID(ModNetworkMessage msg)
    {
        msg.Read(out byte[] value);
        ScriptInstanceID scriptInstanceID = new(value);
        return scriptInstanceID;
    }
    
    private static void WriteScriptInstanceID(ModNetworkMessage msg, ScriptInstanceID scriptInstanceID)
    {
        msg.Write(scriptInstanceID.value);
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