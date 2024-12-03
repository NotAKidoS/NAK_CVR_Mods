using NAK.ShareBubbles.Impl;

namespace NAK.ShareBubbles;

public delegate IShareBubbleImpl BubbleImplFactory();

// This is all so fucked because I wanted to allow for custom bubble types, so Stickers could maybe be shared via ShareBubbles
// but it is aaaaaaaaaaaaaaaaaaaaaaa

public static class ShareBubbleRegistry
{
    #region Type Registration

    private static readonly Dictionary<uint, BubbleImplFactory> registeredTypes = new();
    
    public static void RegisterBubbleType(uint typeHash, BubbleImplFactory factory)
    {
        registeredTypes[typeHash] = factory;
    }
    
    public static bool TryCreateImplementation(uint typeHash, out IShareBubbleImpl implementation)
    {
        implementation = null;
        if (!registeredTypes.TryGetValue(typeHash, out BubbleImplFactory factory)) 
            return false;
        
        implementation = factory();
        return implementation != null;
    }

    #endregion Type Registration
}