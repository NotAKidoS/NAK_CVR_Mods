using ABI_RC.Core.AudioEffects;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace NAK.Melons.PropUndoButton;

// https://pixabay.com/sound-effects/selection-sounds-73225/

public class PropUndoButton : MelonMod
{
    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(PropUndoButton));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle Undo Prop Button.");

    public static readonly MelonPreferences_Entry<bool> EntryUseSFX =
        Category.CreateEntry("Use SFX", true, description: "Enable or disable undo SFX.");

    // audio clip names, InterfaceAudio adds "PropUndo_" prefix
    public const string sfx_spawn = "PropUndo_sfx_spawn";
    public const string sfx_undo = "PropUndo_sfx_undo";
    public const string sfx_warn = "PropUndo_sfx_warn";

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // prop spawn sfx
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.SpawnProp)),
            null,
            new HarmonyLib.HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnSpawnProp), BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // desktop input patch so we don't run in menus/gui
            typeof(InputModuleMouseKeyboard).GetMethod(nameof(InputModuleMouseKeyboard.UpdateInput)),
            null,
            new HarmonyLib.HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnUpdateInput), BindingFlags.NonPublic | BindingFlags.Static))
        );
        SetupDefaultAudioClips();
    }

    private void SetupDefaultAudioClips()
    {
        // PropUndo and audio folders do not exist, create them if dont exist yet
        string path = Application.streamingAssetsPath + "/Cohtml/UIResources/GameUI/mods/PropUndo/audio/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            LoggerInstance.Msg("Created PropUndo/audio directory!");
        }

        // copy embedded resources to this folder if they do not exist
        string[] clipNames = { "sfx_spawn.wav", "sfx_undo.wav", "sfx_warn.wav" };
        foreach (string clipName in clipNames)
        {
            string clipPath = Path.Combine(path, clipName);
            if (!File.Exists(clipPath))
            {
                // read the clip data from embedded resources
                byte[] clipData = null;
                string resourceName = "PropUndoButton.SFX." + clipName;
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    clipData = new byte[stream.Length];
                    stream.Read(clipData, 0, clipData.Length);
                }

                // write the clip data to the file
                using (FileStream fileStream = new FileStream(clipPath, FileMode.CreateNew))
                {
                    fileStream.Write(clipData, 0, clipData.Length);
                }

                LoggerInstance.Msg("Placed missing sfx in audio folder: " + clipName);
            }
        }
    }

    private static void OnUpdateInput()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            DeleteLatestSpawnable();
        }
    }

    private static void OnSpawnProp()
    {
        if (!EntryEnabled.Value) return;

        if (!MetaPort.Instance.worldAllowProps || !MetaPort.Instance.settings.GetSettingsBool("ContentFilterPropsEnabled", false))
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        if (GetAllProps().Count >= 20)
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        PlayAudioModule(sfx_spawn);
    }

    private static void DeleteLatestSpawnable()
    {
        if (!EntryEnabled.Value) return;

        var propData = GetLatestProp();

        if (propData == null)
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        if (propData.Spawnable == null)
        {
            propData.Recycle();
            // what should i do here?
            return;
        }

        propData.Spawnable.Delete();
        PlayAudioModule(sfx_undo);
    }

    private static void PlayAudioModule(string module)
    {
        if (!EntryUseSFX.Value) return;
        InterfaceAudio.PlayModule(module);
    }

    private static CVRSyncHelper.PropData GetLatestProp()
    {
        // should already be sorted by spawn order
        return CVRSyncHelper.Props.LastOrDefault((CVRSyncHelper.PropData match) => match.SpawnedBy == MetaPort.Instance.ownerId);
    }

    private static List<CVRSyncHelper.PropData> GetAllProps()
    {
        // im not storing the count because there is good chance itll desync from server
        return CVRSyncHelper.Props.FindAll((CVRSyncHelper.PropData match) => match.SpawnedBy == MetaPort.Instance.ownerId);
    }
}