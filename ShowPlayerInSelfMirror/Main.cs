using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.ContentClones;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using ABI.CCK.Components;
using MelonLoader;

namespace NAK.ShowPlayerInSelfMirror;

public class ShowPlayerInSelfMirrorMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        PlayerPlayerMirror.Initialize();
    }
}

/// <summary>
/// Manages adding/removing player clones to/from the personal mirror via the Player Select Page.
/// </summary>
public static class PlayerPlayerMirror
{
    private static readonly Dictionary<string, ContentCloneManager.CloneData> _clonedPlayers = new();

    private static Category _ourCategory;
    private static ToggleButton _toggle;

    public static void Initialize()
    {
        _ourCategory = QuickMenuAPI.PlayerSelectPage.AddCategory("Show Player In Self Mirror");
        _toggle = _ourCategory.AddToggle(
            "Add To Self Mirrors",
            "Should this player be shown in your self mirrors?",
            false
        );

        _toggle.OnValueUpdated += OnToggleChanged;
        QuickMenuAPI.OnPlayerSelected += OnPlayerSelected;

        CVRGameEventSystem.Avatar.OnRemoteAvatarClear.AddListener(OnRemoteAvatarCleared);
        CVRGameEventSystem.Avatar.OnRemoteAvatarLoad.AddListener(OnRemoteAvatarLoad);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(OnRemotePlayerLeave);
    }

    private static void OnToggleChanged(bool value)
    {
        string playerId = QuickMenuAPI.SelectedPlayerID;
        if (string.IsNullOrEmpty(playerId))
            return;

        if (value)
        {
            if (!_clonedPlayers.TryAdd(playerId, null))
                return;

            if (CVRPlayerManager.Instance.TryGetPlayerBase(playerId, out PlayerBase player))
            {
                if (!TryCreateClone(playerId, player))
                {
                    _clonedPlayers.Remove(playerId);
                    _toggle.ToggleValue = false;
                }
            }
        }
        else
        {
            RemoveAndForgetClone(playerId);
        }
    }

    private static void OnPlayerSelected(object _, string playerId)
    {
        // If this is us, hide the category entirely
        if (playerId == MetaPort.Instance.ownerId)
        {
            _ourCategory.Hidden = true;
            return;
        }
        
        // Show the category for other players
        _ourCategory.Hidden = false;
        
        bool enabled = _clonedPlayers.ContainsKey(playerId);
        _toggle.ToggleValue = enabled;
    }

    private static void OnRemoteAvatarCleared(CVRPlayerEntity playerEntity, CVRAvatar _)
    {
        string playerId = playerEntity.Uuid;

        if (!_clonedPlayers.TryGetValue(playerId, out ContentCloneManager.CloneData clone))
            return;

        if (clone != null)
        {
            ContentCloneManager.DestroyClone(clone);
            _clonedPlayers[playerId] = null;
        }
    }

    private static void OnRemoteAvatarLoad(CVRPlayerEntity playerEntity, CVRAvatar _)
    {
        string playerId = playerEntity.Uuid;

        if (!_clonedPlayers.ContainsKey(playerId))
            return;

        if (!CVRPlayerManager.Instance.TryGetPlayerBase(playerId, out PlayerBase player))
            return;

        TryCreateClone(playerId, player);
    }

    private static void OnRemotePlayerLeave(CVRPlayerEntity playerEntity)
    {
        string playerId = playerEntity.Uuid;
        RemoveAndForgetClone(playerId);
    }

    private static bool TryCreateClone(string playerId, PlayerBase player)
    {
        if (!player.AvatarObject)
            return false;

        if (_clonedPlayers.TryGetValue(playerId, out ContentCloneManager.CloneData existing)
            && existing is { IsDestroyed: false })
            return true;

        ContentCloneManager.CloneData clone = ContentCloneManager.CreateClone(
            player.AvatarObject,
            ContentCloneManager.CloneOptions.ExtensionOfPlayer
        );

        if (clone == null)
            return false;

        _clonedPlayers[playerId] = clone;
        return true;
    }

    private static void RemoveAndForgetClone(string playerId)
    {
        if (_clonedPlayers.TryGetValue(playerId, out ContentCloneManager.CloneData clone))
        {
            if (clone != null)
                ContentCloneManager.DestroyClone(clone);
        }

        _clonedPlayers.Remove(playerId);
    }
}