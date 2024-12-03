using Newtonsoft.Json;

namespace NAK.ShareBubbles.API.Responses;

public class ActiveSharesResponse
{
    [JsonProperty("value")]
    public List<ShareUser> Value { get; set; }
}

// Idk why not just reuse UserDetails
public class ShareUser
{
    [JsonProperty("image")]
    public string Image { get; set; }
    
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("name")]
    public string Name { get; set; }
}