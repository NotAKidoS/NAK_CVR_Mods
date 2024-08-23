using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MTJobSystem;
using NAK.Stickers.Networking;
using NAK.Stickers.Utilities;

namespace NAK.Stickers.Integrations
{
    public static partial class BtkUiAddon
    {
        private static readonly object _cacheLock = new();
        
        private static bool _initialClearCacheFolder;
        private static Category _stickerSelectionCategory;
        internal static readonly Dictionary<string, ImageInfo> _cachedImages = new();
        
        public static string GetBtkUiCachePath(string stickerName)
        {
            if (_cachedImages.TryGetValue(stickerName, out ImageInfo imageInfo))
                return "coui://uiresources/GameUI/mods/BTKUI/images/" + ModSettings.ModName + "/UserImages/" + imageInfo.cacheName + ".png";
            return string.Empty;
        }

        internal class ImageInfo
        {
            public string filePath;
            public string cacheName; // BTKUI cache path
            public bool wasFoundThisPass;
            public bool hasChanged;
            public DateTime lastModified;
            public Button button;
        }

        #region Setup

        private static void Setup_StickerSelectionCategory(Page page)
        {
            _onOurTabOpened += UpdateStickerSelectionAsync;
            _stickerSelectionCategory = AddMelonCategory(ref page, ModSettings.Hidden_Foldout_SelectionCategory);
            ModNetwork.OnTextureOutboundStateChanged += OnSendingTextureOverNetworkChanged; // disable buttons when sending texture over network
            StickerSystem.OnImageLoadFailed += OnLoadImageFailed;
            GetInitialImageInfo();
        }
        
        private static void GetInitialImageInfo()
        {
            Task.Run(() =>
            {
                try
                {
                    string path = StickerSystem.GetStickersFolderPath();
                    if (!Directory.Exists(path))
                    {
                        StickerMod.Logger.Warning("Stickers folder not found.");
                        return;
                    }

                    var stickerFiles = Directory.EnumerateFiles(path, "*.png");
                    var currentFiles = new HashSet<string>(stickerFiles);

                    lock (_cacheLock)
                    {
                        if (!_initialClearCacheFolder)
                        {
                            _initialClearCacheFolder = true;
                            DeleteOldIcons(ModSettings.ModName);
                        }

                        var keysToRemove = new List<string>();

                        foreach (var stickerFile in currentFiles)
                        {
                            string stickerName = Path.GetFileNameWithoutExtension(stickerFile);
                            
                            if (_cachedImages.TryGetValue(stickerName, out ImageInfo imageInfo))
                            {
                                imageInfo.wasFoundThisPass = true;
                                DateTime lastModified = File.GetLastWriteTime(stickerFile);
                                if (lastModified == imageInfo.lastModified) continue;
                                imageInfo.hasChanged = true;
                                imageInfo.lastModified = lastModified;
                            }
                            else
                            {
                                _cachedImages[stickerName] = new ImageInfo
                                {
                                    filePath = stickerFile,
                                    wasFoundThisPass = true,
                                    hasChanged = true,
                                    lastModified = File.GetLastWriteTime(stickerFile)
                                };
                            }
                        }

                        foreach (var kvp in _cachedImages)
                        {
                            var imageName = kvp.Key;
                            ImageInfo imageInfo = kvp.Value;

                            if (!imageInfo.wasFoundThisPass)
                            {
                                MainThreadInvoke(() =>
                                {
                                    if (imageInfo.button != null)
                                    {
                                        imageInfo.button.Delete();
                                        imageInfo.button = null;
                                    }
                                });

                                if (!string.IsNullOrEmpty(imageInfo.cacheName))
                                {
                                    DeleteOldIcon(ModSettings.ModName, imageInfo.cacheName);
                                    imageInfo.cacheName = string.Empty;
                                }

                                keysToRemove.Add(imageName);
                            }
                            else if (imageInfo.hasChanged)
                            {
                                imageInfo.hasChanged = false;

                                if (!string.IsNullOrEmpty(imageInfo.cacheName))
                                {
                                    DeleteOldIcon(ModSettings.ModName, imageInfo.cacheName);
                                    imageInfo.cacheName = string.Empty;
                                }

                                MemoryStream imageStream = LoadStreamFromFile(imageInfo.filePath);
                                if (imageStream == null) continue;

                                try
                                {
                                    if (imageStream.Length > 256 * 1024)
                                        ImageUtility.Resize(ref imageStream, 256, 256);
                                }
                                catch (Exception e)
                                {
                                    StickerMod.Logger.Warning($"Failed to resize image: {e.Message}");
                                }

                                imageInfo.cacheName = $"{imageName}_{Guid.NewGuid()}";

                                PrepareIconFromMemoryStream(ModSettings.ModName, imageInfo.cacheName, imageStream);
                            }

                            imageInfo.wasFoundThisPass = false;
                        }

                        foreach (var key in keysToRemove)
                            _cachedImages.Remove(key);
                    }
                }
                catch (Exception e)
                {
                    StickerMod.Logger.Error($"Failed to update sticker selection: {e.Message}");
                }
            });
        }
        
        private static void UpdateStickerSelectionAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    string path = StickerSystem.GetStickersFolderPath();
                    if (!Directory.Exists(path))
                    {
                        StickerMod.Logger.Warning("Stickers folder not found.");
                        return;
                    }

                    var stickerFiles = Directory.EnumerateFiles(path, "*.png");
                    var currentFiles = new HashSet<string>(stickerFiles);

                    lock (_cacheLock)
                    {
                        if (!_initialClearCacheFolder)
                        {
                            _initialClearCacheFolder = true;
                            DeleteOldIcons(ModSettings.ModName);
                        }

                        var keysToRemove = new List<string>();

                        foreach (var stickerFile in currentFiles)
                        {
                            string stickerName = Path.GetFileNameWithoutExtension(stickerFile);
                            
                            if (_cachedImages.TryGetValue(stickerName, out ImageInfo imageInfo))
                            {
                                imageInfo.wasFoundThisPass = true;
                                if (imageInfo.button == null)
                                {
                                    imageInfo.hasChanged = true;
                                    continue;
                                }
                                DateTime lastModified = File.GetLastWriteTime(stickerFile);
                                if (lastModified == imageInfo.lastModified) continue;
                                imageInfo.hasChanged = true;
                                imageInfo.lastModified = lastModified;
                            }
                            else
                            {
                                _cachedImages[stickerName] = new ImageInfo
                                {
                                    filePath = stickerFile,
                                    wasFoundThisPass = true,
                                    hasChanged = true,
                                    lastModified = File.GetLastWriteTime(stickerFile)
                                };
                            }
                        }

                        foreach (var kvp in _cachedImages)
                        {
                            var imageName = kvp.Key;
                            ImageInfo imageInfo = kvp.Value;

                            if (!imageInfo.wasFoundThisPass)
                            {
                                MainThreadInvoke(() =>
                                {
                                    if (imageInfo.button != null)
                                    {
                                        imageInfo.button.Delete();
                                        imageInfo.button = null;
                                    }
                                });

                                if (!string.IsNullOrEmpty(imageInfo.cacheName))
                                {
                                    DeleteOldIcon(ModSettings.ModName, imageInfo.cacheName);
                                    imageInfo.cacheName = string.Empty;
                                }

                                keysToRemove.Add(imageName);
                            }
                            else if (imageInfo.hasChanged)
                            {
                                imageInfo.hasChanged = false;

                                if (!string.IsNullOrEmpty(imageInfo.cacheName))
                                {
                                    DeleteOldIcon(ModSettings.ModName, imageInfo.cacheName);
                                    imageInfo.cacheName = string.Empty;
                                }

                                MemoryStream imageStream = LoadStreamFromFile(imageInfo.filePath);
                                if (imageStream == null) continue;

                                try
                                {
                                    if (imageStream.Length > 256 * 1024)
                                        ImageUtility.Resize(ref imageStream, 256, 256);
                                }
                                catch (Exception e)
                                {
                                    StickerMod.Logger.Warning($"Failed to resize image: {e.Message}");
                                }

                                imageInfo.cacheName = $"{imageName}_{Guid.NewGuid()}";

                                PrepareIconFromMemoryStream(ModSettings.ModName, imageInfo.cacheName, imageStream);
                                
                                MainThreadInvoke(() =>
                                {
                                    if (imageInfo.button != null)
                                        imageInfo.button.ButtonIcon = "UserImages/" + imageInfo.cacheName;
                                    else
                                    {
                                        imageInfo.button = _stickerSelectionCategory.AddButton(imageName, "UserImages/" + imageInfo.cacheName, $"Select {imageName}.", ButtonStyle.TextWithIcon);
                                        imageInfo.button.OnPress += () => OnStickerButtonClick(imageInfo);
                                    }
                                });
                            }

                            imageInfo.wasFoundThisPass = false;
                        }

                        foreach (var key in keysToRemove)
                            _cachedImages.Remove(key);

                        MainThreadInvoke(() =>
                        {
                            _stickerSelectionCategory.CategoryName = $"{ModSettings.SM_SelectionCategory} ({_cachedImages.Count})";
                        });
                    }
                }
                catch (Exception e)
                {
                    StickerMod.Logger.Error($"Failed to update sticker selection: {e.Message}");
                }
            });
        }

        private static MemoryStream LoadStreamFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                //StickerMod.Logger.Msg($"Loaded sticker stream from {filePath}");
                
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
        private static void MainThreadInvoke(Action action)
            => MTJobManager.RunOnMainThread("fuck", action.Invoke);

        #endregion Setup

        #region Button Actions

        private static void OnStickerButtonClick(ImageInfo imageInfo)
        {
            // i wish i could highlight it
            StickerSystem.Instance.LoadImage(imageInfo.button.ButtonText);
        }

        #endregion Button Actions

        #region Callbacks

        private static void OnSendingTextureOverNetworkChanged(bool isSending)
        {
            MTJobManager.RunAsyncOnMainThread("fuck2", () =>
            {
                // go through all buttons and disable them
                if (_isOurTabOpened && isSending) QuickMenuAPI.ShowAlertToast("Sending Sticker over Mod Network...", 2);
                foreach ((_, ImageInfo value) in _cachedImages) value.button.Disabled = isSending;
            });
        }

        private static void OnLoadImageFailed(string reason)
        {
            if (!_isOurTabOpened) return;
            QuickMenuAPI.ShowAlertToast(reason, 2);
        }

        #endregion Callbacks
    }
}