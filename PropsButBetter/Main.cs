using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Util;
using ABI_RC.Systems.InputManagement.InputModules;
using ABI_RC.Systems.UI.UILib;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.PropsButBetter;

public class PropsButBetterMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        
        // Replace prop select method
        HarmonyInstance.Patch(
            typeof(ControllerRay).GetMethod(nameof(ControllerRay.HandleSpawnableClicked),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(PropsButBetterMod).GetMethod(nameof(OnPreHandleSpawnableClicked),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        // Replace player select method
        HarmonyInstance.Patch(
            typeof(ControllerRay).GetMethod(nameof(ControllerRay.HandlePlayerClicked),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(PropsButBetterMod).GetMethod(nameof(OnPreHandlePlayerClicked),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // Desktop keybindings for undo/redo
        HarmonyInstance.Patch(
            typeof(CVRInputModule_Keyboard).GetMethod(nameof(CVRInputModule_Keyboard.Update_Binds),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(PropsButBetterMod).GetMethod(nameof(OnUpdateKeyboardBinds),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        HarmonyInstance.Patch( // delete my props in reverse order for redo
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.DeleteMyProps),
                BindingFlags.Public | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(PropHelper).GetMethod(nameof(PropHelper.OnPreDeleteMyProps),
                BindingFlags.Public | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // delete all props in reverse order for redo
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.DeleteAllProps),
                BindingFlags.Public | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(PropHelper).GetMethod(nameof(PropHelper.OnPreDeleteAllProps),
                BindingFlags.Public | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // prop spawn sfx
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.SpawnProp),
                BindingFlags.Public | BindingFlags.Static),
            postfix: new HarmonyMethod(typeof(PropHelper).GetMethod(nameof(PropHelper.OnTrySpawnProp),
                BindingFlags.Public | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.SelectPropToSpawn),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(PropHelper).GetMethod(nameof(PropHelper.OnSelectPropToSpawn),
                BindingFlags.Public | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.EnterPropDeleteMode),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(PropHelper).GetMethod(nameof(PropHelper.OnClearPropToSpawn),
                BindingFlags.Public | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.ClearPropToSpawn),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(PropHelper).GetMethod(nameof(PropHelper.OnClearPropToSpawn),
                BindingFlags.Public | BindingFlags.Static))
        );

        HarmonyInstance.Patch(
            typeof(ControllerRay).GetMethod(nameof(ControllerRay.HandlePropSpawn),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(PropHelper).GetMethod(nameof(PropHelper.OnHandlePropSpawn),
                BindingFlags.Public | BindingFlags.Static))
        );

        UILibHelper.LoadIcons();
        QuickMenuPropList.BuildUI();
        QuickMenuPropSelect.BuildUI();
        PropHelper.Initialize();
        
        // Bono approved hack
        QuickMenuAPI.InjectCSSStyle("""
                                    .shit {
                                        width: 100%;
                                        height: 100%;
                                        object-fit: cover; 
                                        border-radius: 10px; 
                                        display: block; 
                                        position: absolute;
                                        top: 3px; 
                                        left: 3px;
                                    }

                                    .shit2 {
                                        opacity: 0; 
                                        transition: opacity 0.3s ease-in-out;
                                    }

                                    """);
        
        // Build ui once quick menu is loaded
        QuickMenuAPI.OnMenuGenerated += _ =>
        {
            QuickMenuPropSelect.ListenForQM();
            PropListEntry.ListenForQM();
            GlobalButton.ListenForQM();
            UndoRedoButtons.ListenForButtons();
        };
        
        SetupDefaultAudioClips();
    }

    /*public override void OnUpdate()
    {
        PropDistanceHider.Tick();
    }*/

    // ReSharper disable once RedundantAssignment
    private static bool OnPreHandleSpawnableClicked(ref ControllerRay __instance, ref CVRSpawnable __result)
    {
        __result = null;
        
        CVRSpawnable spawnable = __instance.hitTransform.GetComponentInParent<CVRSpawnable>();
        if (spawnable == null) return false;
        
        PlayerSetup.PropSelectionMode selectionMode = PlayerSetup.Instance.GetCurrentPropSelectionMode();
        switch (selectionMode)
        {
            case PlayerSetup.PropSelectionMode.None:
            {
                // Click a prop while a menu is open to open the details menu
                if (__instance._interactDown 
                    && __instance.CanSelectPlayersAndProps()
                    && spawnable.TryGetComponent(out CVRAssetInfo assetInfo))
                {
                    // Direct to Main Menu if it is open
                    if (ViewManager.Instance.IsViewShown)
                    {
                        ViewManager.Instance.GetPropDetails(assetInfo.objectId);
                        ViewManager.Instance.UiStateToggle(true);
                    }
                    // Direct to Quick Menu by default
                    else
                    {
                        QuickMenuPropSelect.ShowInfo(spawnable.PropData);
                    }

                    __instance._interactDown = false; // Consume the click
                }
                break;
            }
            case PlayerSetup.PropSelectionMode.Delete:
            {
                // Click a prop while in delete mode to delete it
                if (__instance._interactDown 
                    && !spawnable.IsSpawnedByAdmin())
                {
                    spawnable.Delete();
                    __instance._interactDown = false; // Consume the click
                    return false; // Don't return the spawnable, it's been deleted
                }
                break;
            }
        }

        // Return normal prop hover
        __result = spawnable;
        return false;
    }
    
    private static bool OnPreHandlePlayerClicked(ref ControllerRay __instance, ref PlayerBase __result)
    {
        if (!ModSettings.EntryFixPlayerSelectRedirect.Value)
            return true;
        
        __result = null;
        
        if (PlayerSetup.Instance.GetCurrentPropSelectionMode() 
            != PlayerSetup.PropSelectionMode.None)
            return false;

        PlayerBase playerBase = null;
        if (__instance.CanSelectPlayersAndProps())
        {
            bool foundPlayerDescriptor = __instance.hitTransform.TryGetComponent(out playerBase);
            if (!foundPlayerDescriptor && __instance.hitTransform.TryGetComponent(out CameraIndicator cameraIndicator))
            {
                PlayerBase cameraOwner = cameraIndicator.ownerPlayer;
                if (!playerBase.IsLocalPlayer)
                {
                    playerBase = cameraOwner;
                    foundPlayerDescriptor = true;
                }
            }

            if (foundPlayerDescriptor && __instance._interactDown)
            {
                // Direct to Main Menu if it is open
                if (ViewManager.Instance.IsViewShown)
                {
                    ViewManager.Instance.RequestUserDetailsPage(playerBase.PlayerId);
                    ViewManager.Instance.UiStateToggle(true);
                }
                // Direct to Quick Menu by default
                else
                {
                    QuickMenuAPI.OpenPlayerListByUserID(playerBase.PlayerId);
                    CVR_MenuManager.Instance.ToggleQuickMenu(true);
                }
            }
        }

        __result = playerBase;
        return false;
    }

    private static void OnUpdateKeyboardBinds()
    {
        if (!ModSettings.EntryUseUndoRedoKeybinds.Value)
            return;
        
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKey(KeyCode.LeftShift) 
                && Input.GetKeyDown(KeyCode.Z))
                PropHelper.RedoProp();
            else if (Input.GetKeyDown(KeyCode.Z)) 
                PropHelper.UndoProp();
        }
    }

    private void SetupDefaultAudioClips()
    {
        // PropUndo and audio folders do not exist, create them if dont exist yet
        var path = Application.streamingAssetsPath + $"/Cohtml/UIResources/GameUI/mods/{nameof(PropsButBetter)}/audio/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            LoggerInstance.Msg("Created audio directory!");
        }

        // copy embedded resources to this folder if they do not exist
        string[] clipNames = { "sfx_spawn.wav", "sfx_undo.wav", "sfx_redo.wav", "sfx_warn.wav", "sfx_deny.wav" };
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        
        foreach (var clipName in clipNames)
        {
            var clipPath = Path.Combine(path, clipName);
            if (!File.Exists(clipPath))
            {
                // read the clip data from embedded resources
                byte[] clipData;
                var resourceName = $"{nameof(PropsButBetter)}.Resources.SFX." + clipName;
                using (Stream stream = executingAssembly.GetManifestResourceStream(resourceName))
                {
                    clipData = new byte[stream!.Length];
                    // ReSharper disable once MustUseReturnValue
                    stream.Read(clipData, 0, clipData.Length);
                }

                // write the clip data to the file
                using (FileStream fileStream = new(clipPath, FileMode.CreateNew))
                {
                    fileStream.Write(clipData, 0, clipData.Length);
                }

                LoggerInstance.Msg("Placed missing sfx in audio folder: " + clipName);
            }
        }
    }
}