using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using HarmonyLib;
using MelonLoader;

namespace NAK.CustomSpawnPoint;

public class CustomSpawnPointMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        SpawnPointManager.Init();
        
        HarmonyInstance.Patch( // listen for world details page request
            typeof(ViewManager).GetMethod(nameof(ViewManager.RequestWorldDetailsPage)),
            new HarmonyMethod(typeof(CustomSpawnPointMod).GetMethod(nameof(OnRequestWorldDetailsPage),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static void OnRequestWorldDetailsPage(string worldId)
    {
        SpawnPointManager.OnRequestWorldDetailsPage(worldId);
    }
}