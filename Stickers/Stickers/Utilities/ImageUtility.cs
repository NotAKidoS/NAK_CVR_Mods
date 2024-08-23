using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;

namespace NAK.Stickers.Utilities;

// i do not know shit about images
// TODO: optimize Image usage, attempt create, reuse when valid

public static class ImageUtility
{
    public static bool IsValidImage(byte[] image)
    {
        try
        {
            using MemoryStream stream = new(image);
            using Image img = Image.FromStream(stream);
            return img.Width > 0 && img.Height > 0;
        }
        catch (Exception e)
        {
            StickerMod.Logger.Error($"[ImageUtility] Failed to validate image: {e}");
            return false;
        }
    }
    
    public static void Resize(ref byte[] image, int width, int height)
    {
        MemoryStream stream = new(image);
        Resize(ref stream, width, height);
        
        image = stream.ToArray();
        stream.Dispose();
    }
    
    public static void Resize(ref MemoryStream stream, int width, int height)
    {
        using Image source = Image.FromStream(stream);
        using Bitmap bitmap = new(width, height);
        Rectangle destRect = new(0, 0, width, height);
    
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    
            using (ImageAttributes wrapMode = new())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(source, destRect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }
        
        stream.Dispose();
        stream = new MemoryStream();
        SaveBitmapPreservingFormat(bitmap, stream);
    }
    
    private static void SaveBitmapPreservingFormat(Bitmap bitmap, Stream stream)
    {
        bool hasTransparency = ImageHasTransparency(bitmap);
        if (!hasTransparency)
        {
            ImageCodecInfo jpegEncoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
            EncoderParameters jpegEncoderParameters = new(1)
            {
                Param = new[]
                {
                    new EncoderParameter(Encoder.Quality, 80L) // basically, fuck you, get smaller
                }
            };
            bitmap.Save(stream, jpegEncoder, jpegEncoderParameters);
        }
        else
        {
            ImageCodecInfo pngEncoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Png.Guid);
            EncoderParameters pngEncoderParameters = new(1)
            {
                Param = new[]
                {
                    //new EncoderParameter(Encoder.Quality, 50L), 
                    new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW), // PNG compression
                    new EncoderParameter(Encoder.ColorDepth, bitmap.PixelFormat == PixelFormat.Format32bppArgb ? 32L : 24L), 
                    new EncoderParameter(Encoder.ScanMethod, (long)EncoderValue.ScanMethodInterlaced),
                }
            };
            bitmap.Save(stream, pngEncoder, pngEncoderParameters);
        }
    }

    private static bool ImageHasTransparency(Bitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).A < 255)
                    return true;
        
        return false; // no transparency found
    }
    
    public static bool IsPowerOfTwo(byte[] image)
    {
        using MemoryStream stream = new(image);
        using Image source = Image.FromStream(stream);
        return IsPowerOfTwo(source.Width) && IsPowerOfTwo(source.Height);
    }

    private static bool IsPowerOfTwo(int value)
    {
        return (value > 0) && (value & (value - 1)) == 0;
    }
    
    public static int NearestPowerOfTwo(int value)
    {
        return (int)Math.Pow(2, Math.Ceiling(Math.Log(value, 2)));
    }
    
    public static int FlooredPowerOfTwo(int value)
    {
        return (int)Math.Pow(2, Math.Floor(Math.Log(value, 2)));
    }

    public static bool ResizeToNearestPowerOfTwo(ref byte[] image)
    {
        using MemoryStream stream = new(image);
        using Image source = Image.FromStream(stream);
        
        if (IsPowerOfTwo(source.Width) && IsPowerOfTwo(source.Height))
            return false; // already power of two

        int newWidth = NearestPowerOfTwo(source.Width);
        int newHeight = NearestPowerOfTwo(source.Height);
        Resize(ref image, newWidth, newHeight);
        return true;
    }
    
    // making the assumption that the above could potentially put an image over my filesize limit
    public static bool ResizeToFlooredPowerOfTwo(byte[] image)
    {
        using MemoryStream stream = new(image);
        using Image source = Image.FromStream(stream);
        if (IsPowerOfTwo(source.Width) && IsPowerOfTwo(source.Height))
            return false; // already power of two

        int newWidth = FlooredPowerOfTwo(source.Width);
        int newHeight = FlooredPowerOfTwo(source.Height);
        Resize(ref image, newWidth, newHeight);
        return true;
    }
    
    public static (Guid hashGuid, int width, int height) ExtractImageInfo(byte[] image)
    {
        using MemoryStream stream = new(image);
        using Image img = Image.FromStream(stream);
        
        int width = img.Width;
        int height = img.Height;

        stream.Position = 0;

        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(stream);
        Guid hashGuid = new(hashBytes.Take(16).ToArray());

        return (hashGuid, width, height);
    }
}