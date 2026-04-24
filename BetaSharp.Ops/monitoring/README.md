# BetaSharp Monitoring Stack

This stack provides live operational visibility for the dedicated server.

The monitoring stack runs fully inside Docker Compose, including the dedicated server.

## Tool versions

The stack is pinned to current releases at the time of this update:

- Prometheus `v3.11.2`
- Grafana `13.0.1`

## Environment overrides

You can customize the compose stack without editing YAML.

Copy the example file:

```bash
cp .env.example .env
```

Available variables:

- `PROMETHEUS_PORT`
- `GRAFANA_PORT`
- `GRAFANA_ADMIN_USER`
- `GRAFANA_ADMIN_PASSWORD`
- `BETASHARP_SERVER_PORT`
- `BETASHARP_DOTNET_CONFIGURATION`

`BETASHARP_DOTNET_CONFIGURATION` controls the server container build mode:

- `Release` (recommended default)
- `Debug` (for heavy investigation only)

## Server configuration

Add these values to `server.properties`:

```properties
metrics-http-enabled=true
metrics-http-host=0.0.0.0
metrics-http-port=9464
profiling-detail=basic
stats-log-interval-seconds=0
```

### Configuration reference

`metrics-http-enabled`

- Enables the dedicated server's monitoring HTTP listener.
- Default: `false`

`metrics-http-host`

- Bind address for the monitoring listener.
- Use `0.0.0.0` for the provided Docker-based Prometheus stack.
- `0.0.0.0`, `*`, and `+` are normalized to a wildcard `HttpListener` binding internally.

`metrics-http-port`

- TCP port for the monitoring listener.
- Default: `9464`

`profiling-detail`

- Controls how much profiling work is active.
- Supported values:
  - `disabled`
  - `basic`
  - `detailed`
- `basic` is the production-safe setting.
- `detailed` is only honored in debug builds. Release builds cap it to `basic`.

`stats-log-interval-seconds`

- Optional periodic server stats log line interval.
- `0` disables periodic stat logging.

## Monitoring endpoints

The server exposes these endpoints when monitoring is enabled:

`/healthz`

- Plain-text liveness endpoint.
- Intended for quick checks and container probes.

`/stats`

- Human-readable multi-line server summary.
- Good for quick SSH or browser inspection.

`/stats.json`

- JSON version of the same summary.
- Useful for ad-hoc tooling.

`/metrics`

- Prometheus text exposition endpoint.
- This is what the provided Prometheus config scrapes.

`/profiler`

- Plain-text profiler snapshot.
- Most useful when `profiling-detail` is `basic` or `detailed`.
- In release builds, this remains intentionally coarse.

## Metrics exported

The current server export includes these metric groups:

- tick health: TPS, MSPT
- population: players, entities, block entities
- networking: active/pending connections, bytes, packets
- chunk and world queues: pending chunk loads/sends, tracked chunks, dirty tracked chunks, lighting queue, scheduled block ticks
- trackers: tracked entities by dimension
- memory: working set and managed heap size

All exported values are intentionally aggregated and low-cardinality.

## Run the stack

From `ops/monitoring/`:

```bash
docker compose up -d --build
```

Containerized server notes:

- server data is stored in `ops/monitoring/server-data/`
- a sample `server.properties` is provided at `ops/monitoring/server-data/server.properties`
- ensure `b1.7.3.jar` exists at the project root (`/home/tacf/code/betasharp/b1.7.3.jar`)
- the compose file bind-mounts that root jar into the server container as `/data/b1.7.3.jar`
- the Docker image can seed `/data` from `ops/monitoring/server-data/` on first start for config defaults
- the server exposes:
  - game port `25565`

The metrics port is not published to the host. Prometheus scrapes it over the internal Docker network.

Endpoints:

- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

Grafana default credentials:

- User: `admin` by default, or `GRAFANA_ADMIN_USER` from `.env`
- Password: `admin` by default, or `GRAFANA_ADMIN_PASSWORD` from `.env`

## Dashboard

Grafana is preprovisioned with a `BetaSharp Server` dashboard.

It includes:

- TPS and MSPT headline stats
- player/entity counts
- active connections
- working set memory
- network throughput
- queue depth panels for chunk loading, chunk sending, lighting, and scheduled block ticks

## Expected network path

- the server listens on `9464` in the `betasharp-server` container
- Prometheus scrapes `betasharp-server:9464`

## Quick checks

With the stack running, these should work:

```bash
curl http://127.0.0.1:9090/api/v1/targets
curl http://127.0.0.1:3000/api/health -u admin:admin
```

## Troubleshooting

Prometheus shows target down:

- Verify the `betasharp-server` container is running.
- Verify `b1.7.3.jar` exists at the project root before startup.
- Verify the game port configured in `.env` is free on the host.
- Rebuild if you added the jar after the last image build:

```bash
docker compose up -d --build
```

Grafana is empty:

- Confirm Prometheus has a healthy `betasharp-server` target.
- Wait one scrape interval after the server starts.

`/profiler` looks sparse:

- That is expected in release builds with `profiling-detail=basic`.
- For deeper scope data, use `BETASHARP_DOTNET_CONFIGURATION=Debug` and `profiling-detail=detailed`.

## Production guidance

- Recommended production setting: `profiling-detail=basic`
- Recommended production build: `BETASHARP_DOTNET_CONFIGURATION=Release`
- Keep `stats-log-interval-seconds=0` unless periodic summaries are explicitly wanted.
- Expose the monitoring listener only on trusted networks.
- The provided Docker stack is intended for local or controlled-host deployment, not open internet exposure.
