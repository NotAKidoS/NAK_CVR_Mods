using ABI_RC.Core.Player;
using MelonLoader;
using UnityEngine;

namespace NAK.LuaNetworkVariables;

public class LuaNetworkVariablesMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    #region Melon Preferences
    

    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(Patches.LuaScriptFactory_Patches));
    }
    
    // public override void OnUpdate()
    // {
    //     // if (Input.GetKeyDown(KeyCode.F1))
    //     // {
    //     //     PlayerSetup.Instance.DropProp("be0b5acc-a987-48dc-a28b-62bd912fe3a0");
    //     // }
    //     //
    //     // if (Input.GetKeyDown(KeyCode.F2))
    //     // {
    //     //     GameObject go = new("TestSyncedObject");
    //     //     go.AddComponent<TestSyncedObject>();
    //     // }
    // }

    #endregion Melon Events
    
    #region Melon Mod Utilities
    
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

    #endregion Melon Mod Utilities
}