using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core;
using ABI_RC.Systems.PlayerColors;
using NAK.CleanPlates.Helpers;
using NAK.CleanPlates.UI;
using Object = UnityEngine.Object;
using UnityEngine;

namespace NAK.CleanPlates;

// Nameplate subsystem.
public static partial class PlateManager
{
    private const string NewUserRank = "New User";
    private const string GroupModeratorRank = "Group Moderator";
    private const string GroupModeratorTag = "Host";
    private const string UnknownUser = "Unknown User";

    private static bool ShouldDisplayImage(PlayerBase player)
        => _imageVisibility == CVRSettings.CVRSettingsOptionFriends.Always
           || (_imageVisibility == CVRSettings.CVRSettingsOptionFriends.FriendsOnly && Friends.FriendsWith(player.PlayerId));

    #region Game Events

    private static void OnFriendsListUpdated()
    {
        UpdateFriendsStatesForAll();
        RefreshAllIcons(); // FriendsOnly image visibility may have flipped per player
    }

    private static void OnPlayerLeaveEntity(CVRPlayerEntity playerEntity)
    {
        NameplateIcons.Release(playerEntity.PuppetMaster);
        profileRefreshed.Remove(playerEntity.PuppetMaster);
        Unregister(playerEntity.PuppetMaster);
    }
    
    private static void OnInstanceDisconnected(string _)
        => UpdateLocalRank(); // kill the GS overridden rank, descriptor is back to API rank now

    internal static void OnAuthenticated()
    {
        // The result is too fucked to parse so just check with AuthManager.
        UpdateLocalPlate(AuthManager.IsAuthenticated);
    }

    #endregion Game Events

    #region Plate Lifecycle

    internal static void OnOverheadControllerStart(OverheadController overheadController)
    {
        PlayerBase player = overheadController.playerBase;
        Transform parent = overheadController.canvas.transform;
        if (!NameplateTheme.Source) NameplateTheme.Source = overheadController.nameplate.usrNameText;
        CreatePlate(player, parent, overheadController);
    }

    private static void CreatePlate(PlayerBase player, Transform parent, OverheadController overhead)
    {
        bool isLocal = player.IsLocalPlayer;
        var data = new NameplateData
        {
            Username = player.PlayerUsername,
            Pronouns = string.Empty,
            Status = string.Empty,
            StatusKind = NameplateStatusKind.None,
            IsFriend = !isLocal && Friends.FriendsWith(player.PlayerId),
        };
        ApplyDescriptor(data, player, isLocal
            ? PlayerColorsManager.CurrentColors
            : PlayerColorsManager.GetPlayerColors(player.PlayerId));

        Entry entry = Register(player, parent, data);
        var overheads = overhead._overheads;
        overheads.Add(entry.Handle);
        overheads.Add(entry.ChatHandle);
        ThirdpartySupport.RaisePlateAttached(player, entry.Plate);
        ApplyBubbleSettings(entry.Chat);
        ApplyMuted(entry);

        if (!isLocal)
        {
            RequestIcon(player);
            FetchPronouns(player);
        }
    }

    private static void UpdateLocalPlate(bool authenticated)
    {
        string username = authenticated 
            ? PlayerSetup.Instance.PlayerUsername 
            : UnknownUser;
        
        MentionTag = $"@{username}";
        UpdateData(PlayerSetup.Instance, d =>
        {
            d.Username = username;
            d.Pronouns = string.Empty;
            d.Status = string.Empty;
            d.StatusKind = NameplateStatusKind.None;
            d.IsFriend = false;
            ApplyDescriptor(d, PlayerSetup.Instance, PlayerColorsManager.CurrentColors);
        });

        if (authenticated)
        {
            RequestIcon(PlayerSetup.Instance);
            FetchPronouns(PlayerSetup.Instance); 
        }
        else
        {
            NameplateIcons.Release(PlayerSetup.Instance);
        }
    }

    private static void ApplyDescriptor(NameplateData data, PlayerBase player, PlayerColors colors)
    {
        data.PrimaryColor = colors.PrimaryColor;
        data.SecondaryColor = colors.SecondaryColor;
        ApplyRank(data, player.playerDescriptor);
    }

    private static void ApplyRank(NameplateData data, PlayerDescriptor descriptor)
    {
        string fullName = descriptor.GetRankFullName();
        bool groupModerator = fullName == GroupModeratorRank;

        data.IsNewUser = fullName == NewUserRank;
        data.RankTag = data.IsNewUser ? string.Empty
            : groupModerator ? GroupModeratorTag
            : RankTag(descriptor);
        data.RankColor = descriptor.GetRankColor();
    }

    private static string RankTag(PlayerDescriptor descriptor)
    {
        // Prefer explicit abbreviations.
        string abbreviation = descriptor.GetRankAbbreviation();
        if (!string.IsNullOrWhiteSpace(abbreviation))
            return abbreviation;

        // Create abbreviation from full name.
        string fullName = descriptor.GetRankFullName();
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        string[] words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Use up to 3 letters for single-word ranks (e.g. "Legend" -> "LEG"),
        if (words.Length == 1)
        {
            string word = words[0].ToUpperInvariant();
            return word.Length <= 3 ? word : word[..3];
        }

        // Use initials (e.g. "Grand Master" -> "GM").
        return new string(words
            .Take(3)
            .Select(w => char.ToUpperInvariant(w[0]))
            .ToArray());
    }

    private static void UpdateLocalRank() 
        => RefreshRank(PlayerSetup.Instance);

    internal static void RefreshRank(PlayerBase player)
        => UpdateData(player, d => ApplyRank(d, player.playerDescriptor));

    #endregion Plate Lifecycle

    #region Icons

    // Nothing in the game tells us someone changed their pfp or pronouns, so the
    // owner has to say so and everyone refetches.
    internal static void RefreshProfile(PlayerBase player)
    {
        // Anyone in the instance can send that message as often as they like,
        // and each one costs us an api request.
        if (player == null) return;
        float now = Time.time;
        if (profileRefreshed.TryGetValue(player, out float last)
            && now - last < ProfileRefreshCooldown)
            return;

        profileRefreshed[player] = now;
        RequestIcon(player);
        FetchPronouns(player);
    }

    private const float ProfileRefreshCooldown = 5f;
    private static readonly Dictionary<PlayerBase, float> profileRefreshed = new();

    internal static void RefreshProfile(string playerId)
    {
        if (!CVRPlayerManager.Instance.TryGetPlayerBase(playerId, out PlayerBase player))
            return;

        RefreshProfile(player);
    }

    private static void RequestIcon(PlayerBase player)
    {
        if (!NameplateView.ShowIconSlot || !ShouldDisplayImage(player))
            return;

        string url = player.playerDescriptor.profileImageUrl;
        if (!string.IsNullOrWhiteSpace(url))
            NameplateIcons.Fetch(player, url);
    }
    
    internal static void ResyncAll()
    {
        UpdateFriendsStatesForAll();
        RefreshAllIcons();
    }

    private static void RefreshAllIcons()
    {
        foreach (Entry e in entries)
        {
            PlayerBase player = e.Player;
            if (NameplateView.ShowIconSlot && ShouldDisplayImage(player))
            {
                RequestIcon(player);
            }
            else
            {
                NameplateIcons.Release(player);
                UpdateData(player, static d => d.Icon = null);
            }
        }
    }

    #endregion Icons

    private static void FetchPronouns(PlayerBase player)
    {
        // surely it is ok to fire api request per-player
        string playerId = player.PlayerId;
        Task.Run(async () =>
        {
            var response = await ApiConnection.MakeRequest<UserDetailsResponse>(
                ApiConnection.ApiOperation.UserDetails, new { userID = playerId });
            if (response?.Data != null)
                RootLogic.Instance.MainThreadQueue.Enqueue(() => UpdateData(player,
                    d => d.Status = response.Data.ProfilePronouns));
        });
    }
    
    public static float Opacity = 1f;
    public static float FullDetailDistance = 5f;
    public static float HideDistance = 30f;
    public static float FadeSpeed = 6f;
    public static float LodBlendSpeed = 3f;
    public static float RevealStagger = 0.05f;
    public static float RevealWindow = 0.5f;

    public static float MaxScale = 20f;
    public static float CollapsedMaxScale = 2f;
    public static float MaxRevealDistance = 200f;
    
    public static float ReadableDistance = 3.5f;
    public static float InspectReadableDistance = 1.75f;
    public static bool NearHide = true;
    public static float UserScale = 1f;
    public static NameplateStyle Style = NameplateStyle.Full;
    
    public static float NearHideDistance = 1.85f;
    public static float NearHideHysteresis = 0.25f;
    
    public static float ScaleSmoothing = 4f;
    public static float TalkStep = 0.03f;
    public static int DistanceChecksPerFrame = 10;
    public static int DistanceCheckDivisor = 6;
    public static float TalkAttack = 14f;
    public static float TalkRelease = 5f;
    public static float PacketSpeakingLevel = 0.5f;
    public static float TtsHoldSeconds = 0.5f;
    public static float HeightClearance = 0.05f;
    public static float CollapseThreshold = 0.5f;

    private static GameObject PlatePrefab => Style == NameplateStyle.Full
        ? CleanPlatesMod.CleanPlatesPrefab
        : CleanPlatesMod.CleanPlatesSimplePrefab;
    private static readonly List<Entry> entries = new();
    private static readonly Dictionary<PlayerBase, Entry> byPlayer = new();
    private static int scanIndex;

    private static readonly LoopProfiler PlateProfiler = new("Plates", trackDrawn: true);

    public static bool Profile
    {
        get => PlateProfiler.Enabled;
        set => PlateProfiler.Enabled = value;
    }

    // Game job places overhead, lift plate by bottom edge
    private static NameplateView InstantiatePlate(Transform parent)
    {
        NameplateView plate = Object.Instantiate(PlatePrefab, parent, false).GetComponent<NameplateView>();
        Transform plateTransform = plate.transform;
        plateTransform.SetLocalPositionAndRotation(
            new Vector3(0f, plate.BottomExtent * plateTransform.localScale.y, 0f), Quaternion.identity);
        return plate;
    }

    private static Entry Register(PlayerBase player, Transform parent, NameplateData data)
    {
        if (byPlayer.TryGetValue(player, out Entry existing))
            Unregister(existing.Player);
        
        NameplateTheme.EnsureBuilt();

        NameplateView plate = InstantiatePlate(parent);
        plate.Bind(data);
        plate.SetState(0f, 0f, 0f);
        plate.Chat.SetPlayerColors(data.PrimaryColor, data.SecondaryColor);
        
        // Local plate has special positioning in first person.
        if (player.IsLocalPlayer) NameplateTheme.Apply(plate.transform, local: true);

        var entry = new Entry
        {
            Parent = parent,
            Plate = plate,
            Chat = plate.Chat,
            Data = data,
            Dirty = true,
            IsLocal = player.IsLocalPlayer,
            Player = player,
            Puppet = player as PuppetMaster,
            OverheadController = player.overheadController,
            Anchor = player.overheadController.transform
        };
        entry.Handle = new OverheadHandle { Entry = entry };
        entry.ChatHandle = new ChatOverheadHandle { Entry = entry };
        entries.Add(entry);
        byPlayer[player] = entry;
        return entry;
    }

    private static void Unregister(PlayerBase player)
    {
        if (!byPlayer.Remove(player, out Entry entry)) return;
        entries.Remove(entry);

        // We put these two in the controller's list, and it outlives the plate
        // when the player is still here and only their avatar went away.
        if (entry.OverheadController != null)
        {
            var overheads = entry.OverheadController._overheads;
            overheads.Remove(entry.Handle);
            overheads.Remove(entry.ChatHandle);
        }

        ThirdpartySupport.RaisePlateDetached(entry.Player, entry.Plate);
        if (entry.Plate != null) Object.Destroy(entry.Plate.gameObject);
    }

    public static void UpdateData(PlayerBase player, Action<NameplateData> mutate)
    {
        if (!byPlayer.TryGetValue(player, out Entry entry)) return;

        mutate(entry.Data);
        NameplateView plate = entry.Plate;
        if (plate == null) return;

        plate.Bind(entry.Data);
        entry.Chat.SetPlayerColors(entry.Data.PrimaryColor, entry.Data.SecondaryColor);
        entry.Dirty = true;
    }
    
    public static bool TryGetPlate(PlayerBase player, out NameplateView plate)
    {
        plate = player != null && byPlayer.TryGetValue(player, out Entry entry) ? entry.Plate : null;
        return plate != null;
    }

    private static bool TryGetChat(PlayerBase player, out NameplateChat chat)
    {
        chat = player != null && byPlayer.TryGetValue(player, out Entry entry) ? entry.Chat : null;
        return chat != null;
    }

    private static void OnInspectingChanged(bool inspecting)
    {
        if (!inspecting) return;

        entries.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
        int count = entries.Count;
        float step = Mathf.Min(RevealStagger, RevealWindow / count);
        float now = Time.time;
        for (int i = 0; i < count; i++)
            entries[i].RevealTime = now + i * step;
    }

    // This path makes me so sad, but game doesn't tell me the changes :(
    private static void UpdateFriendsStatesForAll()
    {
        foreach (Entry e in entries)
        {
            // Read live, the local id was still empty when the entry was made.
            string id = e.Player.PlayerId;
            e.Data.IsFriend = !e.IsLocal && !string.IsNullOrEmpty(id) && Friends.FriendsWith(id);
            if (e.Plate == null) continue;
            e.Plate.Bind(e.Data);
            e.Dirty = true;
        }
    }

    internal static void ForEachChat(Action<NameplateChat> action)
    {
        foreach (Entry e in entries)
            if (e.Chat != null) action(e.Chat);
    }

    // Style is a different prefab, so the instances have to be thrown away.
    public static void RebuildAll()
    {
        foreach (var e in entries)
        {
            ThirdpartySupport.RaisePlateDetached(e.Player, e.Plate);
            if (e.Plate != null) Object.Destroy(e.Plate.gameObject);

            NameplateView plate = InstantiatePlate(e.Parent);
            plate.Bind(e.Data);
            plate.SetState(e.Alpha, e.LodBlend, e.DetailBlend);
            plate.Chat.SetPlayerColors(e.Data.PrimaryColor, e.Data.SecondaryColor);
            ApplyBubbleSettings(plate.Chat);
            if (e.IsLocal) NameplateTheme.Apply(plate.transform, local: true);

            e.Plate = plate;
            e.Chat = plate.Chat;
            ApplyMuted(e);

            ThirdpartySupport.RaisePlateAttached(e.Player, plate);
            e.AppliedTalk = -1f;
            e.Scale = -1f;
            e.Dirty = true;
        }
    }

    public static void RebindAll()
    {
        foreach (var e in entries)
        {
            if (e.Plate == null) continue;
            e.Plate.Bind(e.Data);
            e.Chat.RefreshHistory();
            e.Dirty = true;
        }
    }
}