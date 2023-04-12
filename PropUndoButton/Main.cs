using ABI.CCK.Components;
using ABI_RC.Core.AudioEffects;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
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
        Category.CreateEntry("Use SFX", true, description: "Enable or disable undo SFX.");

    // audio clip names, InterfaceAudio adds "PropUndo_" prefix
    public const string sfx_spawn = "PropUndo_sfx_spawn";
    public const string sfx_undo = "PropUndo_sfx_undo";
    public const string sfx_redo = "PropUndo_sfx_redo";
    public const string sfx_warn = "PropUndo_sfx_warn";

    public const int redoHistoryLimit = 5; // amount that can be in history at once
    public const int redoTimeoutLimit = 60; // seconds

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

    private static void OnUpdateInput()
    {
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

        if (!MetaPort.Instance.worldAllowProps
            || !MetaPort.Instance.settings.GetSettingsBool("ContentFilterPropsEnabled", false)
            || NetworkManager.Instance.GameNetwork.ConnectionState != ConnectionState.Connected)
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        // might need to touch
        if (IsAtPropLimit())
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        PlayAudioModule(sfx_spawn);
    }

    private static void OnDeletePropByInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return;

        CVRSyncHelper.PropData propData = GetPropByInstanceId(instanceId);
        if (propData == null) return;

        // Add the spawned prop to the history of deleted props
        if (deletedProps.Count >= redoHistoryLimit) deletedProps.RemoveAt(0); // Remove the oldest item

        // offset spawn height so game can account for it later
        Vector3 position = propData.Spawnable.transform.position;
        position.y -= propData.Spawnable.spawnHeight;

        DeletedProp deletedProp = new DeletedProp(propData.ObjectId, position, propData.Spawnable.transform.rotation);
        deletedProps.Add(deletedProp);

        PlayAudioModule(sfx_undo);
    }

    private static void OnWorldLoad()
    {
        deletedProps.Clear();
    }

    // delete in reverse order for undo to work as expected
    private static bool OnDeleteMyProps()
    {
        List<CVRSyncHelper.PropData> propsList = GetAllProps();

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
    }

    public static void RedoProp()
    {
        int index = deletedProps.Count - 1;
        if (index < 0 || index >= deletedProps.Count || IsAtPropLimit())
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        DeletedProp deletedProp = deletedProps[index];
        if (Time.time - deletedProp.timeDeleted <= redoTimeoutLimit) // only allow redo of prop spawned in last minute
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
    public static bool AttemptRedoProp(string propGuid, Vector3 position, Quaternion quaternion)
    {
        if (MetaPort.Instance.worldAllowProps
            && MetaPort.Instance.settings.GetSettingsBool("ContentFilterPropsEnabled", false)
            && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected)
        {
            using (DarkRiftWriter darkRiftWriter = DarkRiftWriter.Create())
            {
                darkRiftWriter.Write(propGuid);
                darkRiftWriter.Write(position.x);
                darkRiftWriter.Write(position.y);
                darkRiftWriter.Write(position.z);
                darkRiftWriter.Write(0f);
                darkRiftWriter.Write(quaternion.eulerAngles.y);
                darkRiftWriter.Write(0f);
                darkRiftWriter.Write(1f);
                darkRiftWriter.Write(1f);
                darkRiftWriter.Write(1f);
                darkRiftWriter.Write(0f);
                using (Message message = Message.Create(10050, darkRiftWriter))
                {
                    NetworkManager.Instance.GameNetwork.SendMessage(message, SendMode.Reliable);
                }
                return true;
            }
        }
        else
        {
            if (!MetaPort.Instance.worldAllowProps)
            {
                CohtmlHud.Instance.ViewDropText("Props are not allowed in this world", "");
            }
            return false;
        }
    }

    public static void PlayAudioModule(string module)
    {
        if (!EntryUseSFX.Value) return;
        InterfaceAudio.PlayModule(module);
    }

    public static bool IsAtPropLimit()
    {
        // might need rework
        return GetAllProps().Count >= 20;
    }

    private static CVRSyncHelper.PropData GetPropByInstanceId(string instanceId)
    {
        // find prop by instance id and if it is ours
        return CVRSyncHelper.Props.Find((CVRSyncHelper.PropData match) => (match.InstanceId == instanceId && match.SpawnedBy == MetaPort.Instance.ownerId));
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

    public class DeletedProp
    {
        public string propGuid;
        public Vector3 position;
        public Quaternion rotation;
        public float timeDeleted;

        public DeletedProp(string propGuid, Vector3 position, Quaternion rotation)
        {
            this.propGuid = propGuid;
            this.position = position;
            this.rotation = rotation;
            this.timeDeleted = Time.time;
        }
    }
}