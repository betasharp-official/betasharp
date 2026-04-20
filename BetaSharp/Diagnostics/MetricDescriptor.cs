namespace BetaSharp.Diagnostics;

/// <summary>
/// Descriptor for a metric.
/// </summary>
public sealed class MetricDescriptor
{
    public required ResourceLocation Key { get; init; }

    /// <summary>
    /// Type this metric's value should be.
    /// </summary>
    public required Type ValueType { get; init; }

    public required int Index { get; init; }
    internal Func<string> ValueString { get; init; } = null!;

    /// <summary>
    /// Gets the string representation of this metric's value.
    /// </summary>
    public string Value => ValueString();
}
