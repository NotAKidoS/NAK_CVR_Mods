using FuckMLA;
using MelonLoader;
using UnityEngine.Windows;

namespace NAK.FuckVivox;

public class FuckVivox : MelonMod
{
    internal static MelonLogger.Instance Logger;
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        ApplyPatches(typeof(HarmonyPatches.VivoxServiceInternalPatches));

        WindowFocusManager.OnFocusStateChanged += OnFocusChanged;
    }

    private void OnFocusChanged(bool value)
    {
        Logger.Msg("WindowFocusManager.OnFocusStateChanged " + value);
    }
    
    public override void OnUpdate()
    {
        WindowFocusManager.CheckWindowFocusedState();
        
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F11))
            VivoxHelpers.PleaseReLoginThankYou();
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