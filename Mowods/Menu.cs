using System.Reflection;
using System.Text;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using MelonLoader;
using MelonLoader.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NAK.Mowods;

internal static class Menu
{
    private static readonly List<Node> Nodes = new();
    private static List<Page> _pages;
    private static CohtmlControlledView _view;

    #region Lifecycle

    internal static void Attach()
    {
        CVRGameEventSystem.MainMenu.OnOpen.RemoveListener(Attach);
        _view = ViewManager.Instance.cohtmlView;
        _view.Listener.ReadyForBindings += Bind;
        _view.OnFinishedLoading += Inject;
        Bind();
        Inject();
    }

    private static void Bind()
    {
        CohtmlControlledViewWrapper view = _view.View;
        view.BindCall("MowodsPages", (Func<string>)GetPages);
        view.BindCall("MowodsPage", (Func<int, string>)GetPage);
        view.BindCall("MowodsSearch", (Func<string, string>)Search);
        view.BindCall("MowodsSet", (Action<int, string>)Set);
        view.BindCall("MowodsReset", (Action<int>)Reset);
        view.BindCall("MowodsClick", (Action<int>)Click);
        view.BindCall("MowodsOpenLink", (Action<int>)OpenLink);
    }

    private static void Inject()
    {
        string icon = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(ReadResource("melon.svg")));
        string script = $"{ReadResource("Menu.js")}({JsonConvert.ToString(ReadResource("Menu.css"))}, {JsonConvert.ToString(icon)});";
        _view.View.InternalView.ExecuteScript(script);
    }

    internal static void Push(Setting setting)
    {
        if (setting.Id < 0 || _view is not { FinishedLoading: true }) return;

        string value = JsonOf(setting, setting.Binding.Value).ToString(Formatting.None);
        _view.View.TriggerEvent("MowodsValue", setting.Id, value, setting.Binding.IsDefault);
    }

    #endregion Lifecycle

    #region Pages

    private static List<Page> Pages => _pages ??= BuildPages();

    private static List<Page> BuildPages()
    {
        List<Page> pages = ModsMenu.Pages
            .Concat(MelonIngest.BuildPages())
            .OrderBy(page => page.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (Page page in pages)
        {
            Register(page);
            foreach (Section section in page.Sections)
            {
                Register(section);
                foreach (Item item in section.Items) Register(item);
            }
        }
        return pages;
    }

    private static void Register(Node node)
    {
        node.Id = Nodes.Count;
        Nodes.Add(node);
    }

    private static T Find<T>(int id) where T : Node
    {
        if (id < 0 || id >= Nodes.Count) return null;
        return Nodes[id] as T;
    }

    #endregion Pages

    #region Calls

    private static string GetPages()
    {
        JArray pages = new(Pages.Select(page => new JObject
        {
            ["id"] = page.Id,
            ["name"] = page.Name,
            ["colors"] = ColorsOf(page)
        }));
        return pages.ToString(Formatting.None);
    }

    private static string GetPage(int id)
    {
        Page page = Find<Page>(id);
        MelonBase mod = page.Mod;
        JObject json = new()
        {
            ["id"] = page.Id,
            ["name"] = page.Name,
            ["info"] = InfoOf(mod),
            ["colors"] = ColorsOf(page),
            ["hasLink"] = LinkOf(mod) != null,
            ["sections"] = new JArray(page.Sections.Select(section => SectionJson(section, section.Name, section.Items)))
        };
        return json.ToString(Formatting.None);
    }

    private static string Search(string query)
    {
        JArray sections = new();
        JObject counts = new();
        foreach (Page page in Pages)
        {
            int count = 0;
            foreach (Section section in page.Sections)
            {
                bool sectionMatches = Item.Contains(page.Name, query) || Item.Contains(section.Name, query);
                List<Item> items = section.Items.Where(item => sectionMatches || item.Matches(query)).ToList();
                if (items.Count == 0) continue;

                count += items.Count;
                string name = section.Name == null || section.Name == page.Name ? page.Name : $"{page.Name} / {section.Name}";
                sections.Add(SectionJson(section, name, items));
            }
            counts[page.Id.ToString()] = count;
        }

        JObject json = new() { ["sections"] = sections, ["counts"] = counts };
        return json.ToString(Formatting.None);
    }

    private static void Set(int id, string json)
    {
        Setting setting = Find<Setting>(id);
        Change(setting, () => setting.Binding.Value = setting.FromJson(JToken.Parse(json)));
    }

    private static void Reset(int id)
    {
        IEnumerable<Setting> settings = Find<Node>(id) switch
        {
            Page page => page.Settings,
            Section section => section.Settings,
            Setting setting => new[] { setting },
            _ => Enumerable.Empty<Setting>()
        };

        foreach (Setting setting in settings) Change(setting, setting.Binding.Reset);
    }

    private static void Change(Setting setting, Action change)
    {
        try
        {
            change();
        }
        catch (Exception e)
        {
            MowodsMod.Logger.Warning($"Could not change {setting.Name}: {e.Message}");
        }
        Push(setting);
    }

    private static void Click(int id)
    {
        try
        {
            Find<Button>(id).OnClick?.Invoke();
        }
        catch (Exception e)
        {
            MowodsMod.Logger.Error(e);
        }
    }

    private static void OpenLink(int id)
    {
        string link = LinkOf(Find<Page>(id).Mod);
        if (link != null) Application.OpenURL(link);
    }

    #endregion Calls

    #region Json

    private static JObject SectionJson(Section section, string name, IEnumerable<Item> items) => new()
    {
        ["id"] = section.Id,
        ["name"] = name,
        ["description"] = section.Description,
        ["items"] = new JArray(items.Select(ItemJson))
    };

    private static JObject ItemJson(Item item)
    {
        JObject json = new()
        {
            ["id"] = item.Id,
            ["kind"] = item.Kind,
            ["name"] = item.Name,
            ["tooltip"] = item.Tooltip
        };
        item.Write(json);

        if (item is Setting setting)
        {
            json["value"] = JsonOf(setting, setting.Binding.Value);
            json["default"] = JsonOf(setting, setting.Binding.Default);
            json["isDefault"] = setting.Binding.IsDefault;
        }
        return json;
    }

    private static JToken JsonOf(Setting setting, object value) => value == null ? JValue.CreateNull() : setting.ToJson(value);

    private static string InfoOf(MelonBase mod)
    {
        if (mod == null) return null;

        List<string> parts = new() { Path.GetFileName(mod.MelonAssembly.Location) };
        if (!string.IsNullOrEmpty(mod.Info.Version)) parts.Add("v" + mod.Info.Version);
        if (!string.IsNullOrEmpty(mod.Info.Author)) parts.Add("by " + mod.Info.Author);
        return string.Join("  ·  ", parts);
    }

    // mod half + author half. no melon color = hash of name
    private static JArray ColorsOf(Page page)
    {
        MelonBase mod = page.Mod;
        if (mod == null) return new JArray(HashColor(page.Name));

        Assembly assembly = mod.MelonAssembly.Assembly;
        MelonColorAttribute modColor = assembly.GetCustomAttribute<MelonColorAttribute>();
        MelonAuthorColorAttribute authorColor = assembly.GetCustomAttribute<MelonAuthorColorAttribute>();

        JArray colors = new(modColor != null ? Hex(modColor.DrawingColor) : HashColor(mod.Info.Name));
        if (authorColor != null) colors.Add(Hex(authorColor.DrawingColor));
        else if (!string.IsNullOrEmpty(mod.Info.Author)) colors.Add(HashColor(mod.Info.Author));
        return colors;
    }

    private static string Hex(ColorARGB color) => $"#{color.R:x2}{color.G:x2}{color.B:x2}";

    // fnv-1a as string.GetHashCode isn't deterministic
    private static string HashColor(string name)
    {
        uint hash = name.Aggregate(2166136261u, (h, c) => unchecked((h ^ c) * 16777619u));
        return "#" + ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(hash % 360 / 360f, 0.55f, 0.9f));
    }

    // only respect real urls (anything else is garbage)
    private static string LinkOf(MelonBase mod)
    {
        if (!Uri.TryCreate(mod?.Info.DownloadLink, UriKind.Absolute, out Uri uri)) return null;
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : null;
    }

    #endregion Json

    private static string ReadResource(string name)
    {
        using StreamReader reader = new(typeof(Menu).Assembly.GetManifestResourceStream(name)!);
        return reader.ReadToEnd();
    }
}