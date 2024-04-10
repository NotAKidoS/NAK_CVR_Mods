using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using MelonLoader;
using UnityEngine;

namespace NAK.Nevermind;

public class Nevermind : MelonMod
{
    #region Mod Settings

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(Nevermind));

    private static readonly MelonPreferences_Entry<KeyCode> Setting_NevermindKey =
        Category.CreateEntry("Keybind", KeyCode.Home, description: "Key to cancel world join.");
    
    #endregion

    #region Melon Events
    
    public override void OnUpdate()
    {
        if (!Input.GetKeyDown(Setting_NevermindKey.Value)) 
            return;

        if (CVRObjectLoader.Instance == null
            || CVRDownloadManager.Instance == null)
            return; // game is not ready
        
        if (!CVRObjectLoader.Instance.IsLoadingWorldToJoin())
            return; // no world to cancel

        if (CVRObjectLoader.Instance.WorldBundleRequest != null)
            return; // too late to cancel, world is being loaded
        
        // Cancel world join if still downloading
        CVRDownloadManager.Instance.ActiveWorldDownload = false;
        foreach (var download in CVRDownloadManager.Instance._downloadTasks)
            download.Value.JoinOnComplete = false;
        
        // Cancel world join if still loading
        CVRObjectLoader.Instance.ActiveWorldDownload = null;
        CVRObjectLoader.Instance._isLoadingWorldToJoin = false;
        CVRObjectLoader.Instance.worldLoadingState = CVRObjectLoader.WorldLoadingState.None;
        
        // Notify user of successful cancellation
        LoggerInstance.Msg("World Join Cancelled!");
        ViewManager.Instance.NotifyUser("(Local) Client", "World Join Cancelled", 2f);
    }
    
    #endregion
}