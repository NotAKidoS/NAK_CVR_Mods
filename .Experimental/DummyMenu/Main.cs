using ABI_RC.Systems.GameEventSystem;
using MelonLoader;
using UnityEngine;

namespace NAK.DummyMenu;

public class DummyMenuMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(CreateDummyMenu.Create);
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(ModSettings.EntryToggleDummyMenu.Value)) DummyMenuManager.Instance.ToggleView();
    }
}