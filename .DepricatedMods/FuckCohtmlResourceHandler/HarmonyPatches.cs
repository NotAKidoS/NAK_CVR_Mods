using cohtml;
using cohtml.Net;
using HarmonyLib;

namespace NAK.FuckCohtmlResourceHandler.HarmonyPatches;

class DefaultResourceHandlerPatches
{
    private const string BadgesUrl = "https://files.abidata.io/static_web/Badges/";
    private const string UserImagesUrl = "https://files.abidata.io/user_images/";
    private const string WorldImagesUrl = "https://files.abidata.io/user_content/";
    private const string AllUrl = "https://files.abidata.io/";

    private static Dictionary<string, bool> blockedUrls = new Dictionary<string, bool>();

    public static void Initialize()
    {
        UpdateBlockedUrls();

        FuckCohtmlResourceHandler.EntryBlockBadgesUrl.OnValueChanged += (_, __) => UpdateBlockedUrls();
        FuckCohtmlResourceHandler.EntryBlockUserImagesUrl.OnValueChanged += (_, __) => UpdateBlockedUrls();
        FuckCohtmlResourceHandler.EntryBlockWorldImagesUrl.OnValueChanged += (_, __) => UpdateBlockedUrls();
        FuckCohtmlResourceHandler.EntryBlockAllUrl.OnValueChanged += (_, __) => UpdateBlockedUrls();
    }

    private static void UpdateBlockedUrls()
    {
        blockedUrls[BadgesUrl] = FuckCohtmlResourceHandler.EntryBlockBadgesUrl.Value;
        blockedUrls[UserImagesUrl] = FuckCohtmlResourceHandler.EntryBlockUserImagesUrl.Value;
        blockedUrls[WorldImagesUrl] = FuckCohtmlResourceHandler.EntryBlockWorldImagesUrl.Value;
        blockedUrls[AllUrl] = FuckCohtmlResourceHandler.EntryBlockAllUrl.Value;
        FuckCohtmlResourceHandler.Logger.Msg("Updated Blocked Urls!");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DefaultResourceHandler), nameof(DefaultResourceHandler.OnResourceRequest))]
    static bool Prefix_DefaultResourceHandler_OnResourceRequest(ref IAsyncResourceRequest request, ref IAsyncResourceResponse response)
    {
        if (FuckCohtmlResourceHandler.EntryEnabled.Value)
        {
            foreach (var url in blockedUrls)
            {
                if (!string.IsNullOrEmpty(url.Key) && request.GetURL().Contains(url.Key) && url.Value)
                {
                    response.Finish(IAsyncResourceResponse.Status.Failure);
                    return false;
                }
            }
        }

        return true;
    }
}