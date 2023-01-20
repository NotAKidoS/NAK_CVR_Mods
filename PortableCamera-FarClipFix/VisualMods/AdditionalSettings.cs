using ABI_RC.Systems.Camera;
using UnityEngine;

namespace NAK.Melons.PortableCameraAdditions.VisualMods;

public class AdditionalSettings
{
    public static AdditionalSettings Instance;

    public Camera referenceCamera;
    public bool orthographicMode;

    //Should I move these to MelonPrefs?
    public bool CopyWorldNearClip;
    public bool CopyWorldFarClip;

    private PortableCameraSetting setting_NearClip;
    private PortableCameraSetting setting_FarClip;

    private PortableCameraSetting setting_OrthographicSize;
    private PortableCameraSetting setting_OrthographicNearClip;
    private PortableCameraSetting setting_OrthographicFarClip;

    public void Setup(PortableCamera __instance)
    {
        Instance = this;

        __instance.@interface.AddAndGetHeader(null, typeof(AdditionalSettings), "Additional Settings");

        //Basic Settings

        PortableCameraSetting setting_CopyWorldNearClip = __instance.@interface.AddAndGetSetting(PortableCameraSettingType.Bool);
        setting_CopyWorldNearClip.BoolChanged = new Action<bool>(value => UpdateCameraSettingBool("CopyNearClip", value));
        setting_CopyWorldNearClip.SettingName = "CopyNearClip";
        setting_CopyWorldNearClip.DisplayName = "Copy World Near Clip";
        setting_CopyWorldNearClip.OriginType = typeof(AdditionalSettings);
        setting_CopyWorldNearClip.DefaultValue = true;
        setting_CopyWorldNearClip.Load();

        PortableCameraSetting setting_CopyWorldFarClip = __instance.@interface.AddAndGetSetting(PortableCameraSettingType.Bool);
        setting_CopyWorldFarClip.BoolChanged = new Action<bool>(value => UpdateCameraSettingBool("CopyFarClip", value));
        setting_CopyWorldFarClip.SettingName = "CopyFarClip";
        setting_CopyWorldFarClip.DisplayName = "Copy World Far Clip";
        setting_CopyWorldFarClip.OriginType = typeof(AdditionalSettings);
        setting_CopyWorldFarClip.DefaultValue = true;
        setting_CopyWorldFarClip.Load();

        //Expert Settings

        PortableCameraSetting setting_Orthographic = __instance.@interface.AddAndGetSetting(PortableCameraSettingType.Bool);
        setting_Orthographic.BoolChanged = new Action<bool>(value => UpdateCameraSettingBool("Orthographic", value));
        setting_Orthographic.SettingName = "Orthographic";
        setting_Orthographic.DisplayName = "Orthographic";
        setting_Orthographic.isExpertSetting = true;
        setting_Orthographic.OriginType = typeof(AdditionalSettings);
        setting_Orthographic.DefaultValue = false;
        setting_Orthographic.Load();

        //Normal Clipping Settings

        setting_NearClip = __instance.@interface.AddAndGetSetting(PortableCameraSettingType.Float);
        setting_NearClip.FloatChanged = new Action<float>(value => UpdateCameraSettingFloat("NearClip", value));
        setting_NearClip.SettingName = "NearClip";
        setting_NearClip.DisplayName = "Near Clip Plane";
        setting_NearClip.isExpertSetting = true;
        setting_NearClip.OriginType = typeof(AdditionalSettings);
        setting_NearClip.DefaultValue = 0.01f;
        setting_NearClip.MinValue = 0.001f;
        setting_NearClip.MaxValue = 5000f;
        setting_NearClip.Load();

        setting_FarClip = __instance.@interface.AddAndGetSetting(PortableCameraSettingType.Float);
        setting_FarClip.FloatChanged = new Action<float>(value => UpdateCameraSettingFloat("FarClip", value));
        setting_FarClip.SettingName = "FarClip";
        setting_FarClip.DisplayName = "Far Clip Plane";
        setting_FarClip.isExpertSetting = true;
        setting_FarClip.OriginType = typeof(AdditionalSettings);
        setting_FarClip.DefaultValue = 1000f;
        setting_FarClip.MinValue = 0.002f;
        setting_FarClip.MaxValue = 5000f;
        setting_FarClip.Load();

        //Orthographic Settings

        setting_OrthographicSize = __instance.@interface.AddAndGetSetting(PortableCameraSettingType.Float);
        setting_OrthographicSize.FloatChanged = new Action<float>(value => UpdateCameraSettingFloat("OrthographicSize", value));
        setting_OrthographicSize.SettingName = "OrthographicSize";
        setting_OrthographicSize.DisplayName = "Orthographic Size";
        setting_OrthographicSize.isExpertSetting = true;
        setting_OrthographicSize.OriginType = typeof(AdditionalSettings);
        setting_OrthographicSize.DefaultValue = 5f;
        setting_OrthographicSize.MinValue = 0.01f;
        setting_OrthographicSize.MaxValue = 150f;
        setting_OrthographicSize.Load();

        setting_OrthographicNearClip = __instance.@interface.AddAndGetSetting(PortableCameraSettingType.Float);
        setting_OrthographicNearClip.FloatChanged = new Action<float>(value => UpdateCameraSettingFloat("OrthographicNearClip", value));
        setting_OrthographicNearClip.SettingName = "OrthographicNearClip";
        setting_OrthographicNearClip.DisplayName = "Orthographic Near";
        setting_OrthographicNearClip.isExpertSetting = true;
        setting_OrthographicNearClip.OriginType = typeof(AdditionalSettings);
        setting_OrthographicNearClip.DefaultValue = 0.001f;
        setting_OrthographicNearClip.MinValue = -5000f;
        setting_OrthographicNearClip.MaxValue = 5000f;
        setting_OrthographicNearClip.Load();

        setting_OrthographicFarClip = __instance.@interface.AddAndGetSetting(PortableCameraSettingType.Float);
        setting_OrthographicFarClip.FloatChanged = new Action<float>(value => UpdateCameraSettingFloat("OrthographicFarClip", value));
        setting_OrthographicFarClip.SettingName = "OrthographicFarClip";
        setting_OrthographicFarClip.DisplayName = "Orthographic Far";
        setting_OrthographicFarClip.isExpertSetting = true;
        setting_OrthographicFarClip.OriginType = typeof(AdditionalSettings);
        setting_OrthographicFarClip.DefaultValue = 1000f;
        setting_OrthographicFarClip.MinValue = -5000f;
        setting_OrthographicFarClip.MaxValue = 5000f;
        setting_OrthographicFarClip.Load();

        OnUpdateOptionsDisplay();
    }

    public void OnWorldLoaded(Camera playerCamera)
    {
        orthographicMode = false;
        referenceCamera = playerCamera;
        if (referenceCamera != null)
        {
            if (CopyWorldNearClip)
                setting_NearClip.Set(referenceCamera.nearClipPlane);
            if (CopyWorldFarClip)
                setting_FarClip.Set(referenceCamera.farClipPlane);
        }
    }

    public void OnUpdateOptionsDisplay(bool expertMode = true)
    {
        if (!expertMode)
        {
            return;
        }
        setting_NearClip.settingsObject.SetActive(!orthographicMode);
        setting_FarClip.settingsObject.SetActive(!orthographicMode);
        setting_OrthographicSize.settingsObject.SetActive(orthographicMode);
        setting_OrthographicNearClip.settingsObject.SetActive(orthographicMode);
        setting_OrthographicFarClip.settingsObject.SetActive(orthographicMode);
    }

    public void UpdateOrthographicMode()
    {
        if (PortableCamera.Instance != null)
        {
            PortableCamera.Instance.cameraComponent.orthographic = orthographicMode;
        }
        if (orthographicMode)
        {
            UpdateCameraSettingFloat("OrthographicNearClip", setting_OrthographicNearClip.Slider.value);
            UpdateCameraSettingFloat("OrthographicFarClip", setting_OrthographicFarClip.Slider.value);
        }
        else
        {
            UpdateCameraSettingFloat("NearClip", setting_NearClip.Slider.value);
            UpdateCameraSettingFloat("FarClip", setting_FarClip.Slider.value);
        }
        OnUpdateOptionsDisplay();
    }

    public void UpdateCameraSettingBool(string setting, bool value)
    {
        if (referenceCamera != null)
        {
            switch (setting)
            {
                //Camera Settings
                case "Orthographic":
                    orthographicMode = value;
                    UpdateOrthographicMode();
                    break;
                //Internal Settings
                case "CopyNearClip":
                    CopyWorldNearClip = value;
                    if (CopyWorldNearClip)
                        setting_NearClip.Set(referenceCamera.nearClipPlane);
                    break;
                case "CopyFarClip":
                    CopyWorldFarClip = value;
                    if (CopyWorldFarClip)
                        setting_FarClip.Set(referenceCamera.farClipPlane);
                    break;
            }
        }
    }

    public void UpdateCameraSettingFloat(string setting, float value)
    {
        if (PortableCamera.Instance != null)
        {
            switch (setting)
            {
                //Camera Settings
                case "NearClip":
                    if (!orthographicMode)
                        PortableCamera.Instance.cameraComponent.nearClipPlane = value;
                    break;
                case "FarClip":
                    if (!orthographicMode)
                        PortableCamera.Instance.cameraComponent.farClipPlane = value;
                    break;
                //Orthographic Mode
                case "OrthographicSize":
                    PortableCamera.Instance.cameraComponent.orthographicSize = value;
                    break;
                case "OrthographicNearClip":
                    if (orthographicMode)
                        PortableCamera.Instance.cameraComponent.nearClipPlane = value;
                    break;
                case "OrthographicFarClip":
                    if (orthographicMode)
                        PortableCamera.Instance.cameraComponent.farClipPlane = value;
                    break;
            }
        }
    }
}
