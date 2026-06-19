using ABI_RC.Core.Savior;
using MelonLoader;
using UnityEngine;

namespace NAK.DefaultDynamicBoneWind;

public class DefaultDynamicBoneWindMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        CVRGameSettings.Init();

        CVRGameSettings.CoolBool.OnChanged += ShitChanged;
        CVRGameSettings.Microphone.OnChanged += ShitChanged;
    }

    private static void ShitChanged(Color old, Color now) => Logger.Msg($"Shit changed: {old} has now become {now}");
    private static void ShitChanged(string old, string now) => Logger.Msg($"ShitAudio changed: {old} has now become {now}");

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.P)) CVRGameSettings.CoolBool.Value = CVRGameSettings.CoolBool.Value == Color.white ? Color.black : Color.white;
        if (Input.GetKeyDown(KeyCode.C)) CVRGameSettings.TestSettingsCategory.ResetAll();
        if (Input.GetKeyDown(KeyCode.O)) Logger.Msg($"Setting value: {CVRGameSettings.CoolBool}");
    }
}