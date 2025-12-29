using System.Reflection;
using ABI_RC.Core.Base;
using ABI_RC.Core.Base.Jobs;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NAK.SuperAwesomeMod;

public class SuperAwesomeModMod : MelonMod
{
    #region Melon Events

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(PlayerSetup).GetMethod(nameof(PlayerSetup.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(SuperAwesomeModMod).GetMethod(nameof(OnPlayerSetupStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch(
            typeof(SceneLoaded).GetMethod(nameof(SceneLoaded.FilterWorldComponent),
                BindingFlags.NonPublic | BindingFlags.Static),
            postfix: new HarmonyMethod(typeof(SuperAwesomeModMod).GetMethod(nameof(OnShitLoaded),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        // // patch SharedFilter.ProcessCanvas
        // HarmonyInstance.Patch(
        //     typeof(SharedFilter).GetMethod(nameof(SharedFilter.ProcessCanvas),
        //         BindingFlags.Public | BindingFlags.Static),
        //     postfix: new HarmonyMethod(typeof(SuperAwesomeModMod).GetMethod(nameof(OnProcessCanvas),
        //         BindingFlags.NonPublic | BindingFlags.Static))
        // );
        
        LoggerInstance.Msg("SuperAwesomeModMod! OnInitializeMelon! :D");
    }

    public override void OnApplicationQuit()
    {
        LoggerInstance.Msg("SuperAwesomeModMod! OnApplicationQuit! D:");
    }
    
    #endregion Melon Events
    
    private static void OnPlayerSetupStart()
    {
        // CVRRaycastDebugManager.Initialize(PlayerSetup.Instance.desktopCam);
        // UEMenuHelper.Create();
    }
    
    private static void OnShitLoaded(Component c, List<Task> asyncTasks = null, Scene? scene = null)
    {
        if (!c)
            return;

        if (!c.gameObject)
            return;

        if (c.gameObject.scene.buildIndex > 0)
            return;

        if ((scene != null) && (c.gameObject.scene != scene))
            return;

        if (c is InputField input)
        {
            input.AddComponentIfMissing<InputFocusIntentDetector>();
        }

        if (c is TMPro.TMP_InputField tmpInput)
        {
            tmpInput.AddComponentIfMissing<InputFocusIntentDetector>();
        }
    }

    public class InputFocusIntentDetector : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (TryGetComponent(out TMPro.TMP_InputField input) && input.isActiveAndEnabled)
                ViewManager.Instance.openMenuKeyboard(input);
            else if (TryGetComponent(out InputField inputField) && inputField.isActiveAndEnabled)
                ViewManager.Instance.openMenuKeyboard(inputField);
        }
    }
}