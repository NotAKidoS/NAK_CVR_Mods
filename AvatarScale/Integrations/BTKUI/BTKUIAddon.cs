using System.IO;
using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MelonLoader;
using NAK.AvatarScaleMod.AvatarScaling;
using UnityEngine;

namespace NAK.AvatarScaleMod.Integrations
{
    public static class BtkUiAddon
    {
        private static string _selectedPlayer;

        #region Initialization

        public static void Initialize()
        {
            PrepareIcons();
            SetupRootPage();
            SetupPlayerSelectPage();
            RegisterEventHandlers();
        }

        private static void PrepareIcons()
        {
            QuickMenuAPI.PrepareIcon(ModSettings.ModName, "ASM_Icon_AvatarHeightConfig", GetIconStream("ASM_Icon_AvatarHeightConfig.png"));
            QuickMenuAPI.PrepareIcon(ModSettings.ModName, "ASM_Icon_AvatarHeightCopy", GetIconStream("ASM_Icon_AvatarHeightCopy.png"));
        }

        private static void SetupRootPage()
        {
            // we only need the page, as we inject our own elements into it aa
            Page rootPage = new Page(ModSettings.ModName, ModSettings.SettingsCategory, true, "ASM_Icon_AvatarHeightConfig")
            {
                MenuTitle = ModSettings.SettingsCategory,
                MenuSubtitle = "Universal Scaling Settings"
            };
        }

        private static void SetupPlayerSelectPage()
        {
            // what other things would be worth adding here?
            Category category = QuickMenuAPI.PlayerSelectPage.AddCategory(ModSettings.SettingsCategory, ModSettings.ModName);
            Button button = category.AddButton("Copy Height", "ASM_Icon_AvatarHeightCopy", "Copy selected players Eye Height.");
            button.OnPress += OnCopyPlayerHeight;
        }

        private static void RegisterEventHandlers()
        {
            QuickMenuAPI.OnPlayerSelected += (_, id) => _selectedPlayer = id;
            QuickMenuAPI.OnMenuRegenerate += _ => ScheduleMenuInjection();
            QuickMenuAPI.OnTabChange += OnTabChange;
        }

        private static void ScheduleMenuInjection()
        {
            CVR_MenuManager.Instance.quickMenu.View.BindCall("asm-AvatarHeightUpdated", new Action<float>(OnAvatarHeightUpdated));
            SchedulerSystem.AddJob(InjectMenu, 1f, 1f, 1);
        }

        private static void InjectMenu()
        {
            AvatarScaleMod.Logger.Msg("Injecting into our BTKUI AvatarScaleMod page!");
            string menuJsPath = Path.Combine(Application.streamingAssetsPath, "Cohtml", "UIResources", "AvatarScaleMod", "menu.js");
            string menuJsContent = File.Exists(menuJsPath) ? File.ReadAllText(menuJsPath) : string.Empty;

            if (string.IsNullOrEmpty(menuJsContent))
            {
                AvatarScaleMod.Logger.Msg("Injecting embedded menu.js included with mod!");
                CVR_MenuManager.Instance.quickMenu.View._view.ExecuteScript(Scripts.GetEmbeddedScript("menu.js"));
            }
            else
            {
                AvatarScaleMod.Logger.Msg($"Injecting development menu.js found in: {menuJsPath}");
                CVR_MenuManager.Instance.quickMenu.View._view.ExecuteScript(menuJsContent);
            }
        }

        #endregion

        private static void OnCopyPlayerHeight()
        {
            float networkHeight = AvatarScaleManager.Instance.GetNetworkHeight(_selectedPlayer);
            if (networkHeight < 0) return;
            AvatarScaleManager.Instance.SetHeight(networkHeight);
        }

        private static void OnAvatarHeightUpdated(float height)
        {
            AvatarScaleManager.Instance.SetHeight(height);
        }

        #region Private Methods
        
        private static DateTime lastTime = DateTime.Now;
        private static void OnTabChange(string newTab, string previousTab)
        {
            if (newTab == "btkUI-AvatarScaleMod-MainPage")
            {
                TimeSpan timeDifference = DateTime.Now - lastTime;
                if (timeDifference.TotalSeconds <= 0.5)
                {
                    AvatarScaleManager.Instance.ResetHeight();
                    return;
                }
            }
            lastTime = DateTime.Now;
        }
        
        private static Stream GetIconStream(string iconName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string assemblyName = assembly.GetName().Name;
            return assembly.GetManifestResourceStream($"{assemblyName}.resources.{iconName}");
        }

        #endregion

        #region Melon Pref Helpers

        internal static void AddMelonToggle(ref Category category, MelonPreferences_Entry<bool> entry)
        {
            category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
        }

        internal static void AddMelonSlider(ref Page page, MelonPreferences_Entry<float> entry, float min, float max, int decimalPlaces = 2)
        {
            page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max, decimalPlaces).OnValueUpdated += f => entry.Value = f;
        }

        #endregion
    }
}