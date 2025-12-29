namespace NAK.Stickers;

public enum StickerSize
{
    Jarret,
    Bean,
    Smol,
    ChonkLite,
    Chonk, // Default (was Medium)
    HeckinChonk,
    DoubleHeckinChonk,
    TripleCursedUnit,
    RealityTearingAbomination,
}

public static class StickerSizeExtensions
{
    public static float GetSizeModifier(this StickerSize size)
    {
        return size switch
        {
            StickerSize.Jarret => 0.125f,
            StickerSize.Bean => 0.2f,
            StickerSize.Smol => 0.25f,
            StickerSize.ChonkLite => 0.5f,
            StickerSize.Chonk => 1f,
            StickerSize.HeckinChonk => 2f,
            StickerSize.DoubleHeckinChonk => 4f,
            StickerSize.TripleCursedUnit => 8f,
            StickerSize.RealityTearingAbomination => 16f,
            _ => 0.125f,
        };
    }
}