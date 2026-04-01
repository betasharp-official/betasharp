namespace BetaSharp.Diagnostics;

public sealed class MetricDescriptor
{
    public required ResourceLocation Key { get; init; }
    public required Type ValueType { get; init; }
    public required int Index { get; init; }
    internal Func<string> ValueString { get; init; } = null!;

    public string Value => ValueString();
}
