namespace BetaSharp.Diagnostics;

/// <summary>
/// Handle to a metric stored in <see cref="MetricRegistry"/>.
/// </summary>
/// <typeparam name="T">Value type of the metric.</typeparam>
public readonly struct MetricHandle<T>
{
    internal readonly int Index;
    internal MetricHandle(int index) => Index = index;
}
