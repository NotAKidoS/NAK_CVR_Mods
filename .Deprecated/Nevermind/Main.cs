using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using MelonLoader;
using UnityEngine;

namespace NAK.Nevermind;

public class NevermindMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(NevermindMod));

    private static readonly MelonPreferences_Entry<KeyCode> Entry_CancelKeybind =
        Category.CreateEntry("keybind", KeyCode.Home, description: "Key to cancel world join.");
    
    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnUpdate()
    {
        if (!Input.GetKeyDown(Entry_CancelKeybind.Value)) 
            return;

        if (CVRObjectLoader.Instance == null
            || CVRDownloadManager.Instance == null)
            return; // game is not ready
        
        if (!CVRObjectLoader.Instance.IsLoadingWorldToJoin())
            return; // no world to cancel

        if (CVRObjectLoader.Instance.WorldBundleRequest != null)
            return; // too late to cancel, world is being loaded
        
        // Cancel world join if still downloading
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
    
    #endregion Melon Events
}