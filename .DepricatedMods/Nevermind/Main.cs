using ABI_RC.Core.IO;
using MelonLoader;
using UnityEngine;

namespace NAK.Nevermind;

public class Nevermind : MelonMod
{
    public override void OnUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Home)) return;

        CancelWorldDownloadJoinOnComplete();
        CancelWorldLoadJoin();
    }

    void CancelWorldDownloadJoinOnComplete()
    {
        var downloadManager = CVRDownloadManager.Instance;
        downloadManager.ActiveWorldDownload = false;
        foreach (var download in downloadManager._downloadTasks)
            download.Value.JoinOnComplete = false;
    }

    void CancelWorldLoadJoin()
    {
        var objectLoader = CVRObjectLoader.Instance;
        if (!objectLoader._isLoadingWorld)
        {
            objectLoader.j.Bytes = null;
            objectLoader.j.ObjectId = null;
            objectLoader.IsLoadingWorldToJoin = false;
            objectLoader.CurrentWorldLoadingStage = -1;
        }
    }
}