using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.TrackingModules;
using ABI_RC.Systems.InputManagement;
using ABI_RC.Systems.InputManagement.XR.Modules;

namespace NAK.DesktopVRSwitch.Patches;

internal static class SteamVRNullReferencePatch
{
    public static void DestroySteamVRInstancesImmediate()
    {
        // set to null so input manager doesnt attempt to access it
        if (CVRInputManager._moduleXR != null)
        {
            if (CVRInputManager._moduleXR._leftModule != null)
                if (CVRInputManager._moduleXR._leftModule is CVRXRModule_SteamVR leftModule) leftModule._steamVr = null;
            if (CVRInputManager._moduleXR._rightModule != null)
                if (CVRInputManager._moduleXR._rightModule is CVRXRModule_SteamVR rightModule) rightModule._steamVr = null;
        }
    
        if (IKSystem.Instance == null) 
            return;
        
        // set to null so tracking module doesnt attempt to access it
        foreach (TrackingModule module in IKSystem.Instance._trackingModules)
            if (module is SteamVRTrackingModule steamVRModule)
                steamVRModule._steamVr = null;
    }
}