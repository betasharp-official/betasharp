namespace BetaSharp.Diagnostics;

/// <summary>
/// Metrics for the server.
/// </summary>
public static class ServerMetrics
{
    public static readonly MetricHandle<float> Tps = MetricRegistry.Register<float>("server:tps");

    /// <summary>
    /// Amount of milliseconds each tick takes, in float.
    /// </summary>
    public static readonly MetricHandle<float> Mspt = MetricRegistry.Register<float>("server:mspt");

    public static readonly MetricHandle<int> EntityCount = MetricRegistry.Register<int>("server:entity_count");
    public static readonly MetricHandle<int> PlayerCount = MetricRegistry.Register<int>("server:player_count");
    public static readonly MetricHandle<int> OverworldEntityCount = MetricRegistry.Register<int>("server:entity_count_overworld");
    public static readonly MetricHandle<int> NetherEntityCount = MetricRegistry.Register<int>("server:entity_count_nether");
    public static readonly MetricHandle<int> BlockEntityCount = MetricRegistry.Register<int>("server:block_entity_count");
    public static readonly MetricHandle<int> PendingConnections = MetricRegistry.Register<int>("server:pending_connections");
    public static readonly MetricHandle<int> ActiveConnections = MetricRegistry.Register<int>("server:active_connections");
    public static readonly MetricHandle<long> BytesRead = MetricRegistry.Register<long>("server:bytes_read_total");
    public static readonly MetricHandle<long> BytesWritten = MetricRegistry.Register<long>("server:bytes_written_total");
    public static readonly MetricHandle<long> PacketsRead = MetricRegistry.Register<long>("server:packets_read_total");
    public static readonly MetricHandle<long> PacketsWritten = MetricRegistry.Register<long>("server:packets_written_total");
    public static readonly MetricHandle<int> ChunkLoadsPending = MetricRegistry.Register<int>("server:chunk_loads_pending");
    public static readonly MetricHandle<int> TrackedChunks = MetricRegistry.Register<int>("server:tracked_chunks");
    public static readonly MetricHandle<int> DirtyTrackedChunks = MetricRegistry.Register<int>("server:dirty_tracked_chunks");
    public static readonly MetricHandle<int> PendingChunkSends = MetricRegistry.Register<int>("server:pending_chunk_sends");
    public static readonly MetricHandle<int> MaxPendingChunkSends = MetricRegistry.Register<int>("server:max_pending_chunk_sends");
    public static readonly MetricHandle<int> LightingQueue = MetricRegistry.Register<int>("server:lighting_queue");
    public static readonly MetricHandle<int> ScheduledBlockTicks = MetricRegistry.Register<int>("server:scheduled_block_ticks");
    public static readonly MetricHandle<int> OverworldTrackedEntities = MetricRegistry.Register<int>("server:tracked_entities_overworld");
    public static readonly MetricHandle<int> NetherTrackedEntities = MetricRegistry.Register<int>("server:tracked_entities_nether");
    public static readonly MetricHandle<long> WorkingSetBytes = MetricRegistry.Register<long>("server:working_set_bytes");
    public static readonly MetricHandle<long> HeapBytes = MetricRegistry.Register<long>("server:heap_bytes");

    static ServerMetrics() { }
}
