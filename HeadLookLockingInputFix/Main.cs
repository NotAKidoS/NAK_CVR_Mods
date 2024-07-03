using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using MelonLoader;

namespace NAK.HeadLookLockingInputFix;

public class HeadLookLockingInputFixMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(InputManager).GetMethod(nameof(InputManager.HandleMenuHeadLook),
                BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(HeadLookLockingInputFixMod).GetMethod(nameof(OnPreHandleMenuHeadLook),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static bool OnPreHandleMenuHeadLook() 
        => !MetaPort.Instance.isUsingVr; // only execute in Desktop
}