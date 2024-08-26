using System.Diagnostics;
using MTJobSystem;
using NAK.Stickers.Networking;
using NAK.Stickers.Utilities;
using UnityEngine;

namespace NAK.Stickers;

public partial class StickerSystem
{
    #region Actions

    public static event Action<int, string> OnStickerLoaded;
    public static event Action<int, string> OnStickerLoadFailed;
    
    private static void InvokeOnImageLoaded(int slotIndex, string imageName)
        => MTJobManager.RunOnMainThread("StickersSystem.InvokeOnImageLoaded", 
            () => OnStickerLoaded?.Invoke(slotIndex, imageName));
    
    private static void InvokeOnImageLoadFailed(int slotIndex, string errorMessage)
        => MTJobManager.RunOnMainThread("StickersSystem.InvokeOnImageLoadFailed", 
            () => OnStickerLoadFailed?.Invoke(slotIndex, errorMessage));
    
    #endregion Actions
    
    #region Image Loading

    private static readonly string s_StickersFolderPath = Path.GetFullPath(Application.dataPath + "/../UserData/Stickers/");
    private readonly bool[] _isLoadingImage = new bool[ModSettings.MaxStickerSlots];

    private void LoadAllImagesAtStartup()
    {
        string[] selectedStickers = ModSettings.Hidden_SelectedStickerNames.Value;
        for (int i = 0; i < ModSettings.MaxStickerSlots; i++)
        {
            if (i >= selectedStickers.Length || string.IsNullOrEmpty(selectedStickers[i])) continue;
            LoadImage(selectedStickers[i], i);
        }
    }
    
    public void LoadImage(string imageName, int slotIndex)
    {
        if (string.IsNullOrEmpty(imageName) || slotIndex < 0 || slotIndex >= _isLoadingImage.Length)
            return;

        if (_isLoadingImage[slotIndex]) return;
        _isLoadingImage[slotIndex] = true;

        Task.Run(() =>
        {
            try
            {
                Thread.CurrentThread.IsBackground = false; // working around bug in MTJobManager
                if (!TryLoadImage(imageName, slotIndex, out string errorMessage))
                    throw new Exception(errorMessage);
            }
            catch (Exception ex)
            {
                //StickerMod.Logger.Error($"Failed to load sticker for slot {slotIndex}: {ex.Message}");
                InvokeOnImageLoadFailed(slotIndex, ex.Message);
            }
            finally
            {
                _isLoadingImage[slotIndex] = false;
                Thread.CurrentThread.IsBackground = true; // working around bug in MTJobManager
            }
        });
    }

    private bool TryLoadImage(string imageName, int slotIndex, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (ModNetwork.IsSendingTexture)
        {
            //StickerMod.Logger.Warning("A texture is currently being sent over the network. Cannot load a new image yet.");
            errorMessage = "A texture is currently being sent over the network. Cannot load a new image yet.";
            return false;
        }

        if (!Directory.Exists(s_StickersFolderPath)) Directory.CreateDirectory(s_StickersFolderPath);

        string imagePath = Path.Combine(s_StickersFolderPath, imageName);
        FileInfo fileInfo = new(imagePath);
        if (!fileInfo.Exists)
        {
            //StickerMod.Logger.Warning($"Target image does not exist on disk. Path: {imagePath}");
            errorMessage = "Target image does not exist on disk.";
            return false;
        }

        var bytes = File.ReadAllBytes(imagePath);

        if (!ImageUtility.IsValidImage(bytes))
        {
            //StickerMod.Logger.Error("File is not a valid image or is corrupt.");
            errorMessage = "File is not a valid image or is corrupt.";
            return false;
        }

        //StickerMod.Logger.Msg("Loaded image from disk. Size in KB: " + bytes.Length / 1024 + " (" + bytes.Length + " bytes)");

        if (bytes.Length > ModNetwork.MaxTextureSize)
        {
            ImageUtility.Resize(ref bytes, 256, 256);
            //StickerMod.Logger.Warning("File ate too many cheeseburgers. Attempting experimental resize. Notice: this may cause filesize to increase.");
            //StickerMod.Logger.Msg("Resized image. Size in KB: " + bytes.Length / 1024 + " (" + bytes.Length + " bytes)");
        }

        if (ImageUtility.ResizeToNearestPowerOfTwo(ref bytes))
        {
            //StickerMod.Logger.Warning("Image resolution was not a power of two. Attempting experimental resize. Notice: this may cause filesize to increase.");
            //StickerMod.Logger.Msg("Resized image. Size in KB: " + bytes.Length / 1024 + " (" + bytes.Length + " bytes)");
        }

        if (bytes.Length > ModNetwork.MaxTextureSize)
        {
            //StickerMod.Logger.Error("File is still too large. Aborting. Size in KB: " + bytes.Length / 1024 + " (" + bytes.Length + " bytes)");
            //StickerMod.Logger.Msg("Please resize the image manually to be smaller than " + ModNetwork.MaxTextureSize / 1024 + " KB and round resolution to nearest power of two.");
            errorMessage = "File is still too large. Please resize the image manually to be smaller than " + ModNetwork.MaxTextureSize / 1024 + " KB and round resolution to nearest power of two.";
            return false;
        }

        //StickerMod.Logger.Msg("Image successfully loaded.");

        MTJobManager.RunOnMainThread("StickersSystem.LoadImage", () =>
        {
            ModSettings.Hidden_SelectedStickerNames.Value[slotIndex] = imageName;
            SetTextureSelf(bytes, slotIndex);
            
            InvokeOnImageLoaded(slotIndex, imageName);

            if (!IsInStickerMode) return;
            IsInStickerMode = false;
            IsInStickerMode = true;
        });

        return true;
    }
    
    public static void OpenStickersFolder()
    {
        if (!Directory.Exists(s_StickersFolderPath)) Directory.CreateDirectory(s_StickersFolderPath);
        Process.Start(s_StickersFolderPath);
    }
    
    public static string GetStickersFolderPath()
    {
        if (!Directory.Exists(s_StickersFolderPath)) Directory.CreateDirectory(s_StickersFolderPath);
        return s_StickersFolderPath;
    }

    #endregion Image Loading
}