using ABI_RC.Systems.Camera;
using HarmonyLib;
using UnityEngine;

namespace NAK.PortableCameraAdditions.HarmonyPatches;

[HarmonyPatch]
internal class PortableCameraPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), nameof(PortableCamera.Start))]
    private static void Postfix_PortableCamera_Start(ref PortableCamera __instance)
    {
        //run mod.Setup() instead of registering full mod with icon
        VisualMods.CameraAdditions mainMod = new VisualMods.CameraAdditions();
        mainMod.Setup(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), nameof(PortableCamera.OnWorldLoaded))]
    private static void Postfix_PortableCamera_OnWorldLoaded(Camera worldCamera)
    {
        VisualMods.CameraAdditions.Instance?.OnWorldLoaded(worldCamera);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), nameof(PortableCamera.UpdateOptionsDisplay))]
    private static void Postfix_PortableCamera_UpdateOptionsDisplay(ref bool ____showExpertSettings)
    {
        VisualMods.CameraAdditions.Instance?.OnUpdateOptionsDisplay(____showExpertSettings);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), nameof(PortableCamera.Update))]
    private static void Postfix_PortableCamera_Update(ref PortableCamera __instance)
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            bool flag = __instance.mode == MirroringMode.NoMirror;
            __instance.mode = (flag) ? MirroringMode.Mirror : MirroringMode.NoMirror;
            __instance.mirroringActive.SetActive(flag);
            __instance.mirroringCanvas.gameObject.SetActive(flag);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PortableCamera), nameof(PortableCamera.OnDisable))]
    private static void Postfix_PortableCamera_OnDisable(ref PortableCamera __instance)
    {
        __instance.mode = MirroringMode.NoMirror;
        __instance.mirroringActive.SetActive(false);
        __instance.mirroringCanvas.gameObject.SetActive(false);
    }
}