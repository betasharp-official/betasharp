using System.Runtime.CompilerServices;

namespace BetaSharp.Diagnostics;

/// <summary>
/// Central registry for diagnostic metrics. Metrics are registered once at startup via static
/// field initializers (bootstrapped with <see cref="Bootstrap"/>) and then pushed every tick or
/// frame via <see cref="Set{T}"/>. ImGui windows consume via <see cref="GetByNamespace"/>.
///
/// Thread safety: <see cref="Set{T}"/> and <see cref="Get{T}"/> are safe to call concurrently
/// for 32- and 64-bit value types on .NET (aligned reads/writes are atomic on x64). Staleness
/// tracking records wall-clock milliseconds so the threshold is frame-rate independent.
/// </summary>
public static class MetricRegistry
{
    private const int MaxMetrics = 512;

    // Separate array per T — no boxing, no dictionary on the hot path.
    private static class Storage<T>
    {
        internal static readonly T[] Values = new T[MaxMetrics];
    }

    private static readonly MetricDescriptor?[] s_all = new MetricDescriptor?[MaxMetrics];
    private static readonly Dictionary<ResourceLocation, int> s_byKey = [];
    // Wall-clock ms at last Set() call, per metric. Written by producer threads, read by render thread.
    // Plain long[] is safe for this use: 64-bit aligned writes are atomic on x64 .NET, and we
    // only need eventual consistency for a debug display.
    private static readonly long[] s_lastUpdatedMs = new long[MaxMetrics];
    private static int s_nextIndex;

    /// <summary>
    /// Registers a metric and returns a handle for hot-path access.
    /// Expected to be called only during bootstrap (single-threaded).
    /// </summary>
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

    /// <summary>Hot path — single array write, no allocation.</summary>
    public static void Set<T>(MetricHandle<T> handle, T value)
    {
        Storage<T>.Values[handle.Index] = value;
        s_lastUpdatedMs[handle.Index] = Environment.TickCount64;
    }

    /// <summary>Hot path — single array read.</summary>
    public static T Get<T>(MetricHandle<T> handle)
        => Storage<T>.Values[handle.Index];

    /// <summary>
    /// Returns true if the metric has not been updated within <paramref name="toleranceMs"/> milliseconds.
    /// Frame-rate independent — safe to use regardless of FPS cap or server tick rate.
    /// When connected to a remote server, server-namespace metrics are never pushed and become stale
    /// after the tolerance window.
    /// </summary>
    public static bool IsStale(int index, double toleranceMs = 2000.0)
        => (Environment.TickCount64 - s_lastUpdatedMs[index]) > toleranceMs;

    public static bool IsStale<T>(MetricHandle<T> handle, double toleranceMs = 2000.0)
        => IsStale(handle.Index, toleranceMs);

    /// <summary>
    /// Enumerates all metrics registered under <paramref name="namespace"/>.
    /// Used by ImGui windows to display metrics without holding specific handles.
    /// </summary>
    public static IEnumerable<MetricDescriptor> GetByNamespace(string @namespace)
    {
        int count = s_nextIndex;
        for (int i = 0; i < count; i++)
        {
            var d = s_all[i];
            if (d?.Key.Namespace == @namespace)
                yield return d;
        }
    }

    /// <summary>
    /// Forces the static constructor of <paramref name="provider"/> to run, registering all
    /// metrics declared as static fields in that class. Same pattern as
    /// <see cref="Registries.RegistryExtensions.Bootstrap{T}"/>.
    /// </summary>
    public static void Bootstrap(Type provider)
        => RuntimeHelpers.RunClassConstructor(provider.TypeHandle);
}
