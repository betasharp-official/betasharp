using System.Globalization;
using System.Text;
using System.Text.Json;
using BetaSharp.Diagnostics;
using BetaSharp.Profiling;

namespace BetaSharp.Server;

public sealed record ServerDiagnosticsSnapshot(
    DateTimeOffset Timestamp,
    float Tps,
    float Mspt,
    int PlayerCount,
    int EntityCount,
    int OverworldEntityCount,
    int NetherEntityCount,
    int BlockEntityCount,
    int PendingConnections,
    int ActiveConnections,
    long BytesRead,
    long BytesWritten,
    long PacketsRead,
    long PacketsWritten,
    int PendingChunkLoads,
    int TrackedChunks,
    int DirtyTrackedChunks,
    int PendingChunkSends,
    int MaxPendingChunkSends,
    int LightingQueue,
    int ScheduledBlockTicks,
    int OverworldTrackedEntities,
    int NetherTrackedEntities,
    long WorkingSetBytes,
    long HeapBytes);

public static class ServerDiagnosticsFormatter
{
    public static string FormatSummary(ServerDiagnosticsSnapshot snapshot)
    {
        return string.Join(Environment.NewLine,
        [
            $"Time:             {snapshot.Timestamp:O}",
            $"TPS:              {snapshot.Tps:F2}",
            $"MSPT:             {snapshot.Mspt:F2}",
            $"Players:          {snapshot.PlayerCount}",
            $"Entities:         {snapshot.EntityCount} (overworld {snapshot.OverworldEntityCount}, nether {snapshot.NetherEntityCount})",
            $"Block entities:   {snapshot.BlockEntityCount}",
            $"Connections:      {snapshot.ActiveConnections} active, {snapshot.PendingConnections} pending",
            $"Network:          {snapshot.BytesRead}B in / {snapshot.BytesWritten}B out, {snapshot.PacketsRead} pkts in / {snapshot.PacketsWritten} pkts out",
            $"Chunk queues:     {snapshot.PendingChunkLoads} loads pending, {snapshot.PendingChunkSends} sends pending, {snapshot.MaxPendingChunkSends} max/player",
            $"Tracked chunks:   {snapshot.TrackedChunks} tracked, {snapshot.DirtyTrackedChunks} dirty",
            $"Lighting queue:   {snapshot.LightingQueue}",
            $"Scheduled ticks:  {snapshot.ScheduledBlockTicks}",
            $"Tracked entities: {snapshot.OverworldTrackedEntities} overworld, {snapshot.NetherTrackedEntities} nether",
            $"Memory:           {snapshot.WorkingSetBytes}B working set, {snapshot.HeapBytes}B heap",
        ]);
    }

    public static string FormatJson(ServerDiagnosticsSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }

    public static string FormatMetrics()
    {
        var builder = new StringBuilder();

        foreach (MetricDescriptor metric in MetricRegistry.GetAll())
        {
            if (!TryFormatMetricValue(metric, out string? value))
            {
                continue;
            }

            builder.Append("betasharp_");
            builder.Append(SanitizeMetricName(metric.Key.ToString()));
            builder.Append(' ');
            builder.AppendLine(value);
        }

        return builder.ToString();
    }

    public static string FormatProfiler()
    {
        var builder = new StringBuilder();

        foreach (var stat in Profiler.GetStats())
        {
            builder.Append(stat.Name);
            builder.Append(" last=");
            builder.Append(stat.Last.ToString("F3", CultureInfo.InvariantCulture));
            builder.Append("ms avg=");
            builder.Append(stat.Avg.ToString("F3", CultureInfo.InvariantCulture));
            builder.Append("ms max=");
            builder.Append(stat.Max.ToString("F3", CultureInfo.InvariantCulture));
            builder.AppendLine("ms");
        }

        return builder.ToString();
    }

    private static string SanitizeMetricName(string key)
    {
        return key.Replace(':', '_').Replace('.', '_').Replace('-', '_');
    }

    private static bool TryFormatMetricValue(MetricDescriptor metric, out string? value)
    {
        Type valueType = metric.ValueType;
        object? rawValue = metric.RawValue;

        if (valueType == typeof(bool))
        {
            value = rawValue is true ? "1" : "0";
            return true;
        }

        if (valueType == typeof(byte) ||
            valueType == typeof(short) ||
            valueType == typeof(int) ||
            valueType == typeof(long) ||
            valueType == typeof(float) ||
            valueType == typeof(double) ||
            valueType == typeof(decimal))
        {
            value = Convert.ToString(rawValue, CultureInfo.InvariantCulture);
            return value != null;
        }

        value = null;
        return false;
    }
}
