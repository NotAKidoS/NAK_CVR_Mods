using ABI_RC.Core.Networking.API.Responses;

namespace NAK.ShareBubbles.API.Responses;

[Serializable]
public class PedestalInfoResponse_ButCorrect : UgcResponse
{
    public UserDetails User { get; set; }
    public bool Published { get; set; } // Client mislabelled this as Permitted, but it's actually Published
}