using MelonLoader;
using MelonLoader.Preferences;

namespace NAK.Mowods;

internal static class MelonIngest
{
    internal static IEnumerable<Page> BuildPages()
    {
        // Build lost of custom pages using mod API
        HashSet<MelonPreferences_Category> claimed = ModsMenu.Pages
            .SelectMany(page => page.Settings)
            .Select(setting => setting.Binding.Entry?.Category)
            .Where(category => category != null)
            .ToHashSet();

        // Build list of pages from melonprefs (skipping mods which make a custom page)
        Dictionary<string, Page> pages = new();
        foreach (MelonPreferences_Category category in MelonPreferences.Categories)
        {
            if (category.IsHidden || claimed.Contains(category)) continue;

            List<Setting> settings = category.Entries
                .Where(entry => !entry.IsHidden)
                .Select(CreateSetting)
                .Where(setting => setting != null)
                .ToList();
            if (settings.Count == 0) continue;

            MelonBase mod = OwnerOf(category);
            string owner = mod?.Info.Name ?? NameOf(category);
            if (!pages.TryGetValue(owner, out Page page)) pages[owner] = page = new Page(owner, mod?.MelonAssembly.Assembly);

            Section section = page.AddSection(SectionNameOf(category, owner));
            foreach (Setting setting in settings) section.Add(setting);
        }
        return pages.Values;
    }

    internal static Setting CreateSetting(MelonPreferences_Entry entry)
    {
        string name = entry.DisplayName ?? entry.Identifier;
        string tooltip = entry.Description ?? entry.Comment;
        Type type = entry.GetReflectedType();

        if (type == typeof(bool)) return new Toggle(name, Binding.Of(entry), tooltip);
        if (type == typeof(string)) return new TextField(name, Binding.Of(entry), tooltip);
        if (type.IsEnum) return new Choice(name, Binding.Of(entry), tooltip: tooltip);

        if (Number.IsNumeric(type))
        {
            if (entry.Validator is IValueRange range)
            {
                double min = Convert.ToDouble(range.MinValue);
                double max = Convert.ToDouble(range.MaxValue);
                return new Number(name, Binding.Of(entry), min, max, tooltip: tooltip);
            }
            return new Number(name, Binding.Of(entry), tooltip);
        }

        // TODO: Control for Lists / Vectors ?
        
        return null;
    }
    
    // Combine categories which all start with the same mod name.
    // Ex: "Mod", "Mod_Debug", "ModAdvanced"
    private static MelonBase OwnerOf(MelonPreferences_Category category)
    {
        string key = Normalize(category.Identifier);
        return MelonBase.RegisteredMelons
            .Where(mod => Normalize(mod.Info.Name) is { Length: > 2 } prefix && key.StartsWith(prefix))
            .OrderByDescending(mod => mod.Info.Name.Length)
            .FirstOrDefault();
    }

    private static string NameOf(MelonPreferences_Category category) => category.DisplayName ?? category.Identifier;

    // Determine subcategory name after combining categories into one page.
    // Ex: "Mod_Advanced" -> "Advanced", and anything without a prefix is "General".
    private static string SectionNameOf(MelonPreferences_Category category, string owner)
    {
        string name = NameOf(category);
        if (!name.StartsWith(owner, StringComparison.OrdinalIgnoreCase)) return name;

        string rest = name.Substring(owner.Length).TrimStart('_', '-', '.', ':', ' ');
        return rest.Length == 0 ? "General" : Choice.Label(rest);
    }

    private static string Normalize(string name) => new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}