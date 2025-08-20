using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using HarmonyLib;

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
     [HarmonyPatch(typeof(ControllerRay), nameof(ControllerRay.HandleSpawnableClicked))]
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
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), nameof(ViewManager.RegisterShareEvents))]
    public static void Postfix_ViewManager_RegisterShareEvents(ViewManager __instance) 
    {
        __instance.cohtmlView.View.BindCall("NAKCallShareContent", OnShareContent);
        __instance.cohtmlView.Listener.FinishLoad += (_) => {
            __instance.cohtmlView.View._view.ExecuteScript(
"""
(function waitForContentShare(){
   if (typeof ContentShare !== 'undefined') {
       ContentShare.HasShareBubbles = true;
       console.log('ShareBubbles patch applied');
   } else {
       setTimeout(waitForContentShare, 50);
   }
})();
""");
        };

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
    }
}