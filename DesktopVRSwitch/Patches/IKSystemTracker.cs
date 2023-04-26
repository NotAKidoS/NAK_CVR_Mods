using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.TrackingModules;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NAK.DesktopVRSwitch.Patches;

public class IKSystemTracker : MonoBehaviour
{
    public IKSystem ikSystem;
    public Traverse _traverseModules;

    void Start()
    {
        ikSystem = GetComponent<IKSystem>();
        _traverseModules = Traverse.Create(ikSystem).Field("_trackingModules");
        VRModeSwitchTracker.OnPostVRModeSwitch += PostVRModeSwitch;
    }
    void OnDestroy()
    {
        VRModeSwitchTracker.OnPostVRModeSwitch -= PostVRModeSwitch;
    }

    public void PostVRModeSwitch(bool isVR, Camera activeCamera)
    {
        var _trackingModules = _traverseModules.GetValue<List<TrackingModule>>();
        SteamVRTrackingModule openVRTrackingModule = _trackingModules.FirstOrDefault(m => m is SteamVRTrackingModule) as SteamVRTrackingModule;
        if (openVRTrackingModule != null)
        {
            if (isVR)
            {
                openVRTrackingModule.ModuleStart();
            }
            else
            {
                //why named destroy when it doesnt ?
                openVRTrackingModule.ModuleDestroy();
            }
        }
        else
        {
            var steamVRTrackingModule = CreateSteamVRTrackingModule();
            ikSystem.AddTrackingModule(steamVRTrackingModule);
        }

        //make it so you dont instantly end up in FBT from Desktop
        IKSystem.firstAvatarLoaded = DesktopVRSwitch.EntryEnterCalibrationOnSwitch.Value;
        //turn of finger tracking just in case user switched controllers
        ikSystem.FingerSystem.controlActive = false;
    }

    //thanks for marking the constructor as internal
    private SteamVRTrackingModule CreateSteamVRTrackingModule()
    {
        var steamVRTrackingModuleType = typeof(SteamVRTrackingModule);
        var constructor = steamVRTrackingModuleType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        var instance = constructor.Invoke(null);
        return (SteamVRTrackingModule)instance;
    }
}