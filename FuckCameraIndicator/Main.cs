using MelonLoader;
using System.Reflection;
using ABI_RC.Core.Player;
using UnityEngine;

namespace NAK.FuckCameraIndicator;

public class FuckCameraIndicator : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(PuppetMaster).GetMethod(nameof(PuppetMaster.Start), BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyLib.HarmonyMethod(typeof(FuckCameraIndicator).GetMethod(nameof(OnPuppetMasterStart_Postfix), BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    private static void OnPuppetMasterStart_Postfix(PuppetMaster __instance)
    {
        // thanks for not making it modular, fucking spaghetti
        // and why leave it a skinned mesh... lazy fucking implementation
        
        GameObject indicator = __instance.cameraIndicator;
        GameObject lens = __instance.cameraIndicatorLense;
        
        // Disable NamePlate child object
        const string c_CanvasPath = "[NamePlate]/Canvas";
        GameObject canvas = indicator.transform.Find(c_CanvasPath).gameObject;
        canvas.SetActive(false);

        // Disable lens renderer
        lens.GetComponent<SkinnedMeshRenderer>().forceRenderingOff = true;
    }
}