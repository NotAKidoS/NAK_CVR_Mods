using System.Reflection;
using ABI_RC.Systems.GameEventSystem;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using NAK.CVRLuaToolsExtension.NamedPipes;
using UnityEngine;

namespace NAK.CVRLuaToolsExtension;

public class CVRLuaToolsExtensionMod : MelonMod
{
    internal static MelonLogger.Instance Logger;

    #region Melon Preferences

    private const string ModName = nameof(CVRLuaToolsExtension);

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(ModName);

    internal static readonly MelonPreferences_Entry<bool> EntryAttemptInitOffMainThread =
        Category.CreateEntry("attempt_init_off_main_thread", false,
            "Attempt init off Main Thread", "Attempt to initialize the lua script off main thread.");

    #endregion Melon Preferences

    #region Melon Events

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        HarmonyInstance.Patch(
            typeof(CVRLuaClientBehaviour).GetMethod(nameof(CVRLuaClientBehaviour.InitializeLuaVm)), // protected
            postfix: new HarmonyMethod(typeof(CVRLuaToolsExtensionMod).GetMethod(nameof(OnCVRBaseLuaBehaviourLoadAndRunScript),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch(
            typeof(CVRLuaClientBehaviour).GetMethod(nameof(CVRLuaClientBehaviour.OnDestroy)), // public for some reason
            postfix: new HarmonyMethod(typeof(CVRLuaToolsExtensionMod).GetMethod(nameof(OnCVRBaseLuaBehaviourDestroy),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(
            () => NamedPipeServer.Instance.StartListening());
    }

    #endregion Melon Events

    #region Harmony Patches

    // theres no good place to patch where GetOwnerId & GetObjectId would be valid to call...
    // pls move InitializeMain after Context is created... pls
    private static void OnCVRBaseLuaBehaviourLoadAndRunScript(CVRLuaClientBehaviour __instance)
    {
        LuaHotReloadManager.OnCVRLuaBaseBehaviourLoadAndRunScript(__instance);
    }

    private static void OnCVRBaseLuaBehaviourDestroy(CVRLuaClientBehaviour __instance)
    {
        LuaHotReloadManager.OnCVRLuaBaseBehaviourDestroy(__instance);
        if (CVRLuaClientBehaviourExtensions._isRestarting.ContainsKey(__instance))
            CVRLuaClientBehaviourExtensions._isRestarting.Remove(__instance);
    }

    #endregion Harmony Patches
}