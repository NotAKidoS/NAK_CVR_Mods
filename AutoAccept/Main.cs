using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.API.UserWebsocket;
using ABI_RC.Core.Networking.IO.Social;
using HarmonyLib;
using MelonLoader;
using System.Reflection;
using ABI_RC.Core.Networking;
using ABI_RC.Core.UI;

namespace NAK.AutoAccept;

public class AutoAcceptMod : MelonMod
{
    private static readonly MelonPreferences_Category Category = MelonPreferences.CreateCategory(nameof(AutoAccept));
    
    private enum InviteStatus
    {
        Ask,
        AllFriends,
        SelectCategories,
        DoNotDisturb
    }

    private static readonly MelonPreferences_Entry<InviteStatus> EntryInviteStatus =
        Category.CreateEntry(
            identifier: "invite_status",
            InviteStatus.Ask,
            display_name: "Invite Status",
            description: "How incoming invite requests are handled.");

    private static readonly MelonPreferences_Entry<string> EntryAutoAcceptCategories =
        Category.CreateEntry(
            identifier: "auto_accept_categories",
            "",
            display_name: "Auto Accept Categories",
            description: "Categories whose members get auto-accepted in Select mode.");
    
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // auto accept
            typeof(ViewManager).GetMethod(nameof(ViewManager.ShowInviteRequest),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(AutoAcceptMod).GetMethod(nameof(OnPreShowInviteRequest),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // add useful notification
            typeof(ViewManager).GetMethod(nameof(ViewManager.UpdateInvites),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(AutoAcceptMod).GetMethod(nameof(OnPreUpdateInvites),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // eat useless notifications
            typeof(ViewManager).GetMethod(nameof(ViewManager.BufferMenuPopup),
                BindingFlags.Public | BindingFlags.Instance),
            prefix: new HarmonyMethod(typeof(AutoAcceptMod).GetMethod(nameof(OnPreBufferMenuPopup),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // add useful notification
            typeof(ViewManager).GetMethod(nameof(ViewManager.InvitePlayer),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(AutoAcceptMod).GetMethod(nameof(OnPostInvitePlayer),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // add useful notification
            typeof(ViewManager).GetMethod(nameof(ViewManager.RequestInvite),
                BindingFlags.Public | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(AutoAcceptMod).GetMethod(nameof(OnPostRequestInvite),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        HarmonyInstance.Patch( // new menu buttons
            typeof(ViewManager).GetMethod(nameof(ViewManager.Start),
                BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: new HarmonyMethod(typeof(AutoAcceptMod).GetMethod(nameof(OnViewManagerStart),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        Friends.OnFriendListUpdated += OnFriendsListUpdated;

        LoadAutoAcceptCategories();
    }

    private static readonly HashSet<string> _autoAcceptCategories = new();

    private static void LoadAutoAcceptCategories()
    {
        _autoAcceptCategories.Clear();
        string raw = EntryAutoAcceptCategories.Value;
        if (string.IsNullOrEmpty(raw)) return;
        foreach (string key in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            _autoAcceptCategories.Add(key.Trim());
    }

    private static void SaveAutoAcceptCategories()
    {
        EntryAutoAcceptCategories.Value = string.Join(",", _autoAcceptCategories);
    }
    
    private static readonly HashSet<string> AutoAcceptUserIds = new();

    private static void OnFriendsListUpdated()
    {
        RebuildAutoAcceptUserIds();
        PushAutoAcceptCategoriesToJs();
    }

    private static void RebuildAutoAcceptUserIds()
    {
        AutoAcceptUserIds.Clear();
        if (_autoAcceptCategories.Count > 0)
        {
            foreach (var (userId, friendInfo) in Friends.CurrentFriendsInfo)
            {
                if (friendInfo.Categories.Any(cat => _autoAcceptCategories.Contains(cat)))
                {
                    AutoAcceptUserIds.Add(userId);
                }
            }
        }
        AutoAcceptUserIds.TrimExcess();
    }

    private static readonly HashSet<string> UselessMenuPopups =
    [
        "You have already 3 outstanding invites to this user.",
        "Invite sent.",
        "Request invite successfully sent."
    ];
    
    private static void OnPreBufferMenuPopup(string message, ref bool __runOriginal)
    {
        // Kill the annoying fuckers 
        if (UselessMenuPopups.Contains(message)) __runOriginal = false;
    }

    private static void OnPreUpdateInvites(List<Invite> invites)
    {
        // Display received notification for incoming requests
        for (int index = 0; index < invites.Count; index++)
        {
            Invite invite = invites[index];
            ReceiveInvite(invite);
        }
    }

    private static void OnPreShowInviteRequest(List<RequestInvite> requestInvites, ref bool __runOriginal)
    {
        // Intercept and process incoming invite requests
        bool canAutoAcceptInvite = NetworkManager.Instance.IsConnectedToGameNetwork();
        switch (EntryInviteStatus.Value)
        {
            default:
            case InviteStatus.Ask:
                break;
            case InviteStatus.AllFriends:
                for (var index = requestInvites.Count - 1; index >= 0; index--)
                {
                    RequestInvite inviteRequest = requestInvites[index];
                    if (!canAutoAcceptInvite) CannotAcceptInviteRequest(inviteRequest);
                    else if (Friends.FriendsWith(inviteRequest.Sender.Id))
                    {
                        __runOriginal = false;
                        requestInvites.Remove(inviteRequest);
                        AcceptInviteRequest(inviteRequest);
                    }
                }
                break;
            case InviteStatus.SelectCategories:
                for (var index = requestInvites.Count - 1; index >= 0; index--)
                {
                    RequestInvite inviteRequest = requestInvites[index];
                    if (!canAutoAcceptInvite) CannotAcceptInviteRequest(inviteRequest);
                    else if (AutoAcceptUserIds.Contains(inviteRequest.Sender.Id))
                    {
                        __runOriginal = false;
                        requestInvites.Remove(inviteRequest);
                        AcceptInviteRequest(inviteRequest);
                    }
                }
                break;
            case InviteStatus.DoNotDisturb:
                __runOriginal = false;
                requestInvites.Clear();
                break;
        }

        // Display received notification for remaining unhandled requests
        for (int index = 0; index < requestInvites.Count; index++)
        {
            RequestInvite inviteRequest = requestInvites[index];
            ReceiveInviteRequest(inviteRequest);
        }
    }
    
    private static void AcceptInviteRequest(RequestInvite inviteRequest)
    {
        ApiConnection.SendWebSocketRequest(RequestType.RequestInviteAccept, new { id = inviteRequest.Id });
        CohtmlHud.Instance.ViewDropTextImmediate(
            "(Mod) Auto Accept",
            "Accepting invite",
            $"Accepted request from {inviteRequest.Sender.Name}",
            $"ACCEPTREQ+{inviteRequest.Sender.Id}", 
            false);
    }

    private static void CannotAcceptInviteRequest(RequestInvite inviteRequest)
    {
        CohtmlHud.Instance.ViewDropTextImmediate(
            "(Mod) Auto Accept",
            "Cannot accept invite",
            $"Received request from {inviteRequest.Sender.Name}",
            $"CANNOTACCEPTREQ+{inviteRequest.Sender.Id}", 
            false);
    }
    
    private static void ReceiveInvite(Invite invite)
    {
        CohtmlHud.Instance.ViewDropTextImmediate(
            "(Mod) Auto Accept",
            "Received invite",
            $"Received invite from {invite.User.Name}",
            $"RECEIVEDINV+{invite.User.Id}", 
            false);
    }
    
    private static void ReceiveInviteRequest(RequestInvite inviteRequest)
    {
        CohtmlHud.Instance.ViewDropTextImmediate(
            "(Mod) Auto Accept",
            "Received invite request",
            $"Received request from {inviteRequest.Sender.Name}",
            $"RECEIVEDREQ+{inviteRequest.Sender.Id}", 
            false);
    }
    
    private static void OnPostInvitePlayer()
    {
        ViewManager.Instance.NotifyUser("(Mod) Auto Accept", "Sent invite", 1f);
    }

    private static void OnPostRequestInvite()
    {
        ViewManager.Instance.NotifyUser("(Mod) Auto Accept", "Requested invite", 1f);
    }
    
    // Menu Patches
    
    private static void OnViewManagerStart()
    {
        ViewManager.Instance.cohtmlView.Listener.FinishLoad += _ =>
        {
            ViewManager.Instance.cohtmlView.View._view.ExecuteScript(InjectJs);
        };
        ViewManager.Instance.cohtmlView.Listener.ReadyForBindings += () =>
        {
            var view = ViewManager.Instance.cohtmlView.View;
            view.BindCall("NAKSetInviteStatus", new Action<string>(OnJsSetInviteStatus));
            view.BindCall("NAKSetAutoAcceptCategory", new Action<string, bool>(OnJsSetAutoAcceptCategory));
            view.BindCall("NAKRequestInviteState", new Action(PushInviteStateToJs));
        };
    }

    private static void OnJsSetInviteStatus(string statusName)
    {
        if (!Enum.TryParse(statusName, out InviteStatus status)) return;
        EntryInviteStatus.Value = status;
        RebuildAutoAcceptUserIds();
        PushInviteStatusToJs(); // echo back so the menu is authoritative
    }

    private static void OnJsSetAutoAcceptCategory(string categoryKey, bool add)
    {
        if (string.IsNullOrEmpty(categoryKey)) return;
        if (add) _autoAcceptCategories.Add(categoryKey);
        else _autoAcceptCategories.Remove(categoryKey);
        SaveAutoAcceptCategories();
        RebuildAutoAcceptUserIds();
        PushAutoAcceptCategoriesToJs();
    }

    private static void PushInviteStateToJs()
    {
        PushInviteStatusToJs();
        PushAutoAcceptCategoriesToJs();
    }

    private static void PushInviteStatusToJs()
    {
        ViewManager.Instance.cohtmlView.View.TriggerEvent("NAKInviteStatusUpdate", EntryInviteStatus.Value.ToString());
    }

    private static void PushAutoAcceptCategoriesToJs()
    {
        ViewManager.Instance.cohtmlView.View.TriggerEvent("NAKAutoAcceptCategoriesUpdate", ToJsonArray(_autoAcceptCategories));
    }

    private static string ToJsonArray(IEnumerable<string> values)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        bool first = true;
        foreach (string v in values)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(v.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
        }
        sb.Append(']');
        return sb.ToString();
    }

    private const string InjectJs = @"
(function () {
    if (window.__nakAutoAcceptInjected) return;
    window.__nakAutoAcceptInjected = true;

    var nakInviteStatus = 'Ask';
    var nakAutoAcceptCats = new Set();

    var STATUSES = [
        { key: 'Ask',              label: 'Ask',    tip: 'Show a prompt for each incoming invite.' },
        { key: 'AllFriends',       label: 'All',    tip: 'Auto-accept invites from any friend.' },
        { key: 'SelectCategories', label: 'Select', tip: 'Auto-accept friends in your starred categories.' },
        { key: 'DoNotDisturb',     label: 'DND',    tip: 'Silently ignore all incoming invites.' }
    ];

    function descFor(key) {
        for (var i = 0; i < STATUSES.length; i++)
            if (STATUSES[i].key === key) return STATUSES[i].tip;
        return '';
    }

    function showDesc(key) {
        var d = document.querySelector('#friends .nak-aa-desc');
        if (d) d.textContent = descFor(key || nakInviteStatus);
    }

    function injectStyle() {
        if (document.getElementById('nak-aa-style')) return;
        var css =
            '#friends .nak-aa-statusbar{display:flex;flex-direction:column;gap:6px;margin:8px 0 10px;padding-bottom:10px;border-bottom:1px solid rgba(255,255,255,.08);}' +
            '#friends .nak-aa-statusbar .nak-aa-label{font-size:.74em;letter-spacing:.04em;text-transform:uppercase;opacity:.85;font-weight:600;}' +
            '#friends .nak-aa-statusbar .nak-aa-options{display:flex;gap:4px;background:rgba(255,255,255,.05);padding:3px;border-radius:8px;}' +
            '#friends .nak-aa-statusbar .nak-aa-opt{flex:1;text-align:center;padding:5px 4px;border-radius:6px;cursor:pointer;font-size:.85em;opacity:.7;}' +
            '#friends .nak-aa-statusbar .nak-aa-opt:hover{opacity:1;}' +
            '#friends .nak-aa-statusbar .nak-aa-opt.active{opacity:1;background:rgba(255,255,255,.14);font-weight:600;}' +
            '#friends .nak-aa-statusbar .nak-aa-desc{font-size:.78em;line-height:1.3;opacity:.6;min-height:1.1em;}' +
            '#friends .filter-option{position:relative;}' +
            '#friends .filter-option .nak-aa-toggle{position:absolute;right:8px;top:50%;transform:translateY(-50%);cursor:pointer;line-height:1;opacity:0;pointer-events:none;}' +
            '#friends.nak-mode-select .filter-option[data-nak-key]{padding-right:22px;}' +
            '#friends.nak-mode-select .filter-option .nak-aa-toggle{opacity:.5;pointer-events:auto;}' +
            '#friends.nak-mode-select .filter-option .nak-aa-toggle:hover{opacity:.95;}' +
            '#friends.nak-mode-select .filter-option.nak-aa-eligible .nak-aa-toggle{opacity:1;}';
        var s = document.createElement('style');
        s.id = 'nak-aa-style';
        s.textContent = css;
        document.head.appendChild(s);
    }

    function setInviteStatus(key, tellCs) {
        nakInviteStatus = key;
        refreshStatusBar();
        if (tellCs && window.engine) engine.call('NAKSetInviteStatus', key);
    }

    function refreshStatusBar() {
        var opts = document.querySelectorAll('#friends .nak-aa-statusbar .nak-aa-opt');
        for (var i = 0; i < opts.length; i++)
            opts[i].classList.toggle('active', opts[i].getAttribute('data-nak-status') === nakInviteStatus);
        var f = document.getElementById('friends');
        if (f) f.classList.toggle('nak-mode-select', nakInviteStatus === 'SelectCategories');
        showDesc(null);
    }

    function buildStatusBar() {
        var filter = document.querySelector('#friends .list-filter');
        if (!filter || filter.querySelector('.nak-aa-statusbar')) return;

        var bar = document.createElement('div');
        bar.className = 'nak-aa-statusbar';

        var label = document.createElement('div');
        label.className = 'nak-aa-label';
        label.textContent = 'Incoming invites';
        bar.appendChild(label);

        var opts = document.createElement('div');
        opts.className = 'nak-aa-options';
        STATUSES.forEach(function (st) {
            var b = document.createElement('div');
            b.className = 'nak-aa-opt';
            b.textContent = st.label;
            b.setAttribute('data-nak-status', st.key);
            b.addEventListener('click', function () { setInviteStatus(st.key, true); });
            b.addEventListener('mouseenter', function () { showDesc(st.key); });
            b.addEventListener('mouseleave', function () { showDesc(null); });
            opts.appendChild(b);
        });
        bar.appendChild(opts);

        var desc = document.createElement('div');
        desc.className = 'nak-aa-desc';
        bar.appendChild(desc);

        var h1 = filter.querySelector('h1');
        if (h1) filter.insertBefore(bar, h1.nextSibling); else filter.appendChild(bar);
        refreshStatusBar();
    }

    function eligible(key) { return nakAutoAcceptCats.has(String(key)); }

    function paintCategory(el, key) {
        var on = eligible(key);
        el.classList.toggle('nak-aa-eligible', on);
        var t = el.querySelector('.nak-aa-toggle');
        if (t) {
            t.textContent = on ? '\u2605' : '\u2606';
            t.setAttribute('data-tooltip', on
                ? 'Auto-accepting invites from this category'
                : 'Click to auto-accept invites from this category');
        }
    }

    function decorateCategory(el, key) {
        if (!el) return;
        key = String(key);
        el.setAttribute('data-nak-key', key);
        if (!el.querySelector('.nak-aa-toggle')) {
            var t = document.createElement('span');
            t.className = 'nak-aa-toggle';
            t.addEventListener('click', function (e) {
                e.stopPropagation();
                e.preventDefault();
                var want = !eligible(key);
                if (want) nakAutoAcceptCats.add(key); else nakAutoAcceptCats.delete(key);
                paintCategory(el, key);
                if (window.engine) engine.call('NAKSetAutoAcceptCategory', key, want);
            });
            el.appendChild(t);
        }
        paintCategory(el, key);
    }

    function keyFromClass(cn) {
        var parts = (cn || '').split(/\s+/);
        for (var i = 0; i < parts.length; i++)
            if (parts[i].indexOf('data-filter-') === 0) return parts[i].slice(12);
        return null;
    }

    // catches categories that were already rendered before we injected
    function decorateExisting() {
        var list = window.friendCategories || [];
        var nonSystem = {};
        list.forEach(function (c) { if (!c.IsSystemCategory) nonSystem[String(c.CategoryKey)] = true; });
        var els = document.querySelectorAll('#friends .filter-option');
        for (var i = 0; i < els.length; i++) {
            var key = keyFromClass(els[i].className);
            if (key && nonSystem[key]) decorateCategory(els[i], key);
        }
    }

    function repaintAll() {
        var els = document.querySelectorAll('#friends .filter-option[data-nak-key]');
        for (var i = 0; i < els.length; i++)
            paintCategory(els[i], els[i].getAttribute('data-nak-key'));
    }

    // wrap the game's builder so our toggle is re-added on every re-render
    function wrapCreateRenderCategory() {
        if (typeof window.CreateRenderCategory !== 'function' || window.CreateRenderCategory.__nakWrapped) return;
        var orig = window.CreateRenderCategory;
        var wrapped = function (categoryName, categoryInfo) {
            var el = orig.apply(this, arguments);
            try {
                if (categoryName === 'friends' && categoryInfo && !categoryInfo.IsSystemCategory)
                    decorateCategory(el, categoryInfo.CategoryKey);
            } catch (err) { console.error('[NAK] decorate failed', err); }
            return el;
        };
        wrapped.__nakWrapped = true;
        window.CreateRenderCategory = wrapped;
    }

    if (window.engine) {
        engine.on('NAKInviteStatusUpdate', function (statusKey) {
            setInviteStatus(String(statusKey), false);
        });
        engine.on('NAKAutoAcceptCategoriesUpdate', function (json) {
            nakAutoAcceptCats = new Set();
            try {
                var arr = JSON.parse(json || '[]');
                for (var i = 0; i < arr.length; i++) nakAutoAcceptCats.add(String(arr[i]));
            } catch (e) { console.error('[NAK] bad categories payload', e); }
            repaintAll();
            refreshStatusBar();
        });
    }

    injectStyle();
    wrapCreateRenderCategory();
    buildStatusBar();
    decorateExisting();
    repaintAll();
    if (window.engine) engine.call('NAKRequestInviteState');
})();
";
}