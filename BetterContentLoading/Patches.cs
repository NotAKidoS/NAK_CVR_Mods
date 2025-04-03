using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using HarmonyLib;
using NAK.BetterContentLoading.Util;

namespace NAK.BetterContentLoading.Patches;

internal static class CVRDownloadManager_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVRDownloadManager), nameof(CVRDownloadManager.QueueTask))]
    private static bool Prefix_CVRDownloadManager_QueueTask(
        string assetId, 
        DownloadTask2.ObjectType type, 
        string assetUrl, 
        string fileId, 
        long fileSize, 
        string fileKey, 
        string toAttach,
        string fileHash = null, 
        UgcTagsData tagsData = null, 
        CVRLoadingAvatarController loadingAvatarController = null,
        bool joinOnComplete = false, 
        bool isHomeRequested = false, 
        int compatibilityVersion = 0, 
        int encryptionAlgorithm = 0,
        string spawnerId = null)
    {
        DownloadInfo info;

        switch (type)
        {
            case DownloadTask2.ObjectType.Avatar:
                info = new DownloadInfo(
                    assetId, assetUrl, fileId, fileSize, fileKey, fileHash,
                    compatibilityVersion, encryptionAlgorithm, tagsData);
                BetterDownloadManager.Instance.QueueAvatarDownload(in info, toAttach, loadingAvatarController);
                return true;
            case DownloadTask2.ObjectType.Prop:
                info = new DownloadInfo(
                    assetId, assetUrl, fileId, fileSize, fileKey, fileHash,
                    compatibilityVersion, encryptionAlgorithm, tagsData);
                BetterDownloadManager.Instance.QueuePropDownload(in info, toAttach, spawnerId);
                return true;
            case DownloadTask2.ObjectType.World:
                _ = ThreadingHelper.RunOffMainThreadAsync(() =>
                {
                    var response = ApiConnection.MakeRequest<UgcWithFile>(
                        ApiConnection.ApiOperation.WorldMeta,
                        new { worldID = assetId }
                    );

                    if (response?.Result.Data == null) 
                        return;
                    
                    info = new DownloadInfo(
                        assetId, response.Result.Data.FileLocation, response.Result.Data.FileId, 
                        response.Result.Data.FileSize, response.Result.Data.FileKey, response.Result.Data.FileHash,
                        (int)response.Result.Data.CompatibilityVersion, (int)response.Result.Data.EncryptionAlgorithm, 
                        null);

                    BetterDownloadManager.Instance.QueueWorldDownload(in info, joinOnComplete, isHomeRequested);
                }, CancellationToken.None);
                return true;
            default:
                return true;
        }
    }
}