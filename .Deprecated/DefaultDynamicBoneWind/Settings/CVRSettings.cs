using ABI_RC.Core.IO;
using ABI_RC.Core.Logger;
using Tomlet;
using Tomlet.Models;

namespace ABI_RC.Core.Savior
{
    /// <summary>
    /// A named runtime context. The game sets Active at runtime.
    /// Settings with matching variant names update automatically.
    /// </summary>
    public sealed class CVRSettingsContext
    {
        public readonly string Name;

        private string _active;

        public event Action<string, string> OnChanged;

        public CVRSettingsContext(string name) => Name = name;

        public string Active
        {
            get => _active;
            set
            {
                if (_active == value) return;
                var old = _active;
                _active = value;
                OnChanged?.Invoke(old, value);
            }
        }
    }

    /// <summary>
    /// A .toml file on disk. Contains categories, each a [section].
    /// Variant sub-tables like [Category.VR] are stored within category tables.
    /// </summary>
    public sealed class CVRSettingsFile
    {
        public readonly string Name;

        internal readonly Dictionary<string, CVRSettingsCategory> Categories = new();
        internal readonly List<CVRSettingsContext> Contexts = new();
        internal readonly HashSet<CVRSettingBase> VariantSettings = new();

        private string _path;
        private TomlDocument _cached;
        private bool _failed;
        private bool _threwOnce;
        private bool _savePending;

        public bool HasFailed => _failed;

        public CVRSettingsFile(string name)
        {
            Name = name;
            CVRSettingsRegistry.Register(this);
        }

        public void SetPath(string filePath) => _path = filePath;

        public CVRSettingsCategory CreateCategory(string name, string comment = null, string page = null)
        {
            var cat = new CVRSettingsCategory(name, comment, page, this);
            Categories[name] = cat;
            return cat;
        }

        /// <summary>
        /// Register a context. When Active changes, only settings with variants are notified.
        /// </summary>
        public void RegisterContext(CVRSettingsContext context)
        {
            if (Contexts.Contains(context)) return;
            Contexts.Add(context);
            context.OnChanged += (_, _) =>
            {
                foreach (var setting in VariantSettings)
                    setting.OnContextChanged();
            };
        }

        public void Load()
        {
            if (_path == null || !File.Exists(_path)) return;

            try
            {
                var content = File.ReadAllText(_path);
                if (string.IsNullOrWhiteSpace(content)) return;

                _cached = new TomlParser().Parse(content);

                foreach (var cat in Categories.Values)
                    cat.PopulateFromDocument(_cached);
            }
            catch (Exception e)
            {
                _failed = true;
                CVRLogger.LogError($"[CVRSettings] Failed to load '{_path}': {e}. " +
                                   "Settings will not be saved this session.");
            }
        }

        public void Save()
        {
            if (_path == null || _failed) return;

            var doc = TomlDocument.CreateEmpty();

            foreach (var cat in Categories.Values)
            {
                var table = new TomlTable();
                if (cat.Comment != null)
                    table.Comments.PrecedingComment = cat.Comment;

                foreach (var setting in cat.Settings.Values)
                {
                    if (setting.SkipWrite) continue;
                    var val = setting.ToTomlValue();
                    if (val == null) continue;
                    if (setting.Comment != null)
                        val.Comments.PrecedingComment = setting.Comment;
                    table.PutValue(setting.Name, val);
                }

                var variantNames = new HashSet<string>();
                foreach (var setting in cat.Settings.Values)
                    setting.CollectVariantNames(variantNames);

                foreach (var variantName in variantNames)
                {
                    var variantTable = new TomlTable { ForceNoInline = true };
                    variantTable.Comments.PrecedingComment = $"Active when context is: {variantName}";

                    foreach (var setting in cat.Settings.Values)
                    {
                        var val = setting.GetVariantToml(variantName);
                        if (val != null)
                            variantTable.PutValue(setting.Name, val);
                    }

                    if (variantTable.Entries.Count > 0)
                        table.PutValue(variantName, variantTable);
                }

                doc.PutValue(cat.Name, table);
            }

            File.WriteAllText(_path, doc.SerializedValue);
            _cached = null;
        }

        public void SaveImmediate() => Save();

        public void ResetAll()
        {
            foreach (var cat in Categories.Values)
                cat.ResetAll();
        }

        internal void GetActiveVariants(List<string> results)
        {
            results.Clear();
            foreach (var ctx in Contexts)
                if (ctx.Active != null)
                    results.Add(ctx.Active);
        }

        internal TomlValue GetCachedValue(string category, string key)
        {
            if (_cached == null) return null;
            if (!_cached.ContainsKey(category)) return null;
            var table = _cached.GetSubTable(category);
            return table.ContainsKey(key) ? table.GetValue(key) : null;
        }

        internal TomlTable GetCachedCategoryTable(string category)
        {
            if (_cached == null) return null;
            return _cached.ContainsKey(category) ? _cached.GetSubTable(category) : null;
        }

        internal void MarkDirty()
        {
            if (_path == null || _savePending || _failed) return;
            _savePending = true;
            BetterScheduleSystem.AddJob(() => { _savePending = false; Save(); }, 5f, 0f, 0);
        }

        internal void ThrowIfFailed()
        {
            if (!_failed || _threwOnce) return;
            _threwOnce = true;
            throw new InvalidOperationException(
                $"[CVRSettings] File '{Name}' failed to load. " +
                "Subsequent access returns defaults.");
        }
    }

    /// <summary>
    /// A [section] within a .toml file.
    /// </summary>
    public sealed class CVRSettingsCategory
    {
        public readonly string Name;
        public readonly string Comment;
        public readonly string Page;

        internal readonly CVRSettingsFile File;
        internal readonly Dictionary<string, CVRSettingBase> Settings = new();

        public CVRCategoryUI UI;

        internal CVRSettingsCategory(string name, string comment, string page, CVRSettingsFile file)
        {
            Name = name;
            Comment = comment;
            Page = page ?? file.Name;
            File = file;
            CVRSettingsRegistry.Register(this);
        }

        public CVRSetting<T> Create<T>(string key, T defaultValue, string comment = null, bool writeDefault = true)
        {
            var s = new CVRSetting<T>(key, defaultValue, comment, writeDefault, this);

            var cached = File.GetCachedValue(Name, key);
            if (cached != null)
                s.LoadFromToml(cached);

            s.LoadVariantsFromCache();

            Settings[key] = s;
            return s;
        }
        
        public CVRSettingsCategory WithUI(int sortOrder = 0, UIFilters filters = null)
        {
            UI = new CVRCategoryUI { SortOrder = sortOrder, Filters = filters };
            return this;
        }

        public void ResetAll()
        {
            foreach (var s in Settings.Values)
                s.ResetToDefault();
        }

        internal void PopulateFromDocument(TomlDocument doc)
        {
            if (!doc.ContainsKey(Name)) return;
            var table = doc.GetSubTable(Name);

            foreach (var setting in Settings.Values)
            {
                if (table.ContainsKey(setting.Name))
                    setting.LoadFromToml(table.GetValue(setting.Name));
                setting.LoadVariantsFromCache();
            }
        }
    }

    /// <summary>
    /// Base class for settings.
    /// </summary>
    public abstract class CVRSettingBase
    {
        public readonly string Name;
        public readonly string Comment;
        public readonly bool WriteDefault;

        internal readonly CVRSettingsCategory Category;

        public CVRSettingUI UI;

        protected CVRSettingBase(string name, string comment, bool writeDefault, CVRSettingsCategory category)
        {
            Name = name;
            Comment = comment;
            WriteDefault = writeDefault;
            Category = category;
        }

        internal abstract bool SkipWrite { get; }
        internal abstract TomlValue ToTomlValue();
        internal abstract void LoadFromToml(TomlValue value);
        internal abstract void LoadVariantsFromCache();
        internal abstract void ResetToDefault();
        internal abstract void OnContextChanged();
        internal abstract void CollectVariantNames(HashSet<string> names);
        internal abstract TomlValue GetVariantToml(string variantName);
    }

    /// <summary>
    /// Typed setting. Supports any type Tomlet can serialize.
    /// Optionally has named variants that activate based on context state.
    /// </summary>
    public sealed class CVRSetting<T> : CVRSettingBase
    {
        public readonly T Default;

        private T _value;
        private T _lastEffective;
        private Dictionary<string, T> _variants;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly List<string> ActiveBuffer = new();

        public event Action<T, T> OnChanged;

        internal CVRSetting(string name, T defaultValue, string comment, bool writeDefault,
            CVRSettingsCategory category) : base(name, comment, writeDefault, category)
        {
            Default = defaultValue;
            _value = defaultValue;
            _lastEffective = defaultValue;
        }

        public T Value
        {
            get
            {
                Category.File.ThrowIfFailed();
                return Resolve();
            }
            set
            {
                Category.File.ThrowIfFailed();
                var activeVariant = FindActiveVariant();
                if (activeVariant != null)
                {
                    SetVariant(activeVariant, value);
                    return;
                }
                SetBase(value);
            }
        }

        public T BaseValue
        {
            get => _value;
            set => SetBase(value);
        }

        public void SetSilent(T value) => _value = value;

        public static implicit operator T(CVRSetting<T> s) => s.Value;

        public override string ToString() => Resolve()?.ToString() ?? "";

        // UI
        
        public CVRSetting<T> WithUI(int sortOrder = 0, string tooltip = null, UIFilters filters = null)
        {
            UI = new CVRSettingUI { SortOrder = sortOrder, Tooltip = tooltip, Filters = filters };
            return this;
        }
        
        // Variants

        /// <summary>
        /// Provide a code-default variant value. TOML values take precedence.
        /// Returns self for chaining.
        /// </summary>
        public CVRSetting<T> WithVariant(string variantName, T value)
        {
            _variants ??= new Dictionary<string, T>();
            _variants.TryAdd(variantName, value);
            Category.File.VariantSettings.Add(this);
            return this;
        }

        public void SetVariant(string variantName, T value)
        {
            _variants ??= new Dictionary<string, T>();
            if (_variants.TryGetValue(variantName, out var existing)
                && EqualityComparer<T>.Default.Equals(existing, value))
                return;

            var oldEffective = Resolve();
            _variants[variantName] = value;
            Category.File.VariantSettings.Add(this);
            var newEffective = Resolve();
            FireIfChanged(oldEffective, newEffective);
            Category.File.MarkDirty();
        }

        public T GetVariant(string variantName)
        {
            if (_variants != null && _variants.TryGetValue(variantName, out var val))
                return val;
            return _value;
        }

        public bool HasVariant(string variantName)
            => _variants != null && _variants.ContainsKey(variantName);

        // Resolution

        private T Resolve()
        {
            if (_variants != null)
            {
                Category.File.GetActiveVariants(ActiveBuffer);
                foreach (var name in ActiveBuffer)
                    if (_variants.TryGetValue(name, out var val))
                        return val;
            }
            return _value;
        }

        private string FindActiveVariant()
        {
            if (_variants == null) return null;
            Category.File.GetActiveVariants(ActiveBuffer);
            foreach (var name in ActiveBuffer)
                if (_variants.ContainsKey(name))
                    return name;
            return null;
        }

        private void SetBase(T value)
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            var oldEffective = Resolve();
            _value = value;
            var newEffective = Resolve();
            FireIfChanged(oldEffective, newEffective);
            Category.File.MarkDirty();
        }

        private void FireIfChanged(T oldEffective, T newEffective)
        {
            if (EqualityComparer<T>.Default.Equals(oldEffective, newEffective)) return;
            _lastEffective = newEffective;
            OnChanged?.Invoke(oldEffective, newEffective);
        }

        internal override void OnContextChanged()
        {
            var newEffective = Resolve();
            if (EqualityComparer<T>.Default.Equals(_lastEffective, newEffective)) return;
            var old = _lastEffective;
            _lastEffective = newEffective;
            OnChanged?.Invoke(old, newEffective);
        }

        // Serialization

        internal override bool SkipWrite
            => !WriteDefault && EqualityComparer<T>.Default.Equals(_value, Default);

        internal override void ResetToDefault()
        {
            var oldEffective = Resolve();
            _value = Default;
            _variants?.Clear();
            var newEffective = Resolve();
            FireIfChanged(oldEffective, newEffective);
            Category.File.MarkDirty();
        }

        internal override TomlValue ToTomlValue() => TomletMain.ValueFrom(_value);

        internal override void LoadFromToml(TomlValue val)
        {
            try { _value = TomletMain.To<T>(val); _lastEffective = _value; }
            catch { /* keep default */ }
        }

        internal override void LoadVariantsFromCache()
        {
            var categoryTable = Category.File.GetCachedCategoryTable(Category.Name);
            if (categoryTable == null) return;

            foreach (var key in categoryTable.Keys)
            {
                TomlValue entry;
                try { entry = categoryTable.GetValue(key); }
                catch { continue; }

                if (entry is not TomlTable variantTable) continue;
                if (!variantTable.ContainsKey(Name)) continue;

                try
                {
                    _variants ??= new Dictionary<string, T>();
                    _variants[key] = TomletMain.To<T>(variantTable.GetValue(Name));
                    Category.File.VariantSettings.Add(this);
                }
                catch { /* skip bad values */ }
            }
        }

        internal override void CollectVariantNames(HashSet<string> names)
        {
            if (_variants == null) return;
            foreach (var name in _variants.Keys)
                names.Add(name);
        }

        internal override TomlValue GetVariantToml(string variantName)
        {
            if (_variants == null || !_variants.TryGetValue(variantName, out var val))
                return null;
            return TomletMain.ValueFrom(val);
        }
    }

    // UI metadata shit for ui lib

    public sealed class CVRCategoryUI
    {
        public int SortOrder;
        public UIFilters Filters;
    }

    public sealed class CVRSettingUI
    {
        public int SortOrder;
        public string Tooltip;
        public UIFilters Filters;
    }

    public sealed class UIFilters
    {
        [Flags] public enum SettingsLevel  { None = 0, Basic = 1, Advanced = 2, Niche = 4 }
        [Flags] public enum PlatformType   { None = 0, PCVR = 1, PCDesktop = 2, AndroidVR = 4 }
        [Flags] public enum ContextType    { None = 0, Player = 1, Editor = 2 }
        [Flags] public enum LoaderType     { None = 0, OpenVR = 1, OpenXR = 2, MetaSDK = 4, PicoSDK = 8 }
        [Flags] public enum ControllerType { None = 0, Others = 1, Index = 2, ViveWands = 4 }
        [Flags] public enum ContentType    { None = 0, Default = 1, Mature = 2 }
        [Flags] public enum AccountRank    { None = 0, User = 1, Moderator = 2, Developer = 4 }

        // None means no filter applied, ignore
        public SettingsLevel Level = SettingsLevel.None;
        public PlatformType Platform = PlatformType.None;
        public ContextType Context = ContextType.None;
        public LoaderType Loader = LoaderType.None;
        public ControllerType Controller = ControllerType.None;
        public ContentType Content = ContentType.None;
        public AccountRank Rank = AccountRank.None;
    }
    
    public static class CVRSettingsRegistry
    {
        public static readonly Dictionary<string, CVRSettingsFile> Files = new();
        public static readonly Dictionary<string, List<CVRSettingsCategory>> Pages = new();

        internal static void Register(CVRSettingsFile file) => Files[file.Name] = file;

        internal static void Register(CVRSettingsCategory category)
        {
            if (!Pages.TryGetValue(category.Page, out var list))
            {
                list = new List<CVRSettingsCategory>();
                Pages[category.Page] = list;
            }
            list.Add(category);
        }
    }
}