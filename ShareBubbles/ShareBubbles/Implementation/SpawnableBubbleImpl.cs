using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.API.UserWebsocket;
using ABI_RC.Core.Player;
using NAK.ShareBubbles.API;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.ShareBubbles.Impl
{
    public class SpawnableBubbleImpl : IShareBubbleImpl
    {
        private ShareBubble bubble;
        private string spawnableId;
        private BubblePedestalInfo details;
        private Texture2D downloadedTexture;

        public bool IsPermitted => details is { IsPermitted: true };
        public string AuthorId => details?.AuthorId;

        public void Initialize(ShareBubble shareBubble)
        {
            bubble = shareBubble;
            spawnableId = shareBubble.Data.ContentId;
            bubble.SetHue(0f);
            bubble.SetEquipButtonLabel("<sprite=0> Select");
        }

        public async Task FetchContentInfo()
        {
            var infoResponse = await PedestalInfoBatchProcessor
                .QueuePedestalInfoRequest(PedestalType.Prop, spawnableId);

            details = new BubblePedestalInfo
            {
                Name = infoResponse.Name,
                ImageUrl = infoResponse.ImageUrl,
                AuthorId = infoResponse.User.Id,
                IsPermitted = infoResponse.Permitted,
                IsPublic = infoResponse.Published,
            };

            downloadedTexture = await ImageCache.GetImageAsync(details.ImageUrl);
            
            // Check if bubble was destroyed before image was downloaded
            if (bubble == null || bubble.gameObject == null)
            {
                Object.Destroy(downloadedTexture);
                return;
            }

            bubble.UpdateContent(new BubbleContentInfo
            {
                Name = details.Name,
                Label = $"<sprite=0> {(details.IsPublic ? "Public" : "Private")} Prop",
                Icon = downloadedTexture
            });
        }

        public void HandleClaimAccept(string userId, Action<bool> onClaimActionCompleted)
        {
            if (details == null)
            {
                onClaimActionCompleted(false);
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    var response = await ShareApiHelper.ShareContentAsync<BaseResponse>(
                        ShareApiHelper.ShareContentType.Spawnable, 
                        spawnableId, 
                        userId);
                    
                    // Store the temporary share to revoke when either party leaves the instance
                    if (bubble.Data.Access == ShareAccess.Session)
                        TempShareManager.Instance.AddTempShare(ShareApiHelper.ShareContentType.Spawnable, 
                            spawnableId, userId);

                    onClaimActionCompleted(response.IsSuccessStatusCode);
                }
                catch (Exception ex)
                {
                    ShareBubblesMod.Logger.Error($"Error sharing spawnable: {ex.Message}");
                    onClaimActionCompleted(false);
                }
            });
        }

        public void ViewDetailsPage()
        {
            if (details == null) return;
            ViewManager.Instance.GetPropDetails(spawnableId);
        }

        public void EquipContent()
        {
            if (details == null || !IsPermitted) return;

            try
            {
                PlayerSetup.Instance.SelectPropToSpawn(
                    spawnableId, 
                    details.ImageUrl, 
                    details.Name);
            }
            catch (Exception ex)
            {
                ShareBubblesMod.Logger.Error($"Error equipping spawnable: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            if (downloadedTexture == null) return;
            Object.Destroy(downloadedTexture);
            downloadedTexture = null;
        }
    }
}