namespace BetaSharp.Client.Options;

public class CycleOption : GameOption
{
    public int Value { get; set; }
    public int Length { get; }
    public string[] Labels { get; }
    public Func<int, TranslationStorage, string>? Formatter { get; init; }
    public Action<int>? OnChanged { get; init; }

    public CycleOption(string translationKey, string saveKey, string[] labels, int defaultValue = 0) : this(translationKey, saveKey, labels, defaultValue, labels.Length - 1) { }

    public CycleOption(string translationKey, string saveKey, string[] labels, int defaultValue, int length) : base(translationKey, saveKey)
    {
        Labels = labels;
        Value = defaultValue;
        Length = length;
    }

    public void Cycle(int increment = 1)
    {
        Value = (Value + increment) % Length;
        OnChanged?.Invoke(Value);
    }

    public override string FormatValue(TranslationStorage translations)
    {
        if (Formatter != null)
        {
            return Formatter(Value, translations);
        }

        if (Labels.Length < Length)
            return Labels.Length < Value ? translations.TranslateKeyFormat(Labels[Value]) : translations.TranslateKeyFormat(Labels.Last());
        return Labels.Length < Value ? translations.TranslateKey(Labels[Value]) : translations.TranslateKey(Labels.Last());
    }

    public override void Load(string raw) => Value = int.Parse(raw);

    public override string Save() => Value.ToString();
}
