using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.Movement;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.ESCBothMenus;

public class ESCBothMenusMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(ViewManager).GetMethod(nameof(ViewManager.Update), 
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(ESCBothMenusMod).GetMethod(nameof(OnPreViewManagerUpdate),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static float _timer = -1f;
    private static bool _main;

    private static void OnPreViewManagerUpdate()
    {
        if (CVRInputManager.Instance.mainMenuButton
            && !BetterBetterCharacterController.Instance.IsSittingOnControlSeat())
        {
            if (_timer < 0f) _timer = 0.2f;
            else _main = true;
        }

        if (_timer >= 0f && (_timer -= Time.deltaTime) <= 0f)
        {
            var vm = ViewManager.Instance;
            var qm = CVR_MenuManager.Instance;

            if (vm.IsViewShown) vm.UiStateToggle(false);
            else if (qm.IsViewShown) qm.ToggleQuickMenu(false);
            else if (_main) vm.UiStateToggle(true);
            else qm.ToggleQuickMenu(true);

            _timer = -1f;
            _main = false;
        }

        // consume so original logic doesn't fire
        CVRInputManager.Instance.mainMenuButton = false;
    }
}