using System.Text;
using ABI_RC.Core.Player;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.UI;
using ABI_RC.Core.UI.Hud;
using ABI_RC.Systems.ChatBox;
using ABI_RC.Systems.Communications.Settings;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.PlayerColors;
using MelonLoader;
using UnityEngine;

namespace NAK.ChatBoxHud;

public class ChatBoxHudMod : MelonMod
{
    public enum HudTarget { Disabled, LookAt, Nearest, LookAtThenNearest }
    public enum SourceFilter { None, FriendsOnly, Everyone }
    public enum MaxUsersOnHud { One, Two, Three }

    private const int MaxLines = 4;
    private const int BaseCPL = 62;
    private const int BaseSize = 80;
    private const int MinSize = 50;
    private const int ShrinkAt = 3;
    private const float MinLife = 5f;
    private const float MaxLife = 20f;
    private const float WPM = 250f;
    private const float CPW = 5f;
    private const float LookAngle = 20f;
    private const float FadePct = 0.2f;
    private const int MsgCap = 1000;

    private static readonly MelonPreferences_Category Cat =
        MelonPreferences.CreateCategory(nameof(ChatBoxHud));

    internal static readonly MelonPreferences_Entry<HudTarget> EntryHudMode =
        Cat.CreateEntry("hud_mode", HudTarget.LookAtThenNearest, "HUD Target Mode");
    
    internal static readonly MelonPreferences_Entry<SourceFilter> EntryHudFilter =
        Cat.CreateEntry("hud_filter", SourceFilter.Everyone, "Chat Filter",
            description: "Whose chat messages appear on the HUD.");

    internal static readonly MelonPreferences_Entry<MaxUsersOnHud> EntryMaxUsersOnHud =
        Cat.CreateEntry("max_users_on_hud", MaxUsersOnHud.Three, "Max Users on HUD",
            description: "How many users can be stacked on the HUD when multiple users chat at once.");
    
    internal static readonly MelonPreferences_Entry<bool> EntryUsePlayerColors =
        Cat.CreateEntry("player_colors", true, "Use Player Colors",
            description: "Use player-chosen colors for names instead of friend colors.");
    
    private static readonly Dictionary<string, ChatBuf> Buffers = new();
    private static readonly StringBuilder SB = new();
    private static readonly int[] VisSlots = new int[MaxLines * 3 + 1];

    private static PortalHudIndicator _hud;

    #region Slot System
    
    // Stable slot list: once a user occupies an index they keep it until
    // all their messages expire or they leave range. New users append to
    // the first open position. This prevents blocks from jumping around
    // when someone else sends a new message.
    private static readonly List<string> _slotIds = new();
    private static bool _hudActive;

    private struct Candidate
    {
        public string Id, Name;
        public Vector3 Pos;
        public float Score; // lower = higher priority
    }

    private static readonly List<Candidate> _candidates = new();
    private static readonly Dictionary<string, Candidate> _candidateLookup = new();

    #endregion

    private struct Incoming
    {
        public string Id, Name, Text;
        public ChatBoxAPI.MessageSource Source;
    }

    private class Msg
    {
        public string[] Lines;
        public int Size;
        public float Stamp, Life;
        public ChatBoxAPI.MessageSource Source;
    }

    private class ChatBuf
    {
        public string Id;
        public readonly List<Msg> Msgs = new();
    }

    public override void OnInitializeMelon()
    {
        ChatBoxAPI.OnMessageReceived += OnReceive;
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(OnPlayerSetupStart);
    }

    private static void OnPlayerSetupStart()
    {
        Transform src = HudController.Instance.PortalHudIndicator.transform;
        GameObject go = UnityEngine.Object.Instantiate(src.gameObject, src.parent);
        go.transform.SetLocalPositionAndRotation(src.localPosition, src.localRotation);
        go.transform.localScale = src.localScale;
        _hud = go.GetComponent<PortalHudIndicator>();
    }

    public override void OnUpdate()
    {
        Tick();
    }

    private static void Tick()
    {
        if (_hud == null || EntryHudMode.Value == HudTarget.Disabled) { Hide(); return; }
        Prune();

        int maxSlots = (int)EntryMaxUsersOnHud.Value + 1;
        GatherCandidates();

        // Remove slots whose user has no messages left or is no longer
        // a valid candidate (walked out of range, left the instance, etc.)
        for (int i = _slotIds.Count - 1; i >= 0; i--)
        {
            string sid = _slotIds[i];
            bool dead = !_candidateLookup.ContainsKey(sid)
                     || !Buffers.TryGetValue(sid, out ChatBuf b)
                     || b.Msgs.Count == 0;
            if (dead) _slotIds.RemoveAt(i);
        }

        // If the setting was lowered at runtime, trim from the end
        while (_slotIds.Count > maxSlots)
            _slotIds.RemoveAt(_slotIds.Count - 1);

        // Fill empty slots from best-scored candidates not already slotted
        if (_slotIds.Count < maxSlots && _candidates.Count > 0)
        {
            _candidates.Sort((a, b) => a.Score.CompareTo(b.Score));
            foreach (Candidate c in _candidates)
            {
                if (_slotIds.Count >= maxSlots) break;
                if (_slotIds.Contains(c.Id)) continue;
                _slotIds.Add(c.Id);
            }
        }

        if (_slotIds.Count == 0) { Hide(); return; }

        // Render every active slot into one combined string
        SB.Clear();
        bool first = true;
        foreach (string slotId in _slotIds)
        {
            if (!_candidateLookup.TryGetValue(slotId, out Candidate cand)) continue;
            if (!Buffers.TryGetValue(slotId, out ChatBuf buf) || buf.Msgs.Count == 0) continue;

            if (!first) SB.Append('\n');
            RenderBlock(SB, cand.Name, cand.Id, buf, cand.Pos);
            first = false;
        }

        _hud.SetText(SB.ToString());
        if (!_hudActive) { _hudActive = true; _hud.Activate(); }
    }

    private static void Hide()
    {
        if (!_hudActive) return;
        _hud?.Deactivate();
        _hudActive = false;
        _slotIds.Clear();
    }
    
    // Scores every player who has buffered messages, is in comms range,
    // and passes the current HudTarget mode filter.
    private static void GatherCandidates()
    {
        _candidates.Clear();
        _candidateLookup.Clear();

        HudTarget mode = EntryHudMode.Value;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 lp = PlayerSetup.Instance.GetPlayerPosition();
        Vector3 cf = cam.transform.forward;
        Vector3 cp = cam.transform.position;
        float commsRange = Comms_SettingsHandler.FalloffDistance + 1;

        foreach (KeyValuePair<string, ChatBuf> kvp in Buffers)
        {
            if (kvp.Value.Msgs.Count == 0) continue;
            if (!CVRPlayerManager.Instance.TryGetPlayerBase(kvp.Key, out PlayerBase pb)) continue;

            Vector3 pp = pb.GetPlayerWorldRootPosition();
            float d = Vector3.Distance(lp, pp);
            if (d > commsRange) continue;

            string uid = kvp.Key;
            string n = pb.PlayerUsername ?? "???";
            float angle = Vector3.Angle(cf, (pp - cp).normalized);
            bool inLook = angle < LookAngle;

            float score;
            switch (mode)
            {
                case HudTarget.LookAt:
                    if (!inLook) continue; // hard filter
                    score = angle;
                    break;
                case HudTarget.Nearest:
                    score = d;
                    break;
                case HudTarget.LookAtThenNearest:
                    // look-at targets always rank above distance-only
                    score = inLook ? angle : 1000f + d;
                    break;
                default:
                    continue;
            }

            Candidate c = new Candidate { Id = uid, Name = n, Pos = pp, Score = score };
            _candidates.Add(c);
            _candidateLookup[uid] = c;
        }
    }
    
    private static void RenderBlock(StringBuilder sb, string playerName, string playerId, 
        ChatBuf buf, Vector3 tgtPos)
    {
        Camera cam = PlayerSetup.Instance.activeCam;
        float now = Time.time;
        
        // direction arrows from camera angle to target
        float la = 0f, ra = 0f;
        {
            Vector3 toT = tgtPos - cam.transform.position;
            Vector3 fwd = cam.transform.forward;
            toT.y = 0f; 
            fwd.y = 0f;
            if (toT.sqrMagnitude > 0.001f && fwd.sqrMagnitude > 0.001f)
            {
                float sa = Vector3.SignedAngle(fwd.normalized, toT.normalized, Vector3.up);
                float abs = Mathf.Abs(sa);
                if (abs > LookAngle)
                {
                    float t = Mathf.InverseLerp(LookAngle, 90f, abs);
                    float v = Mathf.Clamp01(t);
                    la = sa < 0f ? v : 0f;
                    ra = sa > 0f ? v : 0f;
                }
            }
        }

        // name color: player colors or friend-based fallback
        string hex;
        if (EntryUsePlayerColors.Value)
        {
            PlayerColors pc = PlayerColorsManager.GetPlayerColors(playerId);
            hex = "#" + ColorUtility.ToHtmlStringRGB(pc.PrimaryColor);
        }
        else hex = Friends.FriendsWith(playerId) ? "#4FC3F7" : "#E0E0E0";

        // header line
        AppendAlpha(sb, la); sb.Append("<b>\u25C4 </b>");
        sb.Append("<alpha=#FF><b><color=").Append(hex).Append('>')
          .Append(Esc(playerName)).Append("</color></b>");
        AppendAlpha(sb, ra); sb.Append("<b> \u25BA</b><alpha=#FF>\n");

        // allocate visible line count per message, newest gets priority
        int count = 0, budget = MaxLines;
        for (int i = buf.Msgs.Count - 1; i >= 0 && budget > 0; i--)
        {
            int v = Mathf.Min(buf.Msgs[i].Lines.Length, budget);
            VisSlots[count++] = v;
            budget -= v;
        }

        // render oldest to newest (slots were stored newest-first, so reverse)
        int startIdx = buf.Msgs.Count - count;
        for (int i = 0; i < count; i++)
        {
            int mi = startIdx + i;
            int vis = VisSlots[count - 1 - i];
            Msg m = buf.Msgs[mi];
            float age = now - m.Stamp;

            float fadeAt = m.Life * (1f - FadePct);
            float alpha = age < fadeAt ? 1f
                : Mathf.Clamp01(1f - (age - fadeAt) / (m.Life * FadePct));
            byte a = (byte)(alpha * 255);

            // smooth line-by-line scroll for overflowing messages
            int off = 0;
            if (m.Lines.Length > vis)
            {
                float usable = m.Life * (1f - FadePct);
                float perLine = usable / m.Lines.Length;
                float initHold = perLine * vis;
                off = Mathf.Min(
                    (int)(Mathf.Max(0f, age - initHold) / perLine),
                    m.Lines.Length - vis);
            }

            for (int j = 0; j < vis; j++)
            {
                sb.Append("<alpha=#").Append(a.ToString("X2")).Append('>')
                  .Append("<size=").Append(m.Size).Append("%>")
                  .Append(m.Lines[off + j])
                  .Append("</size>\n");
            }
        }

        // trim trailing newline from this block
        if (sb.Length > 0 && sb[sb.Length - 1] == '\n') sb.Length--;
    }

    private static void AppendAlpha(StringBuilder sb, float a01)
    {
        sb.Append("<alpha=#").Append(((byte)(a01 * 255)).ToString("X2")).Append('>');
    }
    
    private static void OnReceive(ChatBoxAPI.ChatBoxMessage msg)
    {
        if (msg.Source != ChatBoxAPI.MessageSource.Internal) return;
        if (string.IsNullOrWhiteSpace(msg.Message)) return;

        string sid = msg.SenderGuid;
        if (!CVRPlayerManager.Instance.TryGetPlayerBase(sid, out PlayerBase pb)) return;

        SourceFilter filter = EntryHudFilter.Value;
        if (filter == SourceFilter.None) return;
        if (filter == SourceFilter.FriendsOnly && !Friends.FriendsWith(sid)) return;

        Store(new Incoming
        {
            Id = sid,
            Name = pb.PlayerUsername ?? "???",
            Text = msg.Message,
            Source = msg.Source
        });
    }

    private static void Store(in Incoming inc)
    {
        if (!Buffers.TryGetValue(inc.Id, out ChatBuf buf))
        {
            buf = new ChatBuf { Id = inc.Id };
            Buffers[inc.Id] = buf;
        }

        string text = inc.Text;
        if (text.Length > MsgCap) text = text.Substring(0, MsgCap);
        text = text.Replace("\r", "");

        Fit(text, out string[] lines, out int size);
        for (int i = 0; i < lines.Length; i++) lines[i] = Esc(lines[i]);

        buf.Msgs.Add(new Msg
        {
            Lines = lines,
            Size = size,
            Stamp = Time.time,
            Life = Life(text.Length),
            Source = inc.Source
        });

        while (buf.Msgs.Count > MaxLines * 3)
            buf.Msgs.RemoveAt(0);
    }

    private static void Prune()
    {
        float now = Time.time;
        foreach (ChatBuf buf in Buffers.Values)
            buf.Msgs.RemoveAll(m => now - m.Stamp > m.Life);
    }

    // shrink text size until line count is manageable
    private static void Fit(string text, out string[] lines, out int size)
    {
        lines = Wrap(text, BaseCPL);
        size = BaseSize;
        if (lines.Length <= ShrinkAt) return;

        for (int s = BaseSize - 5; s >= MinSize; s -= 5)
        {
            int cpl = (int)(BaseCPL * BaseSize / (float)s);
            string[] attempt = Wrap(text, cpl);
            lines = attempt; size = s;
            if (attempt.Length <= ShrinkAt) return;
        }
    }

    // word wrap respecting embedded newlines
    private static string[] Wrap(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return new[] { string.Empty };

        List<string> result = new List<string>(4);
        foreach (string seg in text.Split('\n'))
        {
            if (seg.Length <= max) { result.Add(seg); continue; }

            StringBuilder cur = new StringBuilder(max);
            foreach (string w in seg.Split(' '))
            {
                if (w.Length == 0) continue;
                if (w.Length > max)
                {
                    if (cur.Length > 0) { result.Add(cur.ToString()); cur.Clear(); }
                    for (int c = 0; c < w.Length; c += max)
                        result.Add(w.Substring(c, Math.Min(max, w.Length - c)));
                    continue;
                }
                if (cur.Length == 0) cur.Append(w);
                else if (cur.Length + 1 + w.Length <= max) cur.Append(' ').Append(w);
                else { result.Add(cur.ToString()); cur.Clear(); cur.Append(w); }
            }
            if (cur.Length > 0) result.Add(cur.ToString());
        }
        return result.ToArray();
    }

    private static float Life(int chars)
    {
        return Mathf.Clamp(MinLife + chars * (60f / (WPM * CPW)), MinLife, MaxLife);
    }

    private static string Esc(string s) => s.Replace("<", "<\u200B");
}