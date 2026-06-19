using ABI_RC.Systems.InputManagement.InputModules;
using System.Reflection;
using ABI_RC.Core.PropManagement;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.PropUndoButton;

public class PropUndoButton : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // desktop input patch so we don't run in menus/gui
            typeof(CVRInputModule_Keyboard).GetMethod(nameof(CVRInputModule_Keyboard.Update_Binds)),
            postfix: new HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnUpdateInput),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static void OnUpdateInput()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
            {
                PropUserActions.Redo();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                PropUserActions.Undo();
            }
        }
    }
}