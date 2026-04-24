# BetaSharp Server Metric Catalog

This catalog documents the server metrics exported at `/metrics`.

Metric names are exposed in Prometheus as `betasharp_<metric-key>`.

Example:

- internal key: `server:tps`
- Prometheus name: `betasharp_server_tps`

## Tick health

`betasharp_server_tps`

- Current ticks per second.
- Higher is better.
- Normal steady-state target is close to `20`.

`betasharp_server_mspt`

- Milliseconds per tick.
- Lower is better.
- Sustained values above `50` usually imply the server cannot sustain 20 TPS.

## Population

`betasharp_server_player_count`

- Connected player count.

`betasharp_server_entity_count`

- Total entity count across loaded server worlds.

`betasharp_server_entity_count_overworld`

- Entity count in the overworld.

`betasharp_server_entity_count_nether`

- Entity count in the nether.

`betasharp_server_block_entity_count`

- Total loaded block entities across worlds.

## Connections and traffic

`betasharp_server_pending_connections`

- Connections still in the login pipeline.

`betasharp_server_active_connections`

- Active play connections.

`betasharp_server_bytes_read_total`

- Monotonic total bytes read by server-side connections.

`betasharp_server_bytes_written_total`

- Monotonic total bytes written by server-side connections.

`betasharp_server_packets_read_total`

- Monotonic total packets read by server-side connections.

`betasharp_server_packets_written_total`

- Monotonic total packets written by server-side connections.

Notes:

- These totals are process-lifetime counters and are safe to use with `rate(...)` in Prometheus.

## Chunk and world queues

`betasharp_server_chunk_loads_pending`

- Total pending chunk loads tracked by the player/chunk map pipeline.

`betasharp_server_tracked_chunks`

- Total tracked chunks across dimensions.

`betasharp_server_dirty_tracked_chunks`

- Tracked chunks currently marked dirty for update work.

`betasharp_server_pending_chunk_sends`

- Sum of per-player pending chunk send queues.

`betasharp_server_max_pending_chunk_sends`

- Largest pending chunk send queue on any single player.

`betasharp_server_lighting_queue`

- Total pending lighting updates across worlds.

`betasharp_server_scheduled_block_ticks`

- Total pending scheduled block ticks across worlds.

## Entity trackers

`betasharp_server_tracked_entities_overworld`

- Number of entities currently tracked by the overworld entity tracker.

`betasharp_server_tracked_entities_nether`

- Number of entities currently tracked by the nether entity tracker.

## Memory

`betasharp_server_working_set_bytes`

- Current process working set in bytes.
- Includes non-managed memory as reported by the runtime/environment.

`betasharp_server_heap_bytes`

- Current managed heap size approximation from `GC.GetTotalMemory(false)`.

## Endpoint notes

- `/stats` and `/stats.json` present a friendly snapshot built from the same underlying server state.
- `/metrics` is the canonical endpoint for scraping and alerting.
- `/profiler` is separate from metrics and reports coarse scope timing information.

## Production guidance

- Prefer alerting on sustained bad values using `avg_over_time(...)`, `max_over_time(...)`, and `rate(...)`.
- Avoid building dashboards that depend on high-cardinality labels; the current export intentionally does not provide them.
- Treat `profiling-detail=basic` as the default release setting.
