using ABI_RC.Core.Networking;
using DarkRift;
using UnityEngine;

namespace NAK.ShareBubbles.Networking;

public static partial class ModNetwork
{
    #region Private Methods

    private static bool CanSendModNetworkMessage()
        => _isSubscribedToModNetwork && IsConnectedToGameNetwork();
    
    private static bool IsConnectedToGameNetwork()
    {
        return NetworkManager.Instance != null
               && NetworkManager.Instance.GameNetwork != null
               && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;
    }
    
    /// Checks if a Vector3 is invalid (NaN or Infinity).
    private static bool IsInvalidVector3(Vector3 vector)
        => float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z)
           || float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z);

    #endregion Private Methods
}