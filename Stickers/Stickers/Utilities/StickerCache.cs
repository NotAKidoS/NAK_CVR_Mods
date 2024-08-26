using BTKUILib.UIObjects.Components;
using MTJobSystem;
using NAK.Stickers.Integrations;
using System.Collections.Concurrent;
using BTKUILib;
using UnityEngine;

namespace NAK.Stickers.Utilities;

public static class StickerCache
{
    #region Constants and Fields
    
    private static readonly string CohtmlResourcesPath = Path.Combine("coui://", "uiresources", "GameUI", "mods", "BTKUI", "images", ModSettings.ModName, "UserImages");
    private static readonly string ThumbnailPath = Path.Combine(Application.dataPath, "StreamingAssets", "Cohtml", "UIResources", "GameUI", "mods", "BTKUI", "images", ModSettings.ModName, "UserImages");
    
    private static readonly ConcurrentQueue<(FileInfo, Button)> _filesToGenerateThumbnails = new();
    private static readonly HashSet<string> _filesBeingProcessed = new();
    
    private static readonly object _isGeneratingThumbnailsLock = new();
    private static bool _isGeneratingThumbnails;
    private static bool IsGeneratingThumbnails {
        get { lock (_isGeneratingThumbnailsLock) return _isGeneratingThumbnails; }
        set { lock (_isGeneratingThumbnailsLock) { _isGeneratingThumbnails = value; } }
    }
    
    #endregion Constants and Fields

    #region Public Methods
    
    public static string GetBtkUiIconName(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return "Stickers-puzzle"; // default icon when shit fucked
        
        string relativePathWithoutExtension = relativePath[..^Path.GetExtension(relativePath).Length];
        return "UserImages/" + relativePathWithoutExtension.Replace('\\', '/');
    }
    
    public static string GetCohtmlResourcesPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return string.Empty;
        
        string relativePathWithoutExtension = relativePath[..^Path.GetExtension(relativePath).Length];
        string cachePath = Path.Combine(CohtmlResourcesPath, relativePathWithoutExtension + ".png");

        // Normalize path
        cachePath = cachePath.Replace('\\', '/');
        return cachePath;
    }

    public static bool IsThumbnailAvailable(string relativePathWithoutExtension)
    {
        if (string.IsNullOrEmpty(relativePathWithoutExtension)) return false;
        
        string thumbnailImagePath = Path.Combine(ThumbnailPath, relativePathWithoutExtension + ".png");
        return File.Exists(thumbnailImagePath);
    }

    public static void EnqueueThumbnailGeneration(FileInfo fileInfo, Button button)
    {
        lock (_filesBeingProcessed)
        {
            if (!_filesBeingProcessed.Add(fileInfo.FullName))
                return;
            
            _filesToGenerateThumbnails.Enqueue((fileInfo, button));
            MTJobManager.RunOnMainThread("StartGeneratingThumbnailsIfNeeded", StartGeneratingThumbnailsIfNeeded);
        }
    }
    
    #endregion Public Methods
    
    #region Private Methods

    private static void StartGeneratingThumbnailsIfNeeded()
    {
        if (IsGeneratingThumbnails) return;
        IsGeneratingThumbnails = true;

        StickerMod.Logger.Msg("Starting thumbnail generation task.");

        Task.Run(() =>
        {
            try
            {
                Thread.CurrentThread.IsBackground = false; // working around bug in MTJobManager
                
                int generatedThumbnails = 0;

                while (BTKUIAddon.IsPopulatingPage || _filesToGenerateThumbnails.Count > 0)
                {
                    if (!_filesToGenerateThumbnails.TryDequeue(out (FileInfo, Button) fileInfo)) continue;

                    bool success = GenerateThumbnail(fileInfo.Item1);
                    if (success && fileInfo.Item2.ButtonTooltip[5..] == fileInfo.Item1.Name)
                    {
                        var iconPath = GetBtkUiIconName(Path.GetRelativePath(StickerSystem.GetStickersFolderPath(), fileInfo.Item1.FullName));
                        fileInfo.Item2.ButtonIcon = iconPath;
                    }

                    lock (_filesBeingProcessed)
                    {
                        _filesBeingProcessed.Remove(fileInfo.Item1.FullName);
                    }

                    generatedThumbnails++;
                }

                StickerMod.Logger.Msg($"Finished thumbnail generation for {generatedThumbnails} files.");
            }
            catch (Exception e)
            {
                StickerMod.Logger.Error($"Failed to generate thumbnails: {e.Message}");
            }
            finally
            {
                IsGeneratingThumbnails = false;
                Thread.CurrentThread.IsBackground = true; // working around bug in MTJobManager
            }
        });
    }

    private static bool GenerateThumbnail(FileSystemInfo fileInfo)
    {
        string relativePath = Path.GetRelativePath(StickerSystem.GetStickersFolderPath(), fileInfo.FullName);
        string relativePathWithoutExtension = relativePath[..^fileInfo.Extension.Length];
        string thumbnailDirectory = Path.GetDirectoryName(Path.Combine(ThumbnailPath, relativePathWithoutExtension + ".png"));
        if (thumbnailDirectory == null)
            return false;

        if (!Directory.Exists(thumbnailDirectory)) Directory.CreateDirectory(thumbnailDirectory);

        MemoryStream imageStream = LoadStreamFromFile(fileInfo.FullName);
        if (imageStream == null) return false;

        try
        {
            ImageUtility.Resize(ref imageStream, 128, 128);
        }
        catch (Exception e)
        {
            StickerMod.Logger.Warning($"Failed to resize image: {e.Message}");
            imageStream.Dispose();
            return false;
        }
        
        PrepareIconFromMemoryStream(ModSettings.ModName, relativePathWithoutExtension, imageStream);
        imageStream.Dispose();
        return true;
    }

    private static void PrepareIconFromMemoryStream(string modName, string iconPath, MemoryStream destination)
    {
        if (destination == null)
        {
            StickerMod.Logger.Error("Mod " + modName + " attempted to prepare " + iconPath + " but the resource stream was null! Yell at the mod author to fix this!");
        }
        else
        {
            //iconPath = UIUtils.GetCleanString(iconPath);
            iconPath = Path.Combine(ThumbnailPath, iconPath);
            File.WriteAllBytes(iconPath + ".png", destination.ToArray());
            //StickerMod.Logger.Msg("Prepared icon: " + iconPath);
        }
    }
    
    private static MemoryStream LoadStreamFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
                
            MemoryStream memoryStream = new();
            fileStream.CopyTo(memoryStream);
            memoryStream.Position = 0; // Ensure the position is reset before returning
            return memoryStream;
        }
        catch (Exception e)
        {
            StickerMod.Logger.Warning($"Failed to load stream from {filePath}: {e.Message}");
            return null;
        }
    }
    
    #endregion Private Methods
}