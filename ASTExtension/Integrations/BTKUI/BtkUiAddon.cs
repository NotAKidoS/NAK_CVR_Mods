using ABI_RC.Core.Player;
using ABI_RC.Core.Util.AnimatorManager;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using UnityEngine;

namespace NAK.ASTExtension.Integrations;

public static partial class BtkUiAddon
{
    public static void Initialize()
    {
            Prepare_Icons();
            Setup_PlayerSelectPage();
        }
        
    private static void Prepare_Icons()
    {
            QuickMenuAPI.PrepareIcon(ASTExtensionMod.ModName, "ASM_Icon_AvatarHeightCopy",
                GetIconStream("ASM_Icon_AvatarHeightCopy.png"));
        }

    #region Player Select Page

    private static string _selectedPlayer;
        
    private static void Setup_PlayerSelectPage()
    {
            QuickMenuAPI.OnPlayerSelected += OnPlayerSelected;
            Category category = QuickMenuAPI.PlayerSelectPage.AddCategory(ASTExtensionMod.ModName, ASTExtensionMod.ModName);
            Button button = category.AddButton("Copy Height", "ASM_Icon_AvatarHeightCopy", "Copy selected players Eye Height.");
            button.OnPress += OnCopyPlayerHeight;
            
            Button button2 = category.AddButton("Copy AAS", string.Empty, "Copy selected players AAS.");
            button2.OnPress += OnCopyPlayerAAS;
        }
        
    private static void OnPlayerSelected(string _, string id)
    {
            _selectedPlayer = id;
        }
        
    private static void OnCopyPlayerHeight()
    {
        if (string.IsNullOrEmpty(_selectedPlayer))
            return;
        
        if (!CVRPlayerManager.Instance.GetPlayerPuppetMaster(_selectedPlayer, out PuppetMaster player))
            return;
        
        if (player._avatar == null)
            return;

        float height = player.netIkController.GetRemoteHeight();
        ASTExtensionMod.Instance.SetAvatarHeight(height);
    }
    
    private static void OnCopyPlayerAAS()
    {
        if (string.IsNullOrEmpty(_selectedPlayer))
            return;
        
        if (!CVRPlayerManager.Instance.GetPlayerPuppetMaster(_selectedPlayer, out PuppetMaster player))
            return;
        
        AvatarAnimatorManager localAnimator = PlayerSetup.Instance.animatorManager;
        AvatarAnimatorManager remoteAnimator = player.animatorManager;
        if (!localAnimator.IsInitialized 
            || !remoteAnimator.IsInitialized)
            return;
        
        // Copy AAS
        foreach ((var parameterName, CVRAnimatorManager.ParamDef paramDef) in remoteAnimator.Parameters)
        {
            switch (paramDef.type)
            {
                case AnimatorControllerParameterType.Trigger:
                case AnimatorControllerParameterType.Bool:
                    remoteAnimator.GetParameter(parameterName, out bool value);
                    localAnimator.SetParameter(parameterName, value);
                    break;
                case AnimatorControllerParameterType.Float:
                    remoteAnimator.GetParameter(parameterName, out float value2);
                    localAnimator.SetParameter(parameterName, value2);
                    break;
                case AnimatorControllerParameterType.Int:
                    remoteAnimator.GetParameter(parameterName, out int value3);
                    localAnimator.SetParameter(parameterName, value3);
                    break;
            }
        }
        
        
    }
        
    #endregion Player Select Page
}