using MelonLoader;

namespace NAK.Mowods;

public class Binding
{
    private readonly Func<object> _get;
    private readonly Action<object> _set;

    private Binding(Type type, object defaultValue, Func<object> get, Action<object> set)
    {
        Type = type;
        Default = defaultValue;
        _get = get;
        _set = set;
    }

    public Type Type { get; }
    public object Default { get; }
    internal MelonPreferences_Entry Entry { get; private set; }

    public event Action Changed;

    public object Value
    {
        get => _get();
        set => _set(value);
    }

    public bool IsDefault => Equals(Value, Default);

    public void Reset() => Value = Default;
    public void NotifyChanged() => Changed?.Invoke();

    public static Binding Of<T>(Func<T> get, Action<T> set, T defaultValue)
    {
        Binding binding = null;
        binding = new Binding(typeof(T), defaultValue, () => get(), value =>
        {
            set((T)value);
            binding.NotifyChanged();
        });
        return binding;
    }

    public static Binding Of(MelonPreferences_Entry entry)
    {
        // DefaultValue only on generic entry
        object defaultValue = entry.GetType().GetProperty("DefaultValue")?.GetValue(entry);

        Binding binding = new(entry.GetReflectedType(), defaultValue, () => entry.BoxedValue, value =>
        {
            entry.BoxedValue = value;
            entry.Category.SaveToFile(false);
        });
        binding.Entry = entry;

        entry.OnEntryValueChangedUntyped.Subscribe((_, _) => binding.NotifyChanged());
        return binding;
    }
}