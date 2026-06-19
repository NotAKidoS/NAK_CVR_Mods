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
        
        HarmonyInstance.Patch(
            typeof(ViewManager).GetMethod(nameof(ViewManager.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(CustomSpawnPointMod).GetMethod(nameof(OnViewManagerStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch( // listen for world details page request
            typeof(ViewManager).GetMethod(nameof(ViewManager.RequestWorldDetailsPage),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(CustomSpawnPointMod).GetMethod(nameof(OnRequestWorldDetailsPage),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static void OnViewManagerStart()
    {
        SpawnPointManager.OnViewManagerStart();
    }
    
    private static void OnRequestWorldDetailsPage(string worldId)
    {
        SpawnPointManager.OnRequestWorldDetailsPage(worldId);
    }
}