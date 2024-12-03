using ABI_RC.Core.Networking.API.Responses;

namespace NAK.ShareBubbles.API.Responses;

/// Same as PedestalInfoResponse, but with an additional field for publication state, if you could not tell by the name.
/// TODO: actually waiting on luc to add Published to PedestalInfoResponse
[Serializable]
public class PedestalInfoResponseButWithPublicationState : UgcResponse
{
    public UserDetails User { get; set; }
    public bool Permitted { get; set; }
    public bool Published { get; set; }
}