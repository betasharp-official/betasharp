using System.Collections.Concurrent;

namespace BetaSharp.Rules;

public sealed class RuleRegistry
{
    public static RuleRegistry Instance { get; } = CreateDefault();

    private readonly ConcurrentDictionary<ResourceLocation, IGameRule> _rules = new();

    private static RuleRegistry CreateDefault()
    {
        RuleRegistry registry = new();
        DefaultRules.Instance.RegisterAll(registry);
        return registry;
    }

    public IGameRule<T> Register<T>(IGameRule<T> rule) where T : IRuleValue
    {
        if (!_rules.TryAdd(rule.Key, rule))
            throw new InvalidOperationException($"Rule '{rule.Key}' is already registered.");
        return rule;
    }

    public RuleRegistrar For(Namespace @namespace) => new(this, @namespace);

    public bool TryGet(ResourceLocation key, out IGameRule rule) =>
        _rules.TryGetValue(key, out rule!);

    public IGameRule Get(ResourceLocation key) =>
        _rules.TryGetValue(key, out IGameRule? r) ? r
            : throw new KeyNotFoundException($"No rule registered for key '{key}'.");

    public IEnumerable<IGameRule> All => _rules.Values;

    public IEnumerable<IGameRule> ByCategory(string category) =>
        _rules.Values.Where(r => r.Category == category);

    public IEnumerable<string> Categories =>
        _rules.Values.Select(r => r.Category).Distinct().Order();
}

public sealed class RuleRegistrar(RuleRegistry registry, Namespace ns)
{
    public BoolRule Bool(string name, bool defaultValue,
        string category = "general", string description = "") =>
        (BoolRule)registry.Register(new BoolRule(new ResourceLocation(ns, name), defaultValue, category, description));

    public IntRule Int(string name, int defaultValue, int min = int.MinValue, int max = int.MaxValue,
        string category = "general", string description = "") =>
        (IntRule)registry.Register(new IntRule(new ResourceLocation(ns, name), defaultValue, min, max, category, description));

    public FloatRule Float(string name, float defaultValue, float min = float.MinValue, float max = float.MaxValue,
        string category = "general", string description = "") =>
        (FloatRule)registry.Register(new FloatRule(new ResourceLocation(ns, name), defaultValue, min, max, category, description));

    public StringRule String(string name, string defaultValue,
        string category = "general", string description = "") =>
        (StringRule)registry.Register(new StringRule(new ResourceLocation(ns, name), defaultValue, category, description));

    public EnumRule<T> Enum<T>(string name, T defaultValue,
        string category = "general", string description = "") where T : struct, global::System.Enum =>
        (EnumRule<T>)registry.Register(new EnumRule<T>(new ResourceLocation(ns, name), defaultValue, category, description));
}
