using MTJobSystem;

namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Events

    public static Action<bool> OnTextureOutboundStateChanged;

    private static void InvokeTextureOutboundStateChanged(bool isSending)
    {
        MTJobManager.RunOnMainThread("ModNetwork.InvokeTextureOutboundStateChanged", 
            () => OnTextureOutboundStateChanged?.Invoke(isSending));
    }

    #endregion Events
}