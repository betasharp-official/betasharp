using System.Runtime.CompilerServices;

namespace BetaSharp.Diagnostics;

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

    public static void Set<T>(MetricHandle<T> handle, T value)
    {
        Storage<T>.Values[handle.Index] = value;
        s_lastUpdatedMs[handle.Index] = Environment.TickCount64;
    }

    public static T Get<T>(MetricHandle<T> handle)
        => Storage<T>.Values[handle.Index];

    public static bool IsStale(int index, double toleranceMs = 2000.0)
        => (Environment.TickCount64 - s_lastUpdatedMs[index]) > toleranceMs;

    public static bool IsStale<T>(MetricHandle<T> handle, double toleranceMs = 2000.0)
        => IsStale(handle.Index, toleranceMs);

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

    public static void Bootstrap(Type provider)
        => RuntimeHelpers.RunClassConstructor(provider.TypeHandle);
}
