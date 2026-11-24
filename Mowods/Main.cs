using ABI_RC.Systems.GameEventSystem;
using MelonLoader;

namespace NAK.Mowods;

public class MowodsMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        CVRGameEventSystem.MainMenu.OnOpen.AddListener(Menu.Attach);
    }
}