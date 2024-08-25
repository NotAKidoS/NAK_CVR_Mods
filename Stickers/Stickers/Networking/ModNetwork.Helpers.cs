using ABI_RC.Core.Networking;
using DarkRift;

namespace NAK.Stickers.Networking;

public static partial class ModNetwork
{
    #region Private Methods

    private static bool IsConnectedToGameNetwork()
    {
        return NetworkManager.Instance != null
               && NetworkManager.Instance.GameNetwork != null
               && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;
    }

    #endregion Private Methods
}