using ABI.CCK.Components;
using ABI_RC.Core.AudioEffects;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using DarkRift;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace NAK.Melons.PropUndoButton;

// https://pixabay.com/sound-effects/selection-sounds-73225/

public class PropUndoButton : MelonMod
{
    public static List<DeletedProp> deletedProps = new List<DeletedProp>();

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(PropUndoButton));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle Undo Prop Button.");

    public static readonly MelonPreferences_Entry<bool> EntryUseSFX =
        Category.CreateEntry("Use SFX", false, description: "Toggle audio queues for prop spawn, undo, redo, or warning.");

    // audio clip names, InterfaceAudio adds "PropUndo_" prefix
    public const string sfx_spawn = "PropUndo_sfx_spawn";
    public const string sfx_undo = "PropUndo_sfx_undo";
    public const string sfx_redo = "PropUndo_sfx_redo";
    public const string sfx_warn = "PropUndo_sfx_warn";

    public const int redoHistoryLimit = 20; // amount that can be in history at once
    public const int redoTimeoutLimit = 120; // seconds

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // delete all props in reverse order for redo
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.DeleteMyProps)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnDeleteMyProps), BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // prop spawn sfx
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.SpawnProp)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnSpawnProp), BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // prop delete sfx, log for possible redo
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.DeletePropByInstanceId)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnDeletePropByInstanceId), BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // desktop input patch so we don't run in menus/gui
            typeof(InputModuleMouseKeyboard).GetMethod(nameof(InputModuleMouseKeyboard.UpdateInput)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnUpdateInput), BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // clear redo list on world change
            typeof(CVRWorld).GetMethod(nameof(CVRWorld.ConfigureWorld)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnWorldLoad), BindingFlags.NonPublic | BindingFlags.Static))
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
        string[] clipNames = { "sfx_spawn.wav", "sfx_undo.wav", "sfx_redo.wav", "sfx_warn.wav" };
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

    private static void OnWorldLoad() => deletedProps.Clear();

    private static void OnUpdateInput()
    {
        if (!EntryEnabled.Value) return;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
            {
                RedoProp();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                UndoProp();
            }
        }
    }

    private static void OnSpawnProp()
    {
        if (!EntryEnabled.Value) return;

        if (!IsPropSpawnAllowed() || IsAtPropLimit())
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        PlayAudioModule(sfx_spawn);
    }

    private static void OnDeletePropByInstanceId(string instanceId)
    {
        if (!EntryEnabled.Value || string.IsNullOrEmpty(instanceId)) return;

        var propData = GetPropByInstanceIdAndOwnerId(instanceId);
        if (propData == null) return;

        AddDeletedProp(propData);
        PlayAudioModule(sfx_undo);
    }

    private static void AddDeletedProp(CVRSyncHelper.PropData propData)
    {
        if (deletedProps.Count >= redoHistoryLimit)
            deletedProps.RemoveAt(0);

        DeletedProp deletedProp = new DeletedProp(propData);
        deletedProps.Add(deletedProp);
    }

    // delete in reverse order for undo to work as expected
    private static bool OnDeleteMyProps()
    {
        if (!EntryEnabled.Value) return true;

        List<CVRSyncHelper.PropData> propsList = GetAllPropsByOwnerId();

        for (int i = propsList.Count - 1; i >= 0; i--)
        {
            CVRSyncHelper.PropData propData = propsList[i];

            if (propData.Spawnable == null)
            {
                propData.Recycle();
                continue;
            }

            propData.Spawnable.Delete();
        }

        return false;
    }

    private static void UndoProp()
    {
        var propData = GetLatestPropByOwnerId();
        if (propData == null)
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        if (propData.Spawnable == null)
        {
            propData.Recycle();
            return;
        }

        propData.Spawnable.Delete();
    }

    public static void RedoProp()
    {
        int index = deletedProps.Count - 1;
        if (index < 0 || index >= deletedProps.Count || IsAtPropLimit())
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        // only allow redo of prop spawned in last minute
        DeletedProp deletedProp = deletedProps[index];
        if (Time.time - deletedProp.timeDeleted <= redoTimeoutLimit)
        {
            if (AttemptRedoProp(deletedProp.propGuid, deletedProp.position, deletedProp.rotation))
            {
                deletedProps.RemoveAt(index);
                PlayAudioModule(sfx_redo);
                return;
            }
        }

        PlayAudioModule(sfx_warn);
    }

    // original spawn prop method does not let you specify rotation
    public static bool AttemptRedoProp(string propGuid, Vector3 position, Vector3 rotation)
    {
        if (!IsPropSpawnAllowed()) return false;

        using (DarkRiftWriter darkRiftWriter = DarkRiftWriter.Create())
        {
            darkRiftWriter.Write(propGuid);
            darkRiftWriter.Write(position.x);
            darkRiftWriter.Write(position.y);
            darkRiftWriter.Write(position.z);
            darkRiftWriter.Write(rotation.x);
            darkRiftWriter.Write(rotation.y);
            darkRiftWriter.Write(rotation.z);
            darkRiftWriter.Write(1f);
            darkRiftWriter.Write(1f);
            darkRiftWriter.Write(1f);
            darkRiftWriter.Write(0f);
            using (Message message = Message.Create(10050, darkRiftWriter))
            {
                NetworkManager.Instance.GameNetwork.SendMessage(message, SendMode.Reliable);
            }
        }
        return true;
    }

    public static void PlayAudioModule(string module)
    {
        if (EntryUseSFX.Value)
        {
            InterfaceAudio.PlayModule(module);
        }
    }

    private static bool IsPropSpawnAllowed()
    {
        return MetaPort.Instance.worldAllowProps
            && MetaPort.Instance.settings.GetSettingsBool("ContentFilterPropsEnabled", false)
            && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;
    }

    public static bool IsAtPropLimit(int limit = 20)
    {
        return GetAllPropsByOwnerId().Count >= limit;
    }

    private static CVRSyncHelper.PropData GetPropByInstanceIdAndOwnerId(string instanceId)
    {
        return CVRSyncHelper.Props.Find(propData => propData.InstanceId == instanceId && propData.SpawnedBy == MetaPort.Instance.ownerId);
    }

    private static CVRSyncHelper.PropData GetLatestPropByOwnerId()
    {
        return CVRSyncHelper.Props.LastOrDefault(propData => propData.SpawnedBy == MetaPort.Instance.ownerId);
    }

    private static List<CVRSyncHelper.PropData> GetAllPropsByOwnerId()
    {
        return CVRSyncHelper.Props.FindAll(propData => propData.SpawnedBy == MetaPort.Instance.ownerId);
    }

    public class DeletedProp
    {
        public string propGuid;
        public Vector3 position;
        public Vector3 rotation;
        public float timeDeleted;

        public DeletedProp(CVRSyncHelper.PropData propData)
        {
            // Offset spawn height so game can account for it later
            Transform spawnable = propData.Spawnable.transform;
            Vector3 position = spawnable.position;
            position.y -= propData.Spawnable.spawnHeight;

            this.propGuid = propData.ObjectId;
            this.position = position;
            this.rotation = spawnable.rotation.eulerAngles;
            this.timeDeleted = Time.time;
        }
    }
}