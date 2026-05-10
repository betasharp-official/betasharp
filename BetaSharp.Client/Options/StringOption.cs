namespace BetaSharp.Client.Options;

public class StringOption : GameOption
{
    public string Value { get; set; }
    public Func<string, TranslationStorage, string>? Formatter { get; init; }
    public Action<string>? OnChanged { get; init; }

    public StringOption(string translationKey, string saveKey, string defaultValue = "") : base(translationKey, saveKey)
    {
        Value = defaultValue;
    }

    public override string FormatValue(TranslationStorage translations)
    {
        return Value;
    }

    public override void Load(string raw) => Value = raw;

    public override string Save() => Value.ToString().ToLower();
}
