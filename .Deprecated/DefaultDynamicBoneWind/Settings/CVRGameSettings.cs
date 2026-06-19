using ABI_RC.Core.Networking.GameServer;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Systems.XRManagement;
using UnityEngine;

namespace ABI_RC.Core.Savior
{
    public static class CVRGameSettings
    {
        public static readonly CVRSettingsFile GameSettingsFile = new("GameSettings");

        public static readonly CVRSettingsCategory TestSettingsCategory = 
            GameSettingsFile.CreateCategory("General", comment: "Test settings")
                .WithUI(sortOrder: 0, new UIFilters{ Platform = UIFilters.PlatformType.PCVR });

        public static readonly CVRSetting<Color> CoolBool =
            TestSettingsCategory.Create("Setting", Color.white, comment: "A cool toggle")
                .WithUI(sortOrder: 0, tooltip: "Pick a color", 
                    filters: new UIFilters { Level = UIFilters.SettingsLevel.Basic });
    
        public static readonly CVRSetting<bool> NotABool = 
            TestSettingsCategory.Create("Setting 2", defaultValue: false, comment: "A really cool toggle");
        
        public static readonly CVRSettingsCategory Audio =
            GameSettingsFile.CreateCategory("Audio", comment: "Audio volume levels", page: "Audio")
                .WithUI(sortOrder: 2);

        public static readonly CVRSetting<string> Microphone =
            Audio.Create("Microphone", "default", comment: "Active microphone device")
                .WithVariant("VR", string.Empty)
                .WithUI(sortOrder: 0, tooltip: "Select your microphone device");
        
        /// <summary>
        /// Settings with .WithVariant("VR", ...) or [Category.VR] in TOML respond to this.
        /// </summary>
        public static readonly CVRSettingsContext DeviceMode = new("DeviceMode");
        public static readonly CVRSettingsContext InstancePrivacy = new("InstancePrivacy");
        
        public static void Init()
        {
            GameSettingsFile.SetPath(Path.Combine(Application.persistentDataPath, "GameSettings.toml"));
            GameSettingsFile.RegisterContext(DeviceMode);      // Listen for .VR setting variants
            GameSettingsFile.RegisterContext(InstancePrivacy); // Listen for .Public/.Private setting variants
            GameSettingsFile.Load();
            
            SetDeviceModeActive(false);
            XRDeviceEvents.OnPostXRModeSwitch.AddListener(OnPostXRModeSwitch);
            GSInfoHandler.OnGSInfoUpdate += OnGSInfoUpdate;
        }

        private static void OnPostXRModeSwitch(XRModeSwitchEventArgs eventArgs)
            => SetDeviceModeActive(eventArgs.IsUsingVr);

        private static void OnGSInfoUpdate(GSInfoUpdate gsInfoUpdate, GSInfoChanged gsInfoChanged)
        {
            if (gsInfoChanged != GSInfoChanged.InstancePrivacy) return;
            SetInstancePrivateActive(Instances.IsInPrivateInstance());
        }
        
        private static void SetDeviceModeActive(bool isUsingVr)
            => DeviceMode.Active = isUsingVr ? "VR" : null;
        private static void SetInstancePrivateActive(bool isPrivate)
            => InstancePrivacy.Active = isPrivate ? "Private" : "Public";
    }
}