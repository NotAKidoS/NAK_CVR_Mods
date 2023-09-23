using ABI_RC.Systems.Camera;
using BTKUILib;
using BTKUILib.UIObjects;
using MelonLoader;
using UnityEngine;

namespace NAK.AvatarScaleMod.Integrations.BTKUI;

public class PortableCameraCategory
{
    private static string categoryName = "Portable Camera";

    internal static void AddCategory(Page parent)
    {
        QuickMenuAPI.OnTabChange += OnTabChange;

        // Create category and add elements to it
        var category = parent.AddCategory(categoryName);
        category.AddButton("Take Photo", "TakePhoto-Icon", "Quickly take a photo. This respects set timers & other related settings.").OnPress += TakePhoto;
        category.AddButton("Cycle Delay", "CycleDelay-Icon", "Quickly cycle photo timers. Off, 3s, 5s, 10s.").OnPress += CycleCaptureDelay;
        category.AddButton("Open Folder", "OpenFolder-Icon", "Quickly open the root of the ChilloutVR screenshots folder in Windows Explorer.").OnPress += OpenScreenshotsFolder;

        // Clone of the default camera settings page
        var settingsPage = category.AddPage("Settings", "Settings-Icon", "Sub page of settings to configure the portable camera.", parent.MenuTitle);
        settingsPage.AddCategory("Main Settings");
        settingsPage.AddSlider("Field of View", "Field of View of portable camera.", 40f, 10f, 120f);
        settingsPage.AddSlider("Focal Length", "Focal Length of portable camera.", 50f, 24f, 200f);
        settingsPage.AddSlider("Aperture", "Aperture of portable camera.", 1.8f, 1.2f, 8f);
    }

    private static bool PortableCameraReady()
    {
        bool active = (bool)(PortableCamera.Instance?.IsActive());
        if (!active) CVRCamController.Instance?.Toggle();
        return active;
    }

    private static void TakePhoto()
    {
        MelonLogger.Msg("Took photo!");
        if (PortableCameraReady())
            PortableCamera.Instance?.MakePhoto();
    }

    private static void CycleCaptureDelay()
    {
        if (PortableCameraReady())
        {
            PortableCamera.Instance?.ChangeCameraCaptureDelay();
            QuickMenuAPI.ShowAlertToast("Delay set to " + PortableCamera.Instance.timerText.text, 1);
        }
    }

    //this was mistake, but now feature cause fuck it
    private static void PauseCamera()
    {
        MelonLogger.Msg("Paused camera!");
        GameObject camera = PortableCamera.Instance.gameObject;
        PortableCamera.Instance.gameObject.SetActive(!camera.activeSelf);
    }

    private static void OpenScreenshotsFolder()
    {
        MelonLogger.Msg("Opened screenshots folder!");
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ChilloutVR");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        Application.OpenURL("file:///" + path);
    }

    private static DateTime lastTime = DateTime.Now;
    private static void OnTabChange(string newTab, string previousTab)
    {
        if (newTab == "btkUI-AvatarScaleMod-MainPage")
        {
            TimeSpan timeDifference = DateTime.Now - lastTime;
            if (timeDifference.TotalSeconds <= 0.5)
            {
                // The new page and previous page are equal and were opened within 0.5 seconds of each other
                CVRCamController.Instance?.Toggle();
            }
        }
        lastTime = DateTime.Now;
    }
}