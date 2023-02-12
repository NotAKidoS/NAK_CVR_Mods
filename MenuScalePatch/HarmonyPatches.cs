using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using NAK.Melons.MenuScalePatch.Helpers;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NAK.Melons.MenuScalePatch.HarmonyPatches;

/**
    ViewManager.SetScale runs once a second when it should only run when aspect ratio changes- CVR bug
    assuming its caused by cast from int to float getting the screen size, something floating point bleh
    
    ViewManager.UpdatePosition & CVR_MenuManager.UpdatePosition are called every second in a scheduled job.
    (its why ViewManager.SetScale is called, because MM uses aspect ratio in scale calculation)

    I nuke those methods. Fuck them. I cannot disable the jobs though...
**/

[HarmonyPatch]
internal class HarmonyPatches
{
    //stuff needed on start
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Start")]
    private static void Postfix_PlayerSetup_Start()
    {
        try
        {
            MSP_MenuInfo.CameraTransform = PlayerSetup.Instance.GetActiveCamera().transform;
            MenuScalePatch.UpdateAllSettings();
            QuickMenuHelper.Instance.CreateWorldAnchors();
            MainMenuHelper.Instance.CreateWorldAnchors();
        }
        catch (System.Exception e)
        {
            MenuScalePatch.Logger.Error(e);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVR_MenuManager), "SetScale")]
    private static bool Prefix_CVR_MenuManager_SetScale(float avatarHeight, ref float ____scaleFactor)
    {
        ____scaleFactor = avatarHeight / 1.8f;
        if (MetaPort.Instance.isUsingVr) ____scaleFactor *= 0.5f;
        MSP_MenuInfo.ScaleFactor = ____scaleFactor;
        if (!MSP_MenuInfo.PlayerAnchorMenus)
        {
            QuickMenuHelper.Instance.NeedsPositionUpdate = true;
            MainMenuHelper.Instance.NeedsPositionUpdate = true;
        }
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewManager), "SetScale")]
    private static bool Prefix_ViewManager_SetScale()
    {
        return false;
    }

    //nuke UpdateMenuPosition methods
    //there are 2 Jobs calling this each second, which is fucking my shit
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVR_MenuManager), "UpdateMenuPosition")]
    private static bool Prefix_CVR_MenuManager_UpdateMenuPosition()
    {
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewManager), "UpdateMenuPosition")]
    private static bool Prefix_ViewManager_UpdateMenuPosition(ref float ___cachedScreenAspectRatio)
    {
        //this is called once a second, so ill fix their dumb aspect ratio shit
        float ratio = (float)Screen.width / (float)Screen.height;
        float clamp = Mathf.Clamp(ratio, 0f, 1.8f);
        MSP_MenuInfo.AspectRatio = 1.7777779f / clamp;
        return false;
    }

    //Set QM stuff
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "Start")]
    private static void Postfix_CVR_MenuManager_Start(ref CVR_MenuManager __instance, ref GameObject ____leftVrAnchor)
    {
        try
        {
            QuickMenuHelper helper = __instance.quickMenu.gameObject.AddComponent<QuickMenuHelper>();
            helper.handAnchor = ____leftVrAnchor.transform;
            helper.enabled = false;
        }
        catch (System.Exception e)
        {
            MenuScalePatch.Logger.Error(e);
        }
    }

    //Set MM stuff
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), "Start")]
    private static void Postfix_ViewManager_Start(ref ViewManager __instance)
    {
        try
        {
            MainMenuHelper helper = __instance.gameObject.AddComponent<MainMenuHelper>();
            helper.enabled = false;
        }
        catch (System.Exception e)
        {
            MenuScalePatch.Logger.Error(e);
        }
    }

    //hook quickmenu open/close
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVR_MenuManager), "ToggleQuickMenu", new Type[] { typeof(bool) })]
    private static bool Prefix_CVR_MenuManager_ToggleQuickMenu(bool show, ref CVR_MenuManager __instance, ref bool ____quickMenuOpen)
    {
        if (QuickMenuHelper.Instance == null) return true;
        if (show != ____quickMenuOpen)
        {
            ____quickMenuOpen = show;
            __instance.quickMenu.enabled = true;
            __instance.quickMenuAnimator.SetBool("Open", show);
            QuickMenuHelper.Instance.enabled = show;
            QuickMenuHelper.Instance.UpdateWorldAnchors(show);
            //shouldnt run if switching menus on desktop
            if (!MetaPort.Instance.isUsingVr)
            {
                if (!show && MainMenuHelper.Instance.enabled)
                {
                    return false;
                }
                ViewManager.Instance.UiStateToggle(false);
            }
            MSP_MenuInfo.ToggleDesktopInputMethod(show);
            CVRPlayerManager.Instance.ReloadAllNameplates();
        }
        return false;
    }

    //hook menu open/close
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewManager), "UiStateToggle", new Type[] { typeof(bool) })]
    private static bool Postfix_ViewManager_UiStateToggle(bool show, ref ViewManager __instance, ref bool ____gameMenuOpen)
    {
        if (MainMenuHelper.Instance == null) return true;
        if (show != ____gameMenuOpen)
        {
            ____gameMenuOpen = show;
            __instance.gameMenuView.enabled = true;
            __instance.uiMenuAnimator.SetBool("Open", show);
            MainMenuHelper.Instance.enabled = show;
            MainMenuHelper.Instance.UpdateWorldAnchors(show);
            //shouldnt run if switching menus on desktop
            if (!MetaPort.Instance.isUsingVr)
            {
                if (!show && QuickMenuHelper.Instance.enabled)
                {
                    return false;
                }
                CVR_MenuManager.Instance.ToggleQuickMenu(false);
            }
            MSP_MenuInfo.ToggleDesktopInputMethod(show);
            CVRPlayerManager.Instance.ReloadAllNameplates();
        }
        return false;
    }

    //add independent head movement to important input
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InputModuleMouseKeyboard), "UpdateImportantInput")]
    private static void Postfix_InputModuleMouseKeyboard_UpdateImportantInput(ref CVRInputManager ____inputManager)
    {
        ____inputManager.independentHeadTurn |= Input.GetKey(KeyCode.LeftAlt);
    }

    //Support for changing VRMode during runtime.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "CalibrateAvatar")]
    private static void Postfix_PlayerSetup_CalibrateAvatar()
    {
        try
        {
            MSP_MenuInfo.CameraTransform = PlayerSetup.Instance.GetActiveCamera().transform;
        }
        catch (System.Exception e)
        {
            MenuScalePatch.Logger.Error(e);
        }
    }
}