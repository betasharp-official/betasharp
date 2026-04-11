using System.Runtime.CompilerServices;

namespace BetaSharp.Diagnostics;

/// <summary>
/// Registry for metrics.
/// </summary>
public static class MetricRegistry
{
    private const int MaxMetrics = 512;

    private static class Storage<T>
    {
        internal static readonly T[] Values = new T[MaxMetrics];
    }

    private static readonly MetricDescriptor?[] s_all = new MetricDescriptor?[MaxMetrics];
    private static readonly Dictionary<ResourceLocation, int> s_byKey = [];
    private static readonly long[] s_lastUpdatedMs = new long[MaxMetrics];
    private static int s_nextIndex;

    /// <summary>
    /// Register a metric with value type T with location key.
    /// </summary>
    /// <returns>Handle to the metric.</returns>
    public static MetricHandle<T> Register<T>(ResourceLocation key)
    {
        int index = s_nextIndex++;
        var descriptor = new MetricDescriptor
        {
            Key = key,
            ValueType = typeof(T),
            Index = index,
            ValueString = () => Storage<T>.Values[index]?.ToString() ?? string.Empty,
        };
        s_all[index] = descriptor;
        s_byKey[key] = index;
        return new MetricHandle<T>(index);
    }

    /// <summary>
    /// Set the value of a MetricHandle.
    /// </summary>
    /// <typeparam name="T">Type of value to set</typeparam>
    public static void Set<T>(MetricHandle<T> handle, T value)
    {
        Storage<T>.Values[handle.Index] = value;
        s_lastUpdatedMs[handle.Index] = Environment.TickCount64;
    }

    /// <summary>
    /// Get the value of a MetricHandle.
    /// </summary>
    /// <typeparam name="T">Type of value to get</typeparam>
    public static T Get<T>(MetricHandle<T> handle)
        => Storage<T>.Values[handle.Index];

    /// <summary>
    /// Check if a metric, by its index, is stale.
    /// </summary>
    /// <param name="toleranceMs">How long can it stay while being considered stale.</param>
    public static bool IsStale(int index, double toleranceMs = 2000.0)
        => (Environment.TickCount64 - s_lastUpdatedMs[index]) > toleranceMs;

    /// <summary>
    /// Check if a metric, by its handle, is stale.
    /// </summary>
    /// <typeparam name="T">Type of metric to check</typeparam>
    public static bool IsStale<T>(MetricHandle<T> handle, double toleranceMs = 2000.0)
        => IsStale(handle.Index, toleranceMs);

    /// <summary>
    /// Gets all <see cref="MetricDescriptor"/> by namespace.
    /// </summary>
    public static IEnumerable<MetricDescriptor> GetByNamespace(string @namespace)
    {
        int count = s_nextIndex;
        for (int i = 0; i < count; i++)
        {
            MetricDescriptor? d = s_all[i];
            if (d?.Key.Namespace == @namespace)
                yield return d;
        }
    }

    /// <summary>
    /// Bootstrap the metric registry.
    /// </summary>
    public static void Bootstrap(Type provider)
        => RuntimeHelpers.RunClassConstructor(provider.TypeHandle);
}
