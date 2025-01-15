using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.API.UserWebsocket;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using MTJobSystem;
using NAK.ShareBubbles.API;
using NAK.ShareBubbles.API.Exceptions;
using NAK.ShareBubbles.API.Responses;
using Newtonsoft.Json;

namespace NAK.ShareBubbles.Patches;

internal static class PlayerSetup_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.SetSafeScale))]
    public static void Postfix_PlayerSetup_SetSafeScale()
    {
        // I wish there was a callback to listen for player scale changes
        ShareBubbleManager.Instance.OnPlayerScaleChanged();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), nameof(PlayerSetup.GetCurrentPropSelectionMode))]
    private static void Postfix_PlayerSetup_GetCurrentPropSelectionMode(ref PlayerSetup.PropSelectionMode __result)
    {
        // Stickers mod uses invalid enum value 4, so we use 5
        // https://github.com/NotAKidoS/NAK_CVR_Mods/blob/d0c8298074c4dcfc089ccb34ed8b8bd7e0b9cedf/Stickers/Patches.cs#L17
        if (ShareBubbleManager.Instance.IsPlacingBubbleMode) __result = (PlayerSetup.PropSelectionMode)5;
    }
}

internal static class ControllerRay_Patches
{
     [HarmonyPostfix]
     [HarmonyPatch(typeof(ControllerRay), nameof(ControllerRay.DeleteSpawnable))]
     public static void Postfix_ControllerRay_DeleteSpawnable(ref ControllerRay __instance)
     {
         if (!__instance._interactDown)
             return; // not interacted, no need to check

         if (PlayerSetup.Instance.GetCurrentPropSelectionMode()
             != PlayerSetup.PropSelectionMode.Delete)
             return; // not in delete mode, no need to check

         ShareBubble shareBubble = __instance.hitTransform.GetComponentInParent<ShareBubble>();
         if (shareBubble == null) return;
         
         ShareBubbleManager.Instance.DestroyBubble(shareBubble);
     }
     
     [HarmonyPrefix]
     [HarmonyPatch(typeof(ControllerRay), nameof(ControllerRay.HandlePropSpawn))]
     private static void Prefix_ControllerRay_HandlePropSpawn(ref ControllerRay __instance)
     {
         if (!ShareBubbleManager.Instance.IsPlacingBubbleMode) 
             return;
         
         if (__instance._gripDown) ShareBubbleManager.Instance.IsPlacingBubbleMode = false;
         if (__instance._hitUIInternal || !__instance._interactDown)
             return;
        
         ShareBubbleManager.Instance.PlaceSelectedBubbleFromControllerRay(__instance.rayDirectionTransform);
     }
}

internal static class ViewManager_Patches
{
    
private const string DETAILS_TOOLBAR_PATCHES = """

const ContentShareMod = {
    debugMode: false,
    currentContentData: null,
    themeColors: null,

    /* Theme Handling */
    
    getThemeColors: function() {
        if (this.themeColors) return this.themeColors;

        // Default fallback colors
        const defaultColors = {
            background: '#373021',
            border: '#59885d'
        };

        // Try to get colors from favorite category element
        const favoriteCategoryElement = document.querySelector('.favorite-category-selection');
        if (!favoriteCategoryElement) return defaultColors;

        const computedStyle = window.getComputedStyle(favoriteCategoryElement);
        this.themeColors = {
            background: computedStyle.backgroundColor || defaultColors.background,
            border: computedStyle.borderColor || defaultColors.border
        };

        return this.themeColors;
    },

    applyThemeToDialog: function(dialog) {
        const colors = this.getThemeColors();
        dialog.style.backgroundColor = colors.background;
        dialog.style.borderColor = colors.border;

        // Update any close or page buttons to match theme
        const buttons = dialog.querySelectorAll('.close-btn, .page-btn');
        buttons.forEach(button => {
            button.style.borderColor = colors.border;
        });

        return colors;
    },

    /* Core Initialization */
    
    init: function() {
        const styles = [
            this.getSharedStyles(),
            this.ShareBubble.initStyles(),
            this.ShareSelect.initStyles(),
            this.DirectShare.initStyles(),
            this.Unshare.initStyles()
        ].join('\n');

        const styleElement = document.createElement('style');
        styleElement.type = 'text/css';
        styleElement.innerHTML = styles;
        document.head.appendChild(styleElement);

        this.shareBubbleDialog = this.ShareBubble.createDialog();
        this.shareSelectDialog = this.ShareSelect.createDialog();
        this.directShareDialog = this.DirectShare.createDialog();
        this.unshareDialog = this.Unshare.createDialog();
        
        this.initializeToolbars();
        this.bindEvents();
    },

    getSharedStyles: function() {
        return `
            .content-sharing-base-dialog {
                position: fixed;
                background-color: #373021;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                width: 800px;
                min-width: 500px;
                border: 3px solid #59885d;
                padding: 20px;
                z-index: 100000;
                opacity: 0;
                transition: opacity 0.2s linear;
            }

            .content-sharing-base-dialog.in { 
                opacity: 1; 
            }

            .content-sharing-base-dialog.out { 
                opacity: 0; 
            }

            .content-sharing-base-dialog.hidden {
                display: none;
            }

            .content-sharing-base-dialog h2, 
            .content-sharing-base-dialog h3 {
                margin-top: 0;
                margin-bottom: 0.5em;
                text-align: left;
            }
            
            .content-sharing-base-dialog .description {
                margin-bottom: 1em;
                text-align: left;
                font-size: 0.9em;
                color: #aaa;
            }

            .content-sharing-base-dialog .close-btn {
                position: absolute;
                top: 1%;
                right: 1%;
                border-radius: 0.25em;
                border: 3px solid #59885d;
                padding: 0.5em;
                width: 8em;
                text-align: center;
            }

            .page-btn {
                border-radius: 0.25em;
                border: 3px solid #59885d;
                padding: 0.5em;
                width: 8em;
                text-align: center;
            }

            .page-btn:disabled {
                opacity: 0.5;
                cursor: not-allowed;
            }

            .inp-hidden {
                display: none;
            }
        `;
    },

    /* Feature Modules */
    
    ShareBubble: {
        initStyles: function() {
            return `
                .share-bubble-dialog {
                    max-width: 800px;
                    transform: translate(-50%, -60%);
                }

                .share-bubble-dialog .content-btn {
                    position: relative;
                    margin-bottom: 0.5em;
                }

                .share-bubble-dialog .btn-group {
                    display: flex;
                    margin-bottom: 1em;
                }

                .share-bubble-dialog .option-select {
                    flex: 1 1 0;
                    min-width: 0;
                    background-color: inherit;
                    text-align: center;
                    padding: 0.5em;
                    cursor: pointer;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    border: 1px solid inherit;
                }

                .share-bubble-dialog .option-select + .option-select {
                    margin-left: -1px;
                }

                .share-bubble-dialog .option-select.active,
                .share-bubble-dialog .option-select:hover {
                    background-color: rgba(27, 80, 55, 1);
                }

                .share-bubble-dialog .action-buttons {
                    margin-top: 1em;
                    display: flex;
                    justify-content: space-between;
                }

                .share-bubble-dialog .action-btn {
                    flex: 1;
                    text-align: center;
                    padding: 0.5em;
                    border-radius: 0.25em;
                    cursor: pointer;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    margin-right: 0.5em;
                }

                .share-bubble-dialog .action-btn:last-child {
                    margin-right: 0;
                }
            `;
        },

        createDialog: function() {
            const dialog = document.createElement('div');
            dialog.id = 'content-share-bubble-dialog';
            dialog.className = 'content-sharing-base-dialog share-bubble-dialog hidden';
            dialog.innerHTML = `
                <h2>Share Bubble</h2>
                <div class="close-btn button" onclick="ContentShareMod.ShareBubble.hide();">Close</div>

                <h3>Visibility</h3>
                <p class="description">Choose who can see your Sharing Bubble.</p>
                <div class="content-btn btn-group">
                    <div class="option-select visibility-btn active" data-visibility-value="Everyone" 
                         onclick="ContentShareMod.ShareBubble.changeVisibility(this);">Everyone</div>
                    <div class="option-select visibility-btn" data-visibility-value="FriendsOnly" 
                         onclick="ContentShareMod.ShareBubble.changeVisibility(this);">Friends Only</div>
                </div>

                <h3>Lifetime</h3>
                <p class="description">How long the Sharing Bubble lasts. You can delete it at any time.</p>
                <div class="content-btn btn-group">
                    <div class="option-select duration-btn active" data-duration-value="2Minutes" 
                         onclick="ContentShareMod.ShareBubble.changeDuration(this);">2 Minutes</div>
                    <div class="option-select duration-btn" data-duration-value="Session" 
                         onclick="ContentShareMod.ShareBubble.changeDuration(this);">For Session</div>
                </div>

                <div class="grant-access-section">
                    <h3>Access Control</h3>
                    <div class="access-descriptions">
                        <p class="description access-desc" data-access-type="Permanent" style="display: none;">
                            Users will keep access to this Private Content. You can manage permissions later on the Hub.
                        </p>
                        <p class="description access-desc" data-access-type="Session" style="display: none;">
                            Users can only access this Private Content while in the same instance.
                        </p>
                        <p class="description access-desc" data-access-type="None">
                            Users cannot access your Private Content through this share.
                        </p>
                    </div>
                    <div class="content-btn btn-group">
                        <div class="option-select access-btn" data-access-value="Permanent" 
                             onclick="ContentShareMod.ShareBubble.changeAccess(this);">Keep</div>
                        <div class="option-select access-btn" data-access-value="Session" 
                             onclick="ContentShareMod.ShareBubble.changeAccess(this);">Instance Only</div>
                        <div class="option-select access-btn active" data-access-value="None" 
                             onclick="ContentShareMod.ShareBubble.changeAccess(this);">None</div>
                    </div>
                </div>

                <div class="no-access-control-message" style="display: none;">
                    <h3>Access Control</h3>
                    <p class="description">You cannot control access to this content because it is Public or you do not own it.</p>
                </div>

                <input id="share-visibility" class="inp-hidden" value="Everyone">
                <input id="share-duration" class="inp-hidden" value="2Minutes">
                <input id="share-access" class="inp-hidden" value="None">

                <div class="action-buttons">
                    <div class="action-btn button" onclick="ContentShareMod.ShareBubble.submit('drop');">Drop</div>
                    <div class="action-btn button" onclick="ContentShareMod.ShareBubble.submit('select');">Select</div>
                </div>
            `;
            document.body.appendChild(dialog);
            return dialog;
        },

        show: function(contentDetails) {
            const dialog = ContentShareMod.shareBubbleDialog;
            const colors = ContentShareMod.applyThemeToDialog(dialog);
            
            // Additional ShareBubble-specific theming
            const optionSelects = dialog.querySelectorAll('.option-select');
            optionSelects.forEach(element => {
                element.style.borderColor = colors.border;
            });

            const grantAccessSection = dialog.querySelector('.grant-access-section');
            const noAccessControlMessage = dialog.querySelector('.no-access-control-message');
            const showGrantAccess = contentDetails.IsMine && !contentDetails.IsPublic;

            if (grantAccessSection && noAccessControlMessage) {
                grantAccessSection.style.display = showGrantAccess ? '' : 'none';
                noAccessControlMessage.style.display = showGrantAccess ? 'none' : '';
            }

            dialog.classList.remove('hidden', 'out');
            setTimeout(() => dialog.classList.add('in'), 50);

            ContentShareMod.currentContentData = contentDetails;
        },

        hide: function() {
            const dialog = ContentShareMod.shareBubbleDialog;
            dialog.classList.remove('in');
            dialog.classList.add('out');
            setTimeout(() => {
                dialog.classList.add('hidden');
                dialog.classList.remove('out');
            }, 200);
        },

        changeVisibility: function(element) {
            document.getElementById('share-visibility').value = element.dataset.visibilityValue;
            const buttons = ContentShareMod.shareBubbleDialog.querySelectorAll('.visibility-btn');
            buttons.forEach(btn => btn.classList.remove('active'));
            element.classList.add('active');
        },

        changeDuration: function(element) {
            document.getElementById('share-duration').value = element.dataset.durationValue;
            const buttons = ContentShareMod.shareBubbleDialog.querySelectorAll('.duration-btn');
            buttons.forEach(btn => btn.classList.remove('active'));
            element.classList.add('active');
        },

        changeAccess: function(element) {
            document.getElementById('share-access').value = element.dataset.accessValue;
            const buttons = ContentShareMod.shareBubbleDialog.querySelectorAll('.access-btn');
            buttons.forEach(btn => btn.classList.remove('active'));
            element.classList.add('active');
            
            const descriptions = ContentShareMod.shareBubbleDialog.querySelectorAll('.access-desc');
            descriptions.forEach(desc => {
                desc.style.display = desc.dataset.accessType === element.dataset.accessValue ? '' : 'none';
            });
        },

        submit: function(action) {
            const contentDetails = ContentShareMod.currentContentData;
            let bubbleImpl, bubbleContent, contentImage, contentName;

            if (contentDetails.AvatarId) {
                bubbleImpl = 'Avatar';
                bubbleContent = contentDetails.AvatarId;
                contentImage = contentDetails.AvatarImageCoui;
                contentName = contentDetails.AvatarName;
            } else if (contentDetails.SpawnableId) {
                bubbleImpl = 'Spawnable';
                bubbleContent = contentDetails.SpawnableId;
                contentImage = contentDetails.SpawnableImageCoui;
                contentName = contentDetails.SpawnableName;
            } else if (contentDetails.WorldId) {
                bubbleImpl = 'World';
                bubbleContent = contentDetails.WorldId;
                contentImage = contentDetails.WorldImageCoui;
                contentName = contentDetails.WorldName;
            } else if (contentDetails.UserId) {
                bubbleImpl = 'User';
                bubbleContent = contentDetails.UserId;
                contentImage = contentDetails.UserImageCoui;
                contentName = contentDetails.UserName;
            } else {
                console.error('No valid content ID found');
                return;
            }

            const visibility = document.getElementById('share-visibility').value;
            const duration = document.getElementById('share-duration').value;
            const access = document.getElementById('share-access').value;

            const shareRule = visibility === 'FriendsOnly' ? 'FriendsOnly' : 'Everyone';
            const shareLifetime = duration === 'Session' ? 'Session' : 'TwoMinutes';
            const shareAccess = access === 'Permanent' ? 'Permanent' : 
                              access === 'Session' ? 'Session' : 'NoAccess';

            if (ContentShareMod.debugMode) {
                console.log('Sharing content:', {
                    action, bubbleImpl, bubbleContent, 
                    shareRule, shareLifetime, shareAccess
                });
            }

            engine.call('NAKCallShareContent', action, bubbleImpl, bubbleContent, 
                       shareRule, shareLifetime, shareAccess, contentImage, contentName);

            this.hide();
        }
    },

    Unshare: {
        currentPage: 1,
        totalPages: 1,
        sharesPerPage: 5,
        sharesList: null,

        initStyles: function() {
            return `
                .unshare-dialog {
                    width: 800px;
                    height: 1000px;
                    transform: translate(-50%, -60%);
                    display: flex;
                    flex-direction: column;
                }

                .unshare-dialog .shares-container {
                    flex: 1;
                    overflow-y: auto;
                    margin: 20px 0;
                    min-height: 0;
                }

                .unshare-dialog #shares-loading,
                .unshare-dialog #shares-error,
                .unshare-dialog #shares-empty {
                    text-align: center;
                    padding: 2em;
                    font-size: 1.1em;
                }

                .unshare-dialog #shares-error {
                    color: #ff6b6b;
                }

                .unshare-dialog .share-item {
                    display: flex;
                    align-items: center;
                    padding: 15px;
                    border: 1px solid rgba(255, 255, 255, 0.1);
                    margin-bottom: 10px;
                    background: rgba(0, 0, 0, 0.2);
                    border-radius: 4px;
                    min-height: 120px;
                }

                .unshare-dialog .share-item img {
                    width: 96px;
                    height: 96px;
                    border-radius: 4px;
                    margin-right: 15px;
                    cursor: pointer;
                }

                .unshare-dialog .share-item .user-name {
                    flex: 1;
                    font-size: 1.3em;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    margin-right: 15px;
                    cursor: pointer;
                }

                .unshare-dialog .action-btn {
                    width: 180px;
                    height: 60px;
                    font-size: 1.2em;
                    color: white;
                    border-radius: 4px;
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                    margin-left: 10px;
                    padding: 0 20px;
                    text-align: center;
                }

                .unshare-dialog .revoke-btn {
                    background-color: #ff6b6b;
                }

                .unshare-dialog .undo-btn {
                    background-color: #4a9eff;
                }

                .unshare-dialog .action-btn:disabled {
                    opacity: 0.5;
                    cursor: not-allowed;
                    background-color: #666;
                }

                .unshare-dialog .pagination {
                    border-top: 1px solid rgba(255, 255, 255, 0.1);
                    padding-top: 20px;
                }

                .unshare-dialog .page-info {
                    font-size: 1.1em;
                    opacity: 0.8;
                    margin-bottom: 15px;
                    text-align: center;
                }

                .unshare-dialog .page-buttons {
                    display: flex;
                    justify-content: center;
                    gap: 20px;
                }

                .unshare-dialog .share-item.revoked {
                    opacity: 0.7;
                }
            `;
        },

        createDialog: function() {
            const dialog = document.createElement('div');
            dialog.id = 'content-unshare-dialog';
            dialog.className = 'content-sharing-base-dialog unshare-dialog hidden';
            dialog.innerHTML = `
                <h2>Manage Shares</h2>
                <div class="close-btn button" onclick="ContentShareMod.Unshare.hide();">Close</div>
                
                <div class="shares-container">
                    <div id="shares-loading">Loading shares...</div>
                    <div id="shares-error" style="display: none;">Failed to load shares. Please try again.</div>
                    <div id="shares-empty" style="display: none;">No active shares found.</div>
                    <div id="shares-list" style="display: none;"></div>
                </div>
                
                <div class="pagination">
                    <div class="page-info">1/1</div>
                    <div class="page-buttons">
                        <div class="page-btn button" onclick="ContentShareMod.Unshare.previousPage();">Previous</div>
                        <div class="page-btn button" onclick="ContentShareMod.Unshare.nextPage();">Next</div>
                    </div>
                </div>
            `;
            document.body.appendChild(dialog);
            return dialog;
        },

        show: function(contentDetails) {
            const dialog = ContentShareMod.unshareDialog;
            ContentShareMod.applyThemeToDialog(dialog);

            dialog.classList.remove('hidden', 'out');
            setTimeout(() => dialog.classList.add('in'), 50);

            ContentShareMod.currentContentData = contentDetails;
            this.currentPage = 1;
            this.totalPages = 1;
            this.sharesList = null;
            this.requestShares();
        },

        hide: function() {
            const dialog = ContentShareMod.unshareDialog;
            dialog.classList.remove('in');
            dialog.classList.add('out');
            setTimeout(() => {
                dialog.classList.add('hidden');
                dialog.classList.remove('out');
            }, 200);
        },

        requestShares: function() {
            const dialog = ContentShareMod.unshareDialog;
            const sharesContainer = dialog.querySelector('.shares-container');
            
            sharesContainer.querySelector('#shares-loading').style.display = '';
            sharesContainer.querySelector('#shares-error').style.display = 'none';
            sharesContainer.querySelector('#shares-empty').style.display = 'none';
            sharesContainer.querySelector('#shares-list').style.display = 'none';

            const contentDetails = ContentShareMod.currentContentData;
            const contentType = contentDetails.AvatarId ? 'Avatar' : 'Spawnable';
            const contentId = contentDetails.AvatarId || contentDetails.SpawnableId;

            engine.call('NAKGetContentShares', contentType, contentId);
        },

        handleSharesResponse: function(success, shares) {
            const dialog = ContentShareMod.unshareDialog;
            const sharesContainer = dialog.querySelector('.shares-container');
            const loadingElement = sharesContainer.querySelector('#shares-loading');
            const errorElement = sharesContainer.querySelector('#shares-error');
            const emptyElement = sharesContainer.querySelector('#shares-empty');
            const sharesListElement = sharesContainer.querySelector('#shares-list');
            
            loadingElement.style.display = 'none';
            
            if (!success) {
                errorElement.style.display = '';
                return;
            }

            try {
                const response = JSON.parse(shares);
                this.sharesList = response.Data.value;
                
                if (!this.sharesList || this.sharesList.length === 0) {
                    emptyElement.style.display = '';
                    const pagination = dialog.querySelector('.pagination');
                    const [prevButton, nextButton] = pagination.querySelectorAll('.page-btn');
                    prevButton.disabled = true;
                    nextButton.disabled = true;
                    pagination.querySelector('.page-info').textContent = '1/1';
                    return;
                }

                this.totalPages = Math.ceil(this.sharesList.length / this.sharesPerPage);
                this.updatePageContent();
            } catch (error) {
                console.error('Error parsing shares:', error);
                errorElement.style.display = '';
            }
        },

        updatePageContent: function() {
            const dialog = ContentShareMod.unshareDialog;
            const sharesListElement = dialog.querySelector('#shares-list');
            
            const startIndex = (this.currentPage - 1) * this.sharesPerPage;
            const endIndex = startIndex + this.sharesPerPage;
            const currentShares = this.sharesList.slice(startIndex, endIndex);

            sharesListElement.innerHTML = currentShares.map(share => `
                <div class="share-item" data-user-id="${share.id}">
                    <img src="${share.image || '/api/placeholder/96/96'}" 
                         alt="${share.name}'s avatar" 
                         onerror="this.src='/api/placeholder/96/96'"
                         onclick="ContentShareMod.Unshare.viewUserProfile('${share.id}')">
                    <span class="user-name" 
                          onclick="ContentShareMod.Unshare.viewUserProfile('${share.id}')">${share.name}</span>
                    <button class="action-btn revoke-btn button" 
                            onclick="ContentShareMod.Unshare.revokeShare('${share.id}', this)">
                        Revoke
                    </button>
                </div>
            `).join('');

            sharesListElement.style.display = '';

            const pagination = dialog.querySelector('.pagination');
            pagination.querySelector('.page-info').textContent = `${this.currentPage}/${this.totalPages}`;
            const [prevButton, nextButton] = pagination.querySelectorAll('.page-btn');
            prevButton.disabled = this.currentPage === 1;
            nextButton.disabled = this.currentPage === this.totalPages;
        },

        previousPage: function() {
            if (this.currentPage > 1) {
                this.currentPage--;
                this.updatePageContent();
            }
        },

        nextPage: function() {
            if (this.currentPage < this.totalPages) {
                this.currentPage++;
                this.updatePageContent();
            }
        },

        viewUserProfile: function(userId) {
            this.hide();
            getUserDetails(userId);
        },

        revokeShare: function(userId, buttonElement) {
            const contentDetails = ContentShareMod.currentContentData;
            const contentType = contentDetails.AvatarId ? 'Avatar' : 'Spawnable';
            const contentId = contentDetails.AvatarId || contentDetails.SpawnableId;

            buttonElement.disabled = true;
            buttonElement.textContent = 'Revoking...';

            engine.call('NAKRevokeContentShare', contentType, contentId, userId);
        },

        handleRevokeResponse: function(success, userId, error) {
            const dialog = ContentShareMod.unshareDialog;
            const shareItem = dialog.querySelector(`[data-user-id="${userId}"]`);
            if (!shareItem) return;

            const actionButton = shareItem.querySelector('button');

            if (success) {
                shareItem.classList.add('revoked');
                actionButton.className = 'action-btn undo-btn button';
                actionButton.textContent = 'Undo';
                actionButton.onclick = () => {
                    actionButton.disabled = true;
                    actionButton.textContent = 'Restoring...';

                    const contentDetails = ContentShareMod.currentContentData;
                    const contentType = contentDetails.AvatarId ? 'Avatar' : 'Spawnable';
                    const contentId = contentDetails.AvatarId || contentDetails.SpawnableId;

                    engine.call('NAKCallShareContentDirect', contentType, contentId, userId);
                };
                uiPushShow("Share revoked successfully", 3);
            } else {
                actionButton.textContent = 'Failed';
                actionButton.classList.add('failed');
                uiPushShow(error || "Failed to revoke share", 3);
                
                // Reset button after a moment
                setTimeout(() => {
                    actionButton.disabled = false;
                    actionButton.textContent = 'Revoke';
                    actionButton.classList.remove('failed');
                }, 1000);
            }
        },

        handleShareResponse: function(success, userId, error) {
            const dialog = ContentShareMod.unshareDialog;
            const shareItem = dialog.querySelector(`[data-user-id="${userId}"]`);
            if (!shareItem) return;

            const actionButton = shareItem.querySelector('button');

            if (success) {
                this.requestShares();
                uiPushShow("Share restored successfully", 3);
            } else {
                actionButton.textContent = 'Failed';
                actionButton.classList.add('failed');
                uiPushShow(error || "Failed to restore share", 3);
                
                // Reset button after a moment
                setTimeout(() => {
                    actionButton.disabled = false;
                    actionButton.textContent = 'Undo';
                    actionButton.classList.remove('failed');
                }, 1000);
            }
        }
    },
    
    DirectShare: {
        currentPage: 1,
        totalPages: 1,
        usersPerPage: 5,
        usersList: null,
        isInstanceUsers: true,

        initStyles: function() {
            return `
                .direct-share-dialog {
                    width: 800px;
                    height: 1000px;
                    transform: translate(-50%, -60%);
                    display: flex;
                    flex-direction: column;
                }

                .direct-share-dialog .search-container {
                    margin: 20px 0;
                    padding: 10px;
                    background: rgba(0, 0, 0, 0.2);
                    border-radius: 4px;
                }

                .direct-share-dialog .search-input {
                    width: 100%;
                    padding: 10px;
                    border: none;
                    background: transparent;
                    color: inherit;
                    font-size: 1.1em;
                }

                .direct-share-dialog .source-indicator {
                    padding: 10px;
                    text-align: center;
                    opacity: 0.8;
                    background: rgba(0, 0, 0, 0.1);
                    border-radius: 4px;
                    margin-bottom: 20px;
                }

                .direct-share-dialog .users-container {
                    flex: 1;
                    overflow-y: auto;
                    margin-bottom: 20px;
                    min-height: 0;
                }

                .direct-share-dialog .user-item {
                    display: flex;
                    align-items: center;
                    padding: 15px;
                    border: 1px solid rgba(255, 255, 255, 0.1);
                    margin-bottom: 10px;
                    background: rgba(0, 0, 0, 0.2);
                    border-radius: 4px;
                    min-height: 96px;
                }

                .direct-share-dialog .user-item img {
                    width: 96px;
                    height: 96px;
                    margin-right: 15px;
                    cursor: pointer;
                    border-radius: 4px;
                }

                .direct-share-dialog .user-item .user-name {
                    flex: 1;
                    font-size: 1.3em;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    margin-right: 15px;
                    cursor: pointer;
                }

                .direct-share-dialog .user-item .action-btn {
                    width: 140px;
                    height: 50px;
                    font-size: 1.2em;
                    color: white;
                    background-color: rgba(27, 80, 55, 1);
                    border-radius: 4px;
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                }

                .direct-share-dialog .user-item .action-btn:disabled {
                    opacity: 0.5;
                    cursor: not-allowed;
                    background-color: #4a9eff;
                }

                .direct-share-dialog .pagination {
                    border-top: 1px solid rgba(255, 255, 255, 0.1);
                    padding-top: 20px;
                }

                .direct-share-dialog .page-info {
                    font-size: 1.1em;
                    opacity: 0.8;
                    margin-bottom: 15px;
                    text-align: center;
                }

                .direct-share-dialog .page-buttons {
                    display: flex;
                    justify-content: center;
                    gap: 20px;
                }
                
                .direct-share-dialog .user-item .action-btn {
                    width: 180px;
                    height: 60px;
                    font-size: 1.2em;
                    color: white;
                    border-radius: 4px;
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                    margin-left: 10px;
                    padding: 0 20px;
                    text-align: center;
                    background-color: rgba(27, 80, 55, 1);
                }

                .direct-share-dialog .user-item .action-btn:disabled {
                    opacity: 0.5;
                    cursor: not-allowed;
                }

                .direct-share-dialog .user-item .action-btn.shared {
                    background-color: #4a9eff;
                }

                .direct-share-dialog .user-item .action-btn.failed {
                    background-color: #ff6b6b;
                }
            `;
        },

        createDialog: function() {
            const dialog = document.createElement('div');
            dialog.id = 'content-direct-share-dialog';
            dialog.className = 'content-sharing-base-dialog direct-share-dialog hidden';
            dialog.innerHTML = `
                <h2>Direct Share</h2>
                <div class="close-btn button" onclick="ContentShareMod.DirectShare.hide();">Close</div>
                
                <div class="users-container">
                    <div id="users-loading">Loading users...</div>
                    <div id="users-error" style="display: none;">Failed to load users. Please try again.</div>
                    <div id="users-empty" style="display: none;">No users found.</div>
                    <div id="users-list" style="display: none;"></div>
                </div>
                
                <div class="pagination">
                    <div class="page-info">Page 1/1</div>
                    <div class="page-buttons">
                        <div class="page-btn button" onclick="ContentShareMod.DirectShare.previousPage();">Previous</div>
                        <div class="page-btn button" onclick="ContentShareMod.DirectShare.nextPage();">Next</div>
                    </div>
                </div>
            `;
            document.body.appendChild(dialog);
            return dialog;
        },

        show: function(contentDetails) {
            const dialog = ContentShareMod.directShareDialog;
            ContentShareMod.applyThemeToDialog(dialog);
             
            dialog.classList.remove('hidden', 'out');
            setTimeout(() => dialog.classList.add('in'), 50);
             
            ContentShareMod.currentContentData = contentDetails;
            this.currentPage = 1;
            this.totalPages = 1;
            this.usersList = null;
            this.requestUsers(true);
        },

        hide: function() {
            const dialog = ContentShareMod.directShareDialog;
            dialog.classList.remove('in');
            dialog.classList.add('out');
            setTimeout(() => {
                dialog.classList.add('hidden');
                dialog.classList.remove('out');
            }, 200);
        },

        handleUsersResponse: function(success, users, isInstanceUsers) {
            const dialog = ContentShareMod.directShareDialog;
            const usersContainer = dialog.querySelector('.users-container');
            // const sourceIndicator = dialog.querySelector('.source-indicator');
            const loadingElement = usersContainer.querySelector('#users-loading');
            const errorElement = usersContainer.querySelector('#users-error');
            const emptyElement = usersContainer.querySelector('#users-empty');
            const usersListElement = usersContainer.querySelector('#users-list');
            
            loadingElement.style.display = 'none';
            // sourceIndicator.textContent = isInstanceUsers ? 
            //     'Showing users in current instance' : 
            //     'Showing search results';
              
            // TODO: Add source indicator to html:
            // <div class="source-indicator">
            //     Showing users in current instance
            // </div>
            
            if (!success) {
                errorElement.style.display = '';
                return;
            }

            try {
                const response = JSON.parse(users);
                this.usersList = response.entries;
                this.isInstanceUsers = isInstanceUsers;
                
                if (!this.usersList || this.usersList.length === 0) {
                    emptyElement.style.display = '';
                    this.updatePagination();
                    return;
                }

                this.totalPages = Math.ceil(this.usersList.length / this.usersPerPage);
                this.updatePageContent();
            } catch (error) {
                console.error('Error parsing users:', error);
                errorElement.style.display = '';
            }
        },
        
        handleSearch: function(event) {
            if (event.key === 'Enter') {
                const searchValue = event.target.value.trim();
                // Pass true for instance users when empty search, false for search results
                this.requestUsers(searchValue === '', searchValue);
            }
        },

        requestUsers: function(isInstanceUsers, searchQuery = '') {
             const dialog = ContentShareMod.directShareDialog;
             const usersContainer = dialog.querySelector('.users-container');
             
             usersContainer.querySelector('#users-loading').style.display = '';
             usersContainer.querySelector('#users-error').style.display = 'none';
             usersContainer.querySelector('#users-empty').style.display = 'none';
             usersContainer.querySelector('#users-list').style.display = 'none';
             
             engine.call('NAKGetUsersForSharing', searchQuery);
         },

        updatePageContent: function() {
            const dialog = ContentShareMod.directShareDialog;
            const usersListElement = dialog.querySelector('#users-list');
            
            const startIndex = (this.currentPage - 1) * this.usersPerPage;
            const endIndex = startIndex + this.usersPerPage;
            const currentUsers = this.usersList.slice(startIndex, endIndex);

            usersListElement.innerHTML = currentUsers.map(user => `
                <div class="user-item" data-user-id="${user.id}">
                    <img src="${user.image || '/api/placeholder/96/96'}" 
                         alt="${user.name}'s avatar" 
                         onerror="this.src='/api/placeholder/96/96'"
                         onclick="ContentShareMod.DirectShare.viewUserProfile('${user.id}')">
                    <span class="user-name" 
                          onclick="ContentShareMod.DirectShare.viewUserProfile('${user.id}')">${user.name}</span>
                    <button class="action-btn button" 
                            onclick="ContentShareMod.DirectShare.shareWithUser('${user.id}', this)">
                        Share
                    </button>
                </div>
            `).join('');

            usersListElement.style.display = '';
            this.updatePagination();
        },

        updatePagination: function() {
            const dialog = ContentShareMod.directShareDialog;
            const pagination = dialog.querySelector('.pagination');
            const [prevButton, nextButton] = pagination.querySelectorAll('.page-btn');
            
            pagination.querySelector('.page-info').textContent = `Page ${this.currentPage}/${this.totalPages}`;
            prevButton.disabled = this.currentPage === 1;
            nextButton.disabled = this.currentPage === this.totalPages;
        },

        previousPage: function() {
            if (this.currentPage > 1) {
                this.currentPage--;
                this.updatePageContent();
            }
        },

        nextPage: function() {
            if (this.currentPage < this.totalPages) {
                this.currentPage++;
                this.updatePageContent();
            }
        },

        viewUserProfile: function(userId) {
            this.hide();
            getUserDetails(userId);
        },

        shareWithUser: function(userId, buttonElement) {
            const contentDetails = ContentShareMod.currentContentData;
            const contentType = contentDetails.AvatarId ? 'Avatar' : 'Spawnable';
            const contentId = contentDetails.AvatarId || contentDetails.SpawnableId;
            const contentName = contentDetails.AvatarName || contentDetails.SpawnableName;
            const contentImage = contentDetails.AvatarImageURL || contentDetails.SpawnableImageURL;

            buttonElement.disabled = true;
            buttonElement.textContent = 'Sharing...';

            engine.call('NAKCallShareContentDirect', contentType, contentId, userId, contentName, contentImage);
        },

        handleShareResponse: function(success, userId, error) {
            const dialog = ContentShareMod.directShareDialog;
            const userItem = dialog.querySelector(`[data-user-id="${userId}"]`);
            if (!userItem) return;

            const actionButton = userItem.querySelector('button');

            if (success) {
                actionButton.textContent = 'Shared';
                actionButton.disabled = true;
                actionButton.classList.add('shared');
                uiPushShow("Content shared successfully", 3, "shareresponse");
            } else {
                actionButton.disabled = false;
                actionButton.textContent = 'Failed';
                actionButton.classList.add('failed');
                uiPushShow(error || "Failed to share content", 3, "shareresponse");
                
                // Reset button after a moment
                setTimeout(() => {
                    actionButton.disabled = false;
                    actionButton.textContent = 'Share';
                    actionButton.classList.remove('failed', 'shared');
                }, 1000);
            }
        }
    },
    
    ShareSelect: {
        initStyles: function() {
               return `
                   .share-select-dialog {
                       width: 650px;
                       height: 480px;
                       transform: translate(-50%, -80%);
                   }

                   .share-select-dialog .share-options {
                       display: flex;
                       flex-direction: column;
                       gap: 15px;
                       margin-top: 20px;
                   }

                   .share-select-dialog .share-option {
                       padding: 20px;
                       text-align: left;
                       cursor: pointer;
                       background: rgba(0, 0, 0, 0.2);
                       border: 1px solid rgba(255, 255, 255, 0.1);
                       border-radius: 4px;
                       transition: background-color 0.2s ease;
                   }

                   .share-select-dialog .share-option:hover {
                       background-color: rgba(27, 80, 55, 1);
                       border-color: rgba(255, 255, 255, 0.2);
                   }

                   .share-select-dialog h3 {
                       margin: 0 0 8px 0;
                       font-size: 1.2em;
                   }

                   .share-select-dialog p {
                       margin: 0;
                       opacity: 0.8;
                       font-size: 0.95em;
                       line-height: 1.4;
                   }
               `;
           },

        createDialog: function() {
            const dialog = document.createElement('div');
            dialog.id = 'content-share-select-dialog';
            dialog.className = 'content-sharing-base-dialog share-select-dialog hidden';
            dialog.innerHTML = `
                <h2>Share Content</h2>
                <div class="close-btn button" onclick="ContentShareMod.ShareSelect.hide();">Close</div>
                
                <div class="share-options">
                    <div class="share-option" onclick="ContentShareMod.ShareSelect.openShareBubble();">
                        <h3>Share Bubble</h3>
                        <p>Drop or place a bubble in the world that others can interact with</p>
                    </div>
                    
                    <div class="share-option" onclick="ContentShareMod.ShareSelect.openDirectShare();">
                        <h3>Direct Share</h3>
                        <p>Share directly with specific users</p>
                    </div>
                </div>
            `;
            document.body.appendChild(dialog);
            return dialog;
        },

        show: function(contentDetails) {
            const dialog = ContentShareMod.shareSelectDialog;
            ContentShareMod.applyThemeToDialog(dialog);
            
            dialog.classList.remove('hidden', 'out');
            setTimeout(() => dialog.classList.add('in'), 50);
            
            ContentShareMod.currentContentData = contentDetails;
        },

        hide: function() {
            const dialog = ContentShareMod.shareSelectDialog;
            dialog.classList.remove('in');
            dialog.classList.add('out');
            setTimeout(() => {
                dialog.classList.add('hidden');
                dialog.classList.remove('out');
            }, 200);
        },

        openShareBubble: function() {
            this.hide();
            ContentShareMod.ShareBubble.show(ContentShareMod.currentContentData);
        },

        openDirectShare: function() {
            this.hide();
            ContentShareMod.DirectShare.show(ContentShareMod.currentContentData);
        }
    },

    // Toolbar initialization and event bindings
    initializeToolbars: function() {
        const findEmptyButtons = (toolbar) => {
            return Array.from(toolbar.querySelectorAll('.toolbar-btn')).filter(
                btn => btn.textContent.trim() === ""
            );
        };

        const setupToolbar = (selector) => {
            const toolbar = document.querySelector(selector);
            if (!toolbar) return;

            const emptyButtons = findEmptyButtons(toolbar);
            if (emptyButtons.length >= 2) {
                emptyButtons[0].classList.add('content-share-btn');
                emptyButtons[0].textContent = 'Share';

                emptyButtons[1].classList.add('content-unshare-btn');
                emptyButtons[1].textContent = 'Unshare';
            }
        };

        setupToolbar('#avatar-detail .avatar-toolbar');
        setupToolbar('#prop-detail .avatar-toolbar');
    },

    bindEvents: function() {
        // Avatar events
        engine.on("LoadAvatarDetails", (avatarDetails) => {
            const shareBtn = document.querySelector('#avatar-detail .content-share-btn');
            const unshareBtn = document.querySelector('#avatar-detail .content-unshare-btn');
            const canShareDirectly = avatarDetails.IsMine;
            const canUnshare = avatarDetails.IsMine || avatarDetails.IsSharedWithMe;

            if (shareBtn) {
                shareBtn.classList.remove('disabled');
                
                if (canShareDirectly) {
                    shareBtn.onclick = () => ContentShareMod.ShareSelect.show(avatarDetails);
                } else {
                    shareBtn.onclick = () => ContentShareMod.ShareBubble.show(avatarDetails);
                }
            }

            if (unshareBtn) {
                if (canUnshare) {
                    unshareBtn.classList.remove('disabled');
                    unshareBtn.onclick = () => {
                        if (avatarDetails.IsMine) {
                            ContentShareMod.Unshare.show(avatarDetails);
                        } else {
                            uiConfirmShow("Unshare Avatar", 
                                "Are you sure you want to unshare this avatar?",
                                "unshare_avatar_confirmation",
                                avatarDetails.AvatarId);
                        }
                    };
                } else {
                    unshareBtn.classList.add('disabled');
                    unshareBtn.onclick = null;
                }
            }

            ContentShareMod.currentContentData = avatarDetails;
        });

        // Prop events
        engine.on("LoadPropDetails", (propDetails) => {
            const shareBtn = document.querySelector('#prop-detail .content-share-btn');
            const unshareBtn = document.querySelector('#prop-detail .content-unshare-btn');
            const canShareDirectly = propDetails.IsMine;
            const canUnshare = propDetails.IsMine || propDetails.IsSharedWithMe;
                        
            if (shareBtn) {
                shareBtn.classList.remove('disabled');
                
                if (canShareDirectly) {
                    shareBtn.onclick = () => ContentShareMod.ShareSelect.show(propDetails);
                } else {
                    shareBtn.onclick = () => ContentShareMod.ShareBubble.show(propDetails);
                }
            }

            if (unshareBtn) {
                if (canUnshare) {
                    unshareBtn.classList.remove('disabled');
                    unshareBtn.onclick = () => {
                        if (propDetails.IsMine) {
                            ContentShareMod.Unshare.show(propDetails);
                        } else {
                            uiConfirmShow("Unshare Prop",
                                "Are you sure you want to unshare this prop?",
                                "unshare_prop_confirmation",
                                propDetails.SpawnableId);
                        }
                    };
                } else {
                    unshareBtn.classList.add('disabled');
                    unshareBtn.onclick = null;
                }
            }

            ContentShareMod.currentContentData = propDetails;
        });

        // Share response handlers
        engine.on("OnHandleSharesResponse", (success, shares) => {
            if (ContentShareMod.debugMode) {
                console.log('Shares response:', success, shares);
            }
            ContentShareMod.Unshare.handleSharesResponse(success, shares);
        });

        engine.on("OnHandleRevokeResponse", (success, userId, error) => {
            ContentShareMod.Unshare.handleRevokeResponse(success, userId, error);
        });
        
        engine.on("OnHandleShareResponse", function(success, userId, error) {
            // Pass event to Unshare and DirectShare modules depending on which dialog is open
            if (ContentShareMod.unshareDialog && !ContentShareMod.unshareDialog.classList.contains('hidden')) {
                ContentShareMod.Unshare.handleShareResponse(success, userId, error);
            } else if (ContentShareMod.directShareDialog && !ContentShareMod.directShareDialog.classList.contains('hidden')) {
                ContentShareMod.DirectShare.handleShareResponse(success, userId, error);
            }
        });

        // Share release handlers
        engine.on("OnReleasedAvatarShare", (contentId) => {
            if (!ContentShareMod.currentContentData || 
                ContentShareMod.currentContentData.AvatarId !== contentId) return;

            const unshareBtn = document.querySelector('#avatar-detail .content-unshare-btn');
            ContentShareMod.currentContentData.IsSharedWithMe = false;

            if (unshareBtn) {
                unshareBtn.classList.add('disabled');
                unshareBtn.onclick = null;
            }

            const contentIsAccessible = ContentShareMod.currentContentData.IsMine || 
                                     ContentShareMod.currentContentData.IsPublic;
            
            if (!contentIsAccessible) {
                const detail = document.querySelector('#avatar-detail');
                if (detail) {
                    ['drop-btn', 'select-btn', 'fav-btn'].forEach(className => {
                        const button = detail.querySelector('.' + className);
                        if (button) {
                            button.classList.add('disabled');
                            button.removeAttribute('onclick');
                        }
                    });
                }
            }
        });

        engine.on("OnReleasedPropShare", (contentId) => {
            if (!ContentShareMod.currentContentData || 
                ContentShareMod.currentContentData.SpawnableId !== contentId) return;

            const unshareBtn = document.querySelector('#prop-detail .content-unshare-btn');
            ContentShareMod.currentContentData.IsSharedWithMe = false;

            if (unshareBtn) {
                unshareBtn.classList.add('disabled');
                unshareBtn.onclick = null;
            }

            const contentIsAccessible = ContentShareMod.currentContentData.IsMine || 
                                     ContentShareMod.currentContentData.IsPublic;
            
            if (!contentIsAccessible) {
                const detail = document.querySelector('#prop-detail');
                if (detail) {
                    ['drop-btn', 'select-btn', 'fav-btn'].forEach(className => {
                        const button = detail.querySelector('.' + className);
                        if (button) {
                            button.classList.add('disabled');
                            button.removeAttribute('onclick');
                        }
                    });
                }
            }
        });
        
        engine.on("OnHandleUsersResponse", (success, users, isInstanceUsers) => {
            if (ContentShareMod.debugMode) {
                console.log('Users response:', success, isInstanceUsers, users);
            }
            ContentShareMod.DirectShare.handleUsersResponse(success, users, isInstanceUsers);
        });
    }
};

ContentShareMod.init();

""";

    private const string UiConfirmId_ReleaseAvatarShareWarning = "unshare_avatar_confirmation";
    private const string UiConfirmId_ReleasePropShareWarning = "unshare_prop_confirmation";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), nameof(ViewManager.Start))]
    public static void Postfix_ViewManager_Start(ViewManager __instance) 
    {
        // Inject the details toolbar patches when the game menu view is loaded
        __instance.gameMenuView.Listener.FinishLoad += (_) => {
            __instance.gameMenuView.View._view.ExecuteScript(DETAILS_TOOLBAR_PATCHES);
            __instance.gameMenuView.View.BindCall("NAKCallShareContent", OnShareContent);
            __instance.gameMenuView.View.BindCall("NAKGetContentShares", OnGetContentShares);
            __instance.gameMenuView.View.BindCall("NAKRevokeContentShare", OnRevokeContentShare);
            __instance.gameMenuView.View.BindCall("NAKCallShareContentDirect", OnShareContentDirect);
            __instance.gameMenuView.View.BindCall("NAKGetUsersForSharing", OnGetUsersForSharing);
        };
        
        // Add the event listener for the unshare confirmation dialog
        __instance.OnUiConfirm.AddListener(OnReleaseContentShareConfirmation);
        
        return;

        void OnShareContent(
            string action, 
            string bubbleImpl, 
            string bubbleContent, 
            string shareRule, 
            string shareLifetime,
            string shareAccess,
            string contentImage,
            string contentName)
        {
            // Action: drop, select
            // BubbleImpl: Avatar, Prop, World, User
            // BubbleContent: AvatarId, PropId, WorldId, UserId
            // ShareRule: Public, FriendsOnly
            // ShareLifetime: TwoMinutes, Session
            // ShareAccess: PermanentAccess, SessionAccess, NoAccess
    
            ShareRule rule = shareRule switch
            {
                "Everyone" => ShareRule.Everyone,
                "FriendsOnly" => ShareRule.FriendsOnly,
                _ => ShareRule.Everyone
            };
    
            ShareLifetime lifetime = shareLifetime switch
            {
                "Session" => ShareLifetime.Session,
                "TwoMinutes" => ShareLifetime.TwoMinutes,
                _ => ShareLifetime.TwoMinutes
            };
    
            ShareAccess access = shareAccess switch
            {
                "Permanent" => ShareAccess.Permanent,
                "Session" => ShareAccess.Session,
                "None" => ShareAccess.None,
                _ => ShareAccess.None
            };

            uint implTypeHash = ShareBubbleManager.GetMaskedHash(bubbleImpl);
            ShareBubbleData bubbleData = new()
            {
                BubbleId = ShareBubbleManager.GenerateBubbleId(bubbleContent, implTypeHash),
                ImplTypeHash = implTypeHash,
                ContentId = bubbleContent,
                Rule = rule,
                Lifetime = lifetime,
                Access = access,
                CreatedAt = DateTime.UtcNow
            };

            switch (action)
            {
                case "drop":
                    ShareBubbleManager.Instance.DropBubbleInFront(bubbleData);
                    break;
                case "select":
                    ShareBubbleManager.Instance.SelectBubbleForPlace(contentImage, contentName, bubbleData);
                    break;
            }
    
            // Close menu
            ViewManager.Instance.UiStateToggle(false);
        }
        
        void OnReleaseContentShareConfirmation(string id, string value, string contentId)
        {
            // Check if the confirmation event is for unsharing content
            if (id != UiConfirmId_ReleaseAvatarShareWarning
                && id != UiConfirmId_ReleasePropShareWarning)
                return;
            
            //ShareBubblesMod.Logger.Msg($"Unshare confirmation received: {id}, {value}");
            
            // Check if the user confirmed the unshare action
            if (value != "true")
            {
                //ShareBubblesMod.Logger.Msg("Unshare action cancelled by user"); 
                return;
            }
            
            //ShareBubblesMod.Logger.Msg("Releasing share...");
            
            // Determine the content type based on the confirmation ID
            ShareApiHelper.ShareContentType contentType = id == UiConfirmId_ReleaseAvatarShareWarning
                ? ShareApiHelper.ShareContentType.Avatar
                : ShareApiHelper.ShareContentType.Spawnable;
            
            Task.Run(async () => {
                try
                {
                    await ShareApiHelper.ReleaseShareAsync<BaseResponse>(contentType, contentId);
                    MTJobManager.RunOnMainThread("release_share_response", () =>
                    {
                        // Cannot display a success message as opening details page pushes itself to top
                        // after talking to api, so success message would need to be timed to show after
                        // if (contentType == ApiShareHelper.ShareContentType.Avatar)
                        //     ViewManager.Instance.RequestAvatarDetailsPage(contentId);
                        // else
                        //     ViewManager.Instance.GetPropDetails(contentId);

                        ViewManager.Instance.gameMenuView.View._view.TriggerEvent(
                            contentType == ShareApiHelper.ShareContentType.Avatar 
                                ? "OnReleasedAvatarShare" : "OnReleasedPropShare", 
                            contentId);
                        
                        ViewManager.Instance.TriggerPushNotification("Content unshared successfully", 3f);
                    });
                }
                catch (ShareApiException ex)
                {
                    ShareBubblesMod.Logger.Error($"Share API error: {ex.Message}");
                    MTJobManager.RunOnMainThread("release_share_error", () => {
                        ViewManager.Instance.TriggerAlert("Release Share Error", ex.UserFriendlyMessage, -1, true);
                    });
                }
                catch (Exception ex)
                {
                    ShareBubblesMod.Logger.Error($"Unexpected error releasing share: {ex.Message}");
                    MTJobManager.RunOnMainThread("release_share_error", () => {
                        ViewManager.Instance.TriggerAlert("Release Share Error", "An unexpected error occurred", -1, true);
                    });
                }
            });
        }
        
        async void OnGetContentShares(string contentType, string contentId)
        {
            try
            {
                var response = await ShareApiHelper.GetSharesAsync<BaseResponse<ActiveSharesResponse>>(
                    contentType == "Avatar" ? ShareApiHelper.ShareContentType.Avatar : ShareApiHelper.ShareContentType.Spawnable,
                    contentId
                );

                // TODO: somethign better than this cause this is ass and i need to replace the image urls with ImageCache coui ones
                // FUICJK<
                string json = JsonConvert.SerializeObject(response.Data);
                
                // log the json to console
                //ShareBubblesMod.Logger.Msg($"Shares response: {json}");
                
                __instance.gameMenuView.View.TriggerEvent("OnHandleSharesResponse", true, json);
            }
            catch (Exception ex)
            {
                ShareBubblesMod.Logger.Error($"Failed to get content shares: {ex.Message}");
                __instance.gameMenuView.View.TriggerEvent("OnHandleSharesResponse", false);
            }
        }

        async void OnRevokeContentShare(string contentType, string contentId, string userId)
        {
            try
            {
                await ShareApiHelper.ReleaseShareAsync<BaseResponse>(
                    contentType == "Avatar" ? ShareApiHelper.ShareContentType.Avatar : ShareApiHelper.ShareContentType.Spawnable,
                    contentId,
                    userId
                );

                __instance.gameMenuView.View.TriggerEvent("OnHandleRevokeResponse", true, userId);
            }
            catch (ShareApiException ex)
            {
                ShareBubblesMod.Logger.Error($"Share API error revoking share: {ex.Message}");
                __instance.gameMenuView.View.TriggerEvent("OnHandleRevokeResponse", false, userId, ex.UserFriendlyMessage);
            }
            catch (Exception ex)
            {
                ShareBubblesMod.Logger.Error($"Unexpected error revoking share: {ex.Message}");
                __instance.gameMenuView.View.TriggerEvent("OnHandleRevokeResponse", false, userId, "An unexpected error occurred");
            }
        }
        
        async void OnShareContentDirect(string contentType, string contentId, string userId, string contentName = "", string contentImage = "")
        {
            try
            {
                ShareApiHelper.ShareContentType shareContentType = contentType == "Avatar" 
                    ? ShareApiHelper.ShareContentType.Avatar 
                    : ShareApiHelper.ShareContentType.Spawnable;
                
                await ShareApiHelper.ShareContentAsync<BaseResponse>(
                    shareContentType,
                    contentId,
                    userId
                );
                
                // Alert the user that the share occurred
                //ModNetwork.SendDirectShareNotification(userId, shareContentType, contentId);
                
                __instance.gameMenuView.View._view.TriggerEvent("OnHandleShareResponse", true, userId);
            }
            catch (ShareApiException ex)
            {
                ShareBubblesMod.Logger.Error($"Share API error: {ex.Message}");
                __instance.gameMenuView.View._view.TriggerEvent("OnHandleShareResponse", false, userId, ex.UserFriendlyMessage);
            }
            catch (Exception ex)
            {
                ShareBubblesMod.Logger.Error($"Unexpected error sharing content: {ex.Message}");
                __instance.gameMenuView.View._view.TriggerEvent("OnHandleShareResponse", false, userId, "An unexpected error occurred");
            }
        }
        void OnGetUsersForSharing(string searchTerm = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // TODO: Search users implementation will go here
                    // For now just return an empty list
                    var response = new { entries = new List<object>() };
                    string json = JsonConvert.SerializeObject(response);
                    __instance.gameMenuView.View.TriggerEvent("OnHandleUsersResponse", true, json, false);
                }
                else
                {
                    // Get instance users
                    CVRPlayerManager playerManager = CVRPlayerManager.Instance;
                    if (playerManager == null)
                    {
                        __instance.gameMenuView.View.TriggerEvent("OnHandleUsersResponse", false, false, true);
                        return;
                    }

                    var response = new
                    {
                        entries = playerManager.NetworkPlayers
                            .Where(p => p != null && !string.IsNullOrEmpty(p.Uuid) 
                                                  && !MetaPort.Instance.blockedUserIds.Contains(p.Uuid)) // You SHOULDNT HAVE TO DO THIS, but GS dumb
                            .Select(p => new
                            {
                                id = p.Uuid,
                                name = p.Username,
                                image = p.ApiProfileImageUrl
                            })
                            .ToList()
                    };

                    string json = JsonConvert.SerializeObject(response);
                    __instance.gameMenuView.View.TriggerEvent("OnHandleUsersResponse", true, json, true);
                }
            }
            catch (Exception ex)
            {
                ShareBubblesMod.Logger.Error($"Failed to get users: {ex.Message}");
                __instance.gameMenuView.View.TriggerEvent("OnHandleUsersResponse", false, false, string.IsNullOrEmpty(searchTerm));
            }
        }
    }
}