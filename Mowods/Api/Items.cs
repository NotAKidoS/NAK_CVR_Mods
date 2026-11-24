using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace NAK.Mowods;

public abstract class Node
{
    internal int Id = -1;
}

public abstract class Item : Node
{
    protected Item(string name, string tooltip)
    {
        Name = name;
        Tooltip = tooltip;
    }

    public string Name { get; }
    public string Tooltip { get; }

    internal abstract string Kind { get; }

    internal virtual void Write(JObject json) { }

    internal bool Matches(string query) => Contains(Name, query) || Contains(Tooltip, query);

    internal static bool Contains(string text, string query)
        => text != null && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
}

public abstract class Setting : Item
{
    protected Setting(string name, Binding binding, string tooltip) : base(name, tooltip)
    {
        Binding = binding;
        binding.Changed += () => Menu.Push(this);
    }

    public Binding Binding { get; }

    internal virtual JToken ToJson(object value) => JToken.FromObject(value);
    internal virtual object FromJson(JToken token) => token.ToObject(Binding.Type);
}

public class Toggle : Setting
{
    public Toggle(string name, Binding binding, string tooltip = null) : base(name, binding, tooltip) { }

    internal override string Kind => "toggle";
}

public class TextField : Setting
{
    public TextField(string name, Binding binding, string tooltip = null) : base(name, binding, tooltip) { }

    internal override string Kind => "text";
}

public class Number : Setting
{
    public Number(string name, Binding binding, string tooltip = null) : base(name, binding, tooltip) { }

    public Number(string name, Binding binding, double min, double max, double? step = null, string tooltip = null)
        : base(name, binding, tooltip)
    {
        Min = min;
        Max = max;
        // about 100 steps over range
        Step = step ?? (IsInteger ? 1 : Math.Pow(10, Math.Floor(Math.Log10((max - min) / 100))));
    }

    public double? Min { get; }
    public double? Max { get; }
    public double? Step { get; }

    public bool IsInteger => Type.GetTypeCode(Binding.Type) < TypeCode.Single;

    internal override string Kind => Min.HasValue ? "slider" : "number";

    internal override void Write(JObject json)
    {
        json["integer"] = IsInteger;
        json["min"] = Min;
        json["max"] = Max;
        json["step"] = Step;
    }

    internal static bool IsNumeric(Type type) => Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal;
}

public class Choice : Setting
{
    public Choice(string name, Binding binding, IReadOnlyList<(object Value, string Label)> options = null, string tooltip = null)
        : base(name, binding, tooltip)
        => Options = options ?? EnumOptions(binding.Type);

    public IReadOnlyList<(object Value, string Label)> Options { get; }

    internal override string Kind => "choice";

    internal override void Write(JObject json)
    {
        json["options"] = new JArray(Options.Select(option => option.Label));
        // few short options fit as buttons. rest is dropdown
        json["buttons"] = Options.Count <= 4 && Options.Sum(option => option.Label.Length) <= 20;
    }

    // js only knows option index
    internal override JToken ToJson(object value)
    {
        for (int i = 0; i < Options.Count; i++)
            if (Equals(Options[i].Value, value)) return i;
        return -1;
    }

    internal override object FromJson(JToken token) => Options[(int)token].Value;

    public static IReadOnlyList<(object Value, string Label)> EnumOptions(Type type)
        => Enum.GetValues(type).Cast<object>().Distinct().Select(value => (value, Label(value.ToString()))).ToList();

    // CamelCase -> Camel Case
    // TODO: 3D becomes 3 D which is cringe
    internal static string Label(string name) => Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
}

public class Button : Item
{
    public Button(string name, string label, Action onClick, string tooltip = null) : base(name, tooltip)
    {
        Label = label;
        OnClick = onClick;
    }

    public string Label { get; }
    internal Action OnClick { get; }

    internal override string Kind => "button";

    internal override void Write(JObject json) => json["label"] = Label;
}