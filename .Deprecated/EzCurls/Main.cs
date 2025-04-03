using MelonLoader;
using System.Reflection;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.InputManagement.InputModules;

namespace NAK.EzCurls;

public class EzCurls : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(CVRInputModule_XR).GetMethod(nameof(CVRInputModule_XR.ModuleAdded)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(EzCurls).GetMethod(nameof(OnCVRInputModule_XRModuleAdded_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );

        ModSettings.Initialize();
    }

    private static void OnCVRInputModule_XRModuleAdded_Postfix()
        => CVRInputManager.Instance.AddInputModule(new InputModuleCurlAdjuster());
}