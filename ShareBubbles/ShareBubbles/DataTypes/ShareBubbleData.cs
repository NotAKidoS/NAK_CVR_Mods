using ABI_RC.Systems.ModNetwork;

namespace NAK.ShareBubbles;

/// <summary>
/// Used to create a bubble. Need to define locally & over-network.
/// </summary>
public struct ShareBubbleData
{
    public uint BubbleId;           // Local ID of the bubble (ContentId & ImplTypeHash), only unique per user
    public uint ImplTypeHash;       // Hash of the implementation type (for lookup in ShareBubbleRegistry)
    
    public string ContentId;        // ID of the content being shared
    
    public ShareRule Rule;          // Rule for sharing the bubble
    public ShareLifetime Lifetime;  // Lifetime of the bubble
    public ShareAccess Access;      // Access given if requesting bubble content share
    public DateTime CreatedAt;      // Time the bubble was created for checking lifetime
    
    public static void AddConverterForModNetwork()
    {
        ModNetworkMessage.AddConverter(Read, Write);
        return;

        ShareBubbleData Read(ModNetworkMessage msg)
        {
            msg.Read(out uint bubbleId);
            msg.Read(out uint implTypeHash);
            msg.Read(out string contentId);
        
            // Pack rule, lifetime, and access into a single byte to save bandwidth
            msg.Read(out byte packedFlags);
            ShareRule rule = (ShareRule)(packedFlags & 0x0F);                 // First 4 bits for Rule
            ShareLifetime lifetime = (ShareLifetime)((packedFlags >> 4) & 0x3); // Next 2 bits for Lifetime
            ShareAccess access = (ShareAccess)((packedFlags >> 6) & 0x3);      // Last 2 bits for Access
        
            // Read timestamp as uint (seconds since epoch) to save space compared to DateTime
            msg.Read(out uint timestamp);
            DateTime createdAt = DateTime.UnixEpoch.AddSeconds(timestamp);
            //ShareBubblesMod.Logger.Msg($"Reading bubble - Seconds from epoch: {timestamp}");
            //ShareBubblesMod.Logger.Msg($"Converted back to time: {createdAt}");
            
            // We do not support time traveling bubbles
            DateTime now = DateTime.UtcNow;
            if (createdAt > now) 
                createdAt = now;

            return new ShareBubbleData
            {
                BubbleId = bubbleId,
                ImplTypeHash = implTypeHash,
                ContentId = contentId,
                Rule = rule,
                Lifetime = lifetime,
                Access = access,
                CreatedAt = createdAt
            };
        }

        void Write(ModNetworkMessage msg, ShareBubbleData data)
        {
            msg.Write(data.BubbleId);
            msg.Write(data.ImplTypeHash);
            msg.Write(data.ContentId);
        
            // Pack flags into a single byte
            byte packedFlags = (byte)(
                ((byte)data.Rule & 0x0F) |                    // First 4 bits for Rule
                (((byte)data.Lifetime & 0x3) << 4) |          // Next 2 bits for Lifetime
                (((byte)data.Access & 0x3) << 6)              // Last 2 bits for Access
            );
            msg.Write(packedFlags);
        
            // Write timestamp as uint seconds since epoch
            uint timestamp = (uint)(data.CreatedAt.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
            //ShareBubblesMod.Logger.Msg($"Writing bubble - Original time: {data.CreatedAt}, Converted to seconds: {timestamp}");
            msg.Write(timestamp);
        }
    }
}