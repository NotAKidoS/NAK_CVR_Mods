using System.Reflection;
using System.Runtime.CompilerServices;
using MelonLoader;

namespace NAK.Mowods;

public static class ModsMenu
{
    internal static readonly List<Page> Pages = new();

    // no inline. calling assembly must stay the mod
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Page AddPage(string name)
    {
        Page page = new(name, Assembly.GetCallingAssembly());
        Pages.Add(page);
        return page;
    }
}

public class Page : Node
{
    private readonly List<Section> _sections = new();
    private readonly Assembly _assembly;

    internal Page(string name, Assembly assembly)
    {
        Name = name;
        _assembly = assembly;
    }

    public string Name { get; }
    public IReadOnlyList<Section> Sections => _sections;

    internal MelonBase Mod => MelonBase.RegisteredMelons.FirstOrDefault(mod => mod.MelonAssembly.Assembly == _assembly);
    internal IEnumerable<Setting> Settings => _sections.SelectMany(section => section.Settings);

    public Section AddSection(string name = null, string description = null)
    {
        Section section = new(name, description);
        _sections.Add(section);
        return section;
    }
}

public class Section : Node
{
    private readonly List<Item> _items = new();

    internal Section(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<Item> Items => _items;

    internal IEnumerable<Setting> Settings => _items.OfType<Setting>();

    public T Add<T>(T item) where T : Item
    {
        _items.Add(item);
        return item;
    }

    public Setting Add(MelonPreferences_Entry entry)
        => MelonIngest.CreateSetting(entry) is { } setting ? Add(setting) : null;
}