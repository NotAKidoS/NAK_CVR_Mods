using ABI_RC.Core.EventSystem;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Exceptions;
using ABI_RC.Core.Networking.API.UserWebsocket;
using NAK.ShareBubbles.API;
using ShareBubbles.ShareBubbles.Implementation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.ShareBubbles.Impl
{
    public class AvatarBubbleImpl : IShareBubbleImpl
    {
        private ShareBubble bubble;
        private string avatarId;
        private BubblePedestalInfo details;
        private Texture2D downloadedTexture;

        public bool IsPermitted => details is { IsPermitted: true };
        public string AuthorId => details?.AuthorId;

        public void Initialize(ShareBubble shareBubble)
        {
            bubble = shareBubble;
            avatarId = shareBubble.Data.ContentId;
            bubble.SetHue(0.5f);
            bubble.SetEquipButtonLabel("<sprite=2> Wear");
        }

        public async Task FetchContentInfo()
        {
            var infoResponse = await PedestalInfoBatchProcessor
                .QueuePedestalInfoRequest(PedestalType.Avatar, avatarId);
            
            details = new BubblePedestalInfo
            {
                Name = infoResponse.Name,
                ImageUrl = infoResponse.ImageUrl,
                AuthorId = infoResponse.User.Id,
                IsPublic = infoResponse.IsPublished,
                IsPermitted = infoResponse.Permitted,
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
                Label = $"<sprite=2> {(details.IsPublic ? "Public" : "Private")} Avatar",
                Icon = downloadedTexture
            });
        }

        public async Task<ShareClaimResult> HandleClaimAccept(string userId)
        {
            if (details == null)
                return ShareClaimResult.Rejected();

            try
            {
                await ShareApiHelper.ShareContentAsync<BaseResponse>(
                    ShareApiHelper.ShareContentType.Avatar, 
                    avatarId, 
                    userId);
                
                // Add to temp shares if session access
                if (bubble.Data.Access == ShareAccess.Session)
                {
                    TempShareManager.Instance.AddTempShare(ShareApiHelper.ShareContentType.Avatar, 
                        avatarId, userId);
                }
            
                return ShareClaimResult.Success(bubble.Data.Access == ShareAccess.Session);
            }
            catch (ContentAlreadySharedException)
            {
                return ShareClaimResult.AlreadyShared();
            }
            catch (UserOnlyAllowsSharesFromFriendsException)
            {
                return ShareClaimResult.FriendsOnly();
            }
            catch (Exception ex)
            {
                ShareBubblesMod.Logger.Error($"Error sharing avatar: {ex.Message}");
                return ShareClaimResult.Rejected();
            }
        }

        public void ViewDetailsPage()
        {
            if (details == null) return;
            ViewManager.Instance.RequestAvatarDetailsPage(avatarId);
        }

        public void EquipContent()
        {
            if (details == null) return;
            AssetManagement.Instance.LoadLocalAvatar(avatarId);
        }

        public void Cleanup()
        {
            if (downloadedTexture == null) return;
            Object.Destroy(downloadedTexture);
            downloadedTexture = null;
        }
    }
}