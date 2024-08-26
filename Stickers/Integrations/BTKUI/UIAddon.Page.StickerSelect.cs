using System.Diagnostics;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MTJobSystem;
using NAK.Stickers.Utilities;
using UnityEngine;

namespace NAK.Stickers.Integrations;

public static partial class BTKUIAddon
{
    #region Constants and Fields

    private static readonly HashSet<string> SUPPORTED_IMAGE_EXTENSIONS = new() { ".png", ".jpg", ".jpeg" };

    private static Page _ourDirectoryBrowserPage;
    
    private static Category _fileCategory;
    private static Category _folderCategory;
    
    private const int MAX_BUTTONS = 512; // cohtml literally will start to explode
    private static Button[] _fileButtons = new Button[80]; // 100 files, will resize if needed
    private static Button[] _folderButtons = new Button[20]; // 20 folders, will resize if needed
    
    private static readonly Button[] _stickerSelectionButtons = new Button[4];
    private static float _stickerSelectionButtonDoubleClickTime;

    private static DirectoryInfo _curDirectoryInfo;
    private static string _initialDirectory;
    private static int _curSelectedSticker;
    
    private static readonly object _isPopulatingLock = new();
    private static bool _isPopulating;
    internal static bool IsPopulatingPage {
        get { lock (_isPopulatingLock) return _isPopulating; }
        private set { lock (_isPopulatingLock) _isPopulating = value; }
    }

    #endregion Constants and Fields

    #region Page Setup
    
    private static void Setup_StickerSelectionCategory()
    {
        _initialDirectory = StickerSystem.GetStickersFolderPath();
        _curDirectoryInfo = new DirectoryInfo(_initialDirectory);

        // Create page
        _ourDirectoryBrowserPage = Page.GetOrCreatePage(ModSettings.ModName, "Directory Browser");
        QuickMenuAPI.AddRootPage(_ourDirectoryBrowserPage);

        // Setup categories
        _folderCategory = _ourDirectoryBrowserPage.AddCategory("Subdirectories");
        _fileCategory = _ourDirectoryBrowserPage.AddCategory("Images");

        SetupFolderButtons();
        SetupFileButtons();
        SetupStickerSelectionButtons();

        _ourDirectoryBrowserPage.OnPageOpen += OnPageOpen;
        _ourDirectoryBrowserPage.OnPageClosed += OnPageClosed;
    }
    
        private static void SetupFolderButtons(int startIndex = 0)
    {
        for (int i = startIndex; i < _folderButtons.Length; i++)
        {
            Button button = _folderCategory.AddButton("A", "Stickers-folder", "A");
            button.OnPress += () =>
            {
                if (IsPopulatingPage) return;
                _curDirectoryInfo = new DirectoryInfo(Path.Combine(_curDirectoryInfo.FullName, button.ButtonTooltip[5..]));
                _ourDirectoryBrowserPage.OpenPage(false, true);
            };
            _folderButtons[i] = button;
        }
    }

    private static void SetupFileButtons(int startIndex = 0)
    {
        for (int i = startIndex; i < _fileButtons.Length; i++)
        {
            Button button = _fileCategory.AddButton(string.Empty, "Stickers-folder", "A", ButtonStyle.FullSizeImage);
            button.Hidden = true;
            button.OnPress += () =>
            {
                string absolutePath = Path.Combine(_curDirectoryInfo.FullName, button.ButtonTooltip[5..]);
                string relativePath = Path.GetRelativePath(_initialDirectory, absolutePath);
                StickerSystem.Instance.LoadImage(relativePath, _curSelectedSticker);
            };
            _fileButtons[i] = button;
        }
    }

    private static void SetupStickerSelectionButtons()
    {
        Category stickerSelection = _rootPage.AddMelonCategory(ModSettings.Hidden_Foldout_SelectionCategory);

        for (int i = 0; i < _stickerSelectionButtons.Length; i++)
        {
            Button button = stickerSelection.AddButton(string.Empty, "Stickers-puzzle", "Click to select sticker for placement. Double-click or hold to select from Stickers folder.", ButtonStyle.FullSizeImage);
            var curIndex = i;
            button.OnPress += () => SelectStickerAtSlot(curIndex);
            button.OnHeld += () => OpenStickerSelectionForSlot(curIndex);
            _stickerSelectionButtons[i] = button;
            
            // initial setup
            button.ButtonIcon = StickerCache.GetBtkUiIconName(ModSettings.Hidden_SelectedStickerNames.Value[i]);
        }
    }

    #endregion Page Setup

    #region Private Methods
    
    private static void OnPageOpen()
    {
        if (IsPopulatingPage) return; // btkui bug, page open is called twice when using OnHeld
        IsPopulatingPage = true;
        
        _ourDirectoryBrowserPage.PageDisplayName = _curDirectoryInfo.Name;

        HideAllButtons(_folderButtons);
        HideAllButtons(_fileButtons);
        
        // Populate the page
        Task.Run(PopulateMenuItems);
    }

    private static void OnPageClosed()
    {
        if (_curDirectoryInfo.FullName != _initialDirectory)
            _curDirectoryInfo = new DirectoryInfo(Path.Combine(_curDirectoryInfo.FullName, @"..\"));
    }

    private static void HideAllButtons(Button[] buttons)
    {
        foreach (Button button in buttons)
        {
            if (button == null) break; // Array resized, excess buttons are generating
            //if (button.Hidden) break; // Reached the end of the visible buttons
            button.Hidden = true;
            button.ButtonIcon = string.Empty; // hoping this makes cohtml less mad
        }
    }
    
    private static void SelectStickerAtSlot(int index)
    {
        if (_curSelectedSticker != index)
        {
            _curSelectedSticker = index;
            _stickerSelectionButtonDoubleClickTime = 0f;
        }
        
        StickerSystem.Instance.SelectedStickerSlot = index;
        
        // double-click to open (otherwise just hold)
        if (Time.time - _stickerSelectionButtonDoubleClickTime < 0.5f)
        {
            OpenStickerSelectionForSlot(index);
            _stickerSelectionButtonDoubleClickTime = 0f;
            return;
        }
        _stickerSelectionButtonDoubleClickTime = Time.time;
    }

    private static void OpenStickerSelectionForSlot(int index)
    {
        if (IsPopulatingPage) return;
        _curSelectedSticker = index;
        _curDirectoryInfo = new DirectoryInfo(_initialDirectory);
        _ourDirectoryBrowserPage.OpenPage(false, true);
    }

    private static void PopulateMenuItems()
    {
        StickerMod.Logger.Msg("Populating menu items.");
        try
        {
            Thread.CurrentThread.IsBackground = false; // working around bug in MTJobManager
            
            var directories = _curDirectoryInfo.GetDirectories();
            var files = _curDirectoryInfo.GetFiles();
            
            MTJobManager.RunOnMainThread("PopulateMenuItems", () =>
            {
                // resize the arrays to the max amount of buttons
                int foldersCount = Mathf.Min(directories.Length, MAX_BUTTONS);
                if (foldersCount > _folderButtons.Length)
                {
                    int folderEndIdx = _folderButtons.Length;
                    Array.Resize(ref _folderButtons, foldersCount);
                    SetupFolderButtons(folderEndIdx);
                }

                int filesCount = Mathf.Min(files.Length, MAX_BUTTONS);
                if (filesCount > _fileButtons.Length)
                {
                    int fileEndIdx = _fileButtons.Length;
                    Array.Resize(ref _fileButtons, filesCount);
                    SetupFileButtons(fileEndIdx);
                }
                
                _folderCategory.Hidden = foldersCount == 0;
                _folderCategory.CategoryName = $"Subdirectories ({foldersCount})";
                _fileCategory.Hidden = filesCount == 0;
                _fileCategory.CategoryName = $"Images ({filesCount})";
            });

            PopulateFolders(directories);
            PopulateFiles(files);
        }
        catch (Exception e)
        {
            StickerMod.Logger.Error($"Failed to populate menu items: {e.Message}");
        }
        finally
        {
            IsPopulatingPage = false;
            Thread.CurrentThread.IsBackground = true; // working around bug in MTJobManager
        }
    }

    private static void PopulateFolders(IReadOnlyList<DirectoryInfo> directories)
    {
        for (int i = 0; i < _folderButtons.Length; i++)
        {
            if (i >= directories.Count)
                break;
            
            Button button = _folderButtons[i];
            //if (button == null) continue; // Array resized, excess buttons are generating
            
            button.ButtonText = directories[i].Name;
            button.ButtonTooltip = $"Open {directories[i].Name}";
            MTJobManager.RunAsyncOnMainThread("PopulateMenuItems", () => button.Hidden = false);

            if (i <= 16) Thread.Sleep(10); // For the pop-in effect
        }
    }
    
    private static void PopulateFiles(IReadOnlyList<FileInfo> files)
    {
        for (int i = 0; i < _fileButtons.Length; i++)
        {
            if (i >= files.Count)
                break;
            
            FileInfo fileInfo = files[i];

            if (!SUPPORTED_IMAGE_EXTENSIONS.Contains(fileInfo.Extension.ToLower()))
                continue;

            string relativePath = Path.GetRelativePath(_initialDirectory, fileInfo.FullName);
            string relativePathWithoutExtension = relativePath[..^fileInfo.Extension.Length];

            Button button = _fileButtons[i];
            //if (button == null) continue; // Array resized, excess buttons are generating
            
            button.ButtonTooltip = $"Load {fileInfo.Name}"; // Do not change "Load " prefix, we extract file name

            if (StickerCache.IsThumbnailAvailable(relativePathWithoutExtension))
            {
                button.ButtonIcon = StickerCache.GetBtkUiIconName(relativePath);
            }
            else
            {
                button.ButtonIcon = string.Empty;
                StickerCache.EnqueueThumbnailGeneration(fileInfo, button);
            }

            MTJobManager.RunAsyncOnMainThread("PopulateMenuItems", () => button.Hidden = false);

            if (i <= 16) Thread.Sleep(10); // For the pop-in effect
        }
    }

    #endregion Icon Utils
}