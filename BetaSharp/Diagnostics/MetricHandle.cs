namespace BetaSharp.Diagnostics;

public readonly struct MetricHandle<T>
{
    internal readonly int Index;
    internal MetricHandle(int index) => Index = index;
}
