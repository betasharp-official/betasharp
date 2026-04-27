# BetaSharp Monitoring Runbook

## Quick Start

1. Check Grafana `BetaSharp Server` dashboard.
2. Check Prometheus targets: `http://localhost:9090/targets`.
3. Check container health and logs:

```bash
docker compose ps
docker compose logs --tail=100 server
```

## Basic health checks

Check current TPS:

```bash
curl -fsS "http://127.0.0.1:9090/api/v1/query?query=betasharp_server_tps"
```

Check current MSPT:

```bash
curl -fsS "http://127.0.0.1:9090/api/v1/query?query=betasharp_server_mspt"
```

Check queue peaks (1m):

```bash
curl -fsS "http://127.0.0.1:9090/api/v1/query?query=max_over_time(betasharp_server_chunk_loads_pending[1m])"
curl -fsS "http://127.0.0.1:9090/api/v1/query?query=max_over_time(betasharp_server_pending_chunk_sends[1m])"
curl -fsS "http://127.0.0.1:9090/api/v1/query?query=max_over_time(betasharp_server_lighting_queue[1m])"
curl -fsS "http://127.0.0.1:9090/api/v1/query?query=max_over_time(betasharp_server_scheduled_block_ticks[1m])"
```

Inspect recent server logs:

```bash
docker compose logs --tail=200 server
```

Fetch profiler snapshot (without adding tools to server image):

```bash
docker run --rm --network container:betasharp-server curlimages/curl:8.11.1 http://127.0.0.1:9464/profiler
```

## Common issues

Prometheus target down:

- Ensure `betasharp-server` is running.
- Ensure root jar exists at `b1.7.3.jar`.
- Rebuild after jar/config changes: `docker compose up -d --build`.

High MSPT with low TPS:

- Check queue peak queries above.
- Check `/profiler` snapshot for dominant scopes.
- Reproduce with debug build only when needed.

## Increase Profiling Level

For deeper profiling:

1. Set `BETASHARP_DOTNET_CONFIGURATION=Debug` in `.env`.
2. Set `profiling-detail=detailed` in `server.properties`.
3. Rebuild server: `docker compose up -d --build server`.
