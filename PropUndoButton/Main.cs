using System.Reflection;
using ABI_RC.Core.AudioEffects;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI_RC.Systems.InputManagement.InputModules;
using ABI.CCK.Components;
using DarkRift;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.PropUndoButton;

// https://pixabay.com/sound-effects/selection-sounds-73225/

public class PropUndoButton : MelonMod
{
    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(PropUndoButton));

    public static readonly MelonPreferences_Entry<bool> EntryEnabled =
        Category.CreateEntry("Enabled", true, description: "Toggle Undo Prop Button.");

    public static readonly MelonPreferences_Entry<bool> EntryUseSFX =
        Category.CreateEntry("Use SFX", true,
            description: "Toggle audio queues for prop spawn, undo, redo, and warning.");

    internal static List<DeletedProp> deletedProps = new();

    // audio clip names, InterfaceAudio adds "PropUndo_" prefix
    private const string sfx_spawn = "PropUndo_sfx_spawn";
    private const string sfx_undo = "PropUndo_sfx_undo";
    private const string sfx_redo = "PropUndo_sfx_redo";
    private const string sfx_warn = "PropUndo_sfx_warn";
    private const string sfx_deny = "PropUndo_sfx_deny";

    private const int redoHistoryLimit = 20; // amount that can be in history at once
    private const int redoTimeoutLimit = 120; // seconds

    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // delete my props in reverse order for redo
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.DeleteMyProps)),
            new HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnDeleteMyProps),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // delete all props in reverse order for redo
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.DeleteAllProps)),
            new HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnDeleteAllProps),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // prop spawn sfx
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.SpawnProp)),
            postfix: new HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnSpawnProp),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // prop delete sfx, log for possible redo
            typeof(CVRSyncHelper).GetMethod(nameof(CVRSyncHelper.DeleteMyPropByInstanceIdOverNetwork)),
            postfix: new HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnDeletePropByInstanceId),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // desktop input patch so we don't run in menus/gui
            typeof(CVRInputModule_Keyboard).GetMethod(nameof(CVRInputModule_Keyboard.UpdateInput)),
            postfix: new HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnUpdateInput),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // clear redo list on world change
            typeof(CVRWorld).GetMethod(nameof(CVRWorld.ConfigureWorld)),
            postfix: new HarmonyMethod(typeof(PropUndoButton).GetMethod(nameof(OnWorldLoad),
                BindingFlags.NonPublic | BindingFlags.Static))
        );

        SetupDefaultAudioClips();
    }

    private void SetupDefaultAudioClips()
    {
        // PropUndo and audio folders do not exist, create them if dont exist yet
        var path = Application.streamingAssetsPath + "/Cohtml/UIResources/GameUI/mods/PropUndo/audio/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            LoggerInstance.Msg("Created PropUndo/audio directory!");
        }

        // copy embedded resources to this folder if they do not exist
        string[] clipNames = { "sfx_spawn.wav", "sfx_undo.wav", "sfx_redo.wav", "sfx_warn.wav", "sfx_deny.wav" };
        foreach (var clipName in clipNames)
        {
            var clipPath = Path.Combine(path, clipName);
            if (!File.Exists(clipPath))
            {
                // read the clip data from embedded resources
                byte[] clipData = null;
                var resourceName = "PropUndoButton.SFX." + clipName;
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    clipData = new byte[stream.Length];
                    stream.Read(clipData, 0, clipData.Length);
                }

                // write the clip data to the file
                using (FileStream fileStream = new(clipPath, FileMode.CreateNew))
                {
                    fileStream.Write(clipData, 0, clipData.Length);
                }

                LoggerInstance.Msg("Placed missing sfx in audio folder: " + clipName);
            }
        }
    }

    private static void OnWorldLoad()
    {
        deletedProps.Clear();
    }

    private static void OnUpdateInput()
    {
        if (!EntryEnabled.Value) return;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
                RedoProp();
            else if (Input.GetKeyDown(KeyCode.Z)) UndoProp();
        }
    }

    private static void OnSpawnProp()
    {
        if (!EntryEnabled.Value) return;

        if (!IsPropSpawnAllowed() || IsAtPropLimit())
        {
            PlayAudioModule(sfx_deny);
            return;
        }

        PlayAudioModule(sfx_spawn);
    }

    private static void OnDeletePropByInstanceId(string instanceId)
    {
        if (!EntryEnabled.Value || string.IsNullOrEmpty(instanceId)) return;

        CVRSyncHelper.PropData propData = GetPropByInstanceIdAndOwnerId(instanceId);
        if (propData == null) return;

        AddDeletedProp(propData);
        PlayAudioModule(sfx_undo);
    }

    private static void AddDeletedProp(CVRSyncHelper.PropData propData)
    {
        if (deletedProps.Count >= redoHistoryLimit)
            deletedProps.RemoveAt(0);

        DeletedProp deletedProp = new(propData);
        deletedProps.Add(deletedProp);
    }

    // delete in reverse order for undo to work as expected
    private static bool OnDeleteMyProps()
    {
        if (!EntryEnabled.Value) return true;

        var propsList = GetAllPropsByOwnerId();

        if (propsList.Count == 0)
        {
            PlayAudioModule(sfx_warn);
            return false;
        }

        for (var i = propsList.Count - 1; i >= 0; i--)
        {
            CVRSyncHelper.PropData propData = propsList[i];
            SafeDeleteProp(propData);
        }

        return false;
    }

    // delete in reverse order for undo to work as expected
    private static bool OnDeleteAllProps()
    {
        if (!EntryEnabled.Value) return true;

        var propsList = CVRSyncHelper.Props.ToArray();
        if (propsList.Length == 0)
        {
            PlayAudioModule(sfx_warn);
            return false;
        }

        for (var i = propsList.Length - 1; i >= 0; i--)
        {
            CVRSyncHelper.PropData propData = propsList[i];
            SafeDeleteProp(propData);
        }

        return false;
    }

    private static void UndoProp()
    {
        CVRSyncHelper.PropData propData = GetLatestPropByOwnerId();
        if (propData == null)
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        SafeDeleteProp(propData);
    }

    public static void RedoProp()
    {
        var index = deletedProps.Count - 1;
        if (index < 0)
        {
            PlayAudioModule(sfx_warn);
            return;
        }

        if (!IsPropSpawnAllowed() || IsAtPropLimit())
        {
            PlayAudioModule(sfx_deny);
            return;
        }

        // only allow redo of prop spawned in last minute
        DeletedProp deletedProp = deletedProps[index];
        if (Time.time - deletedProp.timeDeleted <= redoTimeoutLimit)
        {
            SendRedoProp(deletedProp.propGuid, deletedProp.position, deletedProp.rotation);
            deletedProps.RemoveAt(index);
            PlayAudioModule(sfx_redo);
        }
        else
        {
            // if latest prop is too old, same with rest
            deletedProps.Clear();
            PlayAudioModule(sfx_warn);
        }
    }

    // original spawn prop method does not let you specify rotation
    public static void SendRedoProp(string propGuid, Vector3 position, Vector3 rotation)
    {
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
    }

    public static void PlayAudioModule(string module)
    {
        if (EntryUseSFX.Value) InterfaceAudio.PlayModule(module);
    }

    private static void SafeDeleteProp(CVRSyncHelper.PropData propData)
    {
        if (propData.Spawnable == null)
        {
            // network delete prop manually, then delete prop data
            CVRSyncHelper.DeleteMyPropByInstanceIdOverNetwork(propData.InstanceId);
            propData.Recycle();
            return;
        }

        // delete prop, prop data, and network delete prop
        propData.Spawnable.Delete();
    }

    private static bool IsPropSpawnAllowed()
    {
        return MetaPort.Instance.worldAllowProps
               && MetaPort.Instance.settings.GetSettingsBool("ContentFilterPropsEnabled")
               && NetworkManager.Instance.GameNetwork.ConnectionState == ConnectionState.Connected;
    }

    public static bool IsAtPropLimit(int limit = 21)
    {
        return GetAllPropsByOwnerId().Count >= limit;
    }

    private static CVRSyncHelper.PropData GetPropByInstanceIdAndOwnerId(string instanceId)
    {
        return CVRSyncHelper.Props.Find(propData =>
            propData.InstanceId == instanceId && propData.SpawnedBy == MetaPort.Instance.ownerId);
    }

    private static CVRSyncHelper.PropData GetLatestPropByOwnerId()
    {
        // return last prop spawned by owner in CVRSyncHelper.MySpawnedPropInstanceIds
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
            if (propData.Spawnable == null)
            {
                // use original spawn position and rotation / last known position and rotation
                position = new Vector3(propData.PositionX, propData.PositionY, propData.PositionZ);
                rotation = new Vector3(propData.RotationX, propData.RotationY, propData.RotationZ);
            }
            else
            {
                Transform spawnableTransform = propData.Spawnable.transform;
                position = spawnableTransform.position;
                rotation = spawnableTransform.rotation.eulerAngles;

                // Offset spawn height so game can account for it later
                position.y -= propData.Spawnable.spawnHeight;
            }

            propGuid = propData.ObjectId;
            timeDeleted = Time.time;
        }
    }
}