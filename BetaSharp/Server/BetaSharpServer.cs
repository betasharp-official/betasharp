using System.Diagnostics;
using BetaSharp.Diagnostics;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Profiling;
using BetaSharp.Registries;
using BetaSharp.Registries.Data;
using BetaSharp.Server.Command;
using BetaSharp.Server.Entities;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Network;
using BetaSharp.Server.Worlds;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using ServerWorld = BetaSharp.Worlds.Core.ServerWorld;

namespace BetaSharp.Server;

public abstract class BetaSharpServer : ICommandOutput
{
    public RegistryAccess RegistryAccess { get; set; } = RegistryAccess.Empty;

    public Holder<GameMode> DefaultGameMode { get; set; } = new(new GameMode());

    private readonly List<IRegistryReloadListener> _reloadListeners = [];

    public Dictionary<string, int> GIVE_COMMANDS_COOLDOWNS = [];
    public ConnectionListener connections;
    public IServerConfiguration config;
    public ServerWorld[] worlds;
    public PlayerManager playerManager;
    private ServerCommandHandler _commandHandler;
    public bool running = true;
    public bool stopped;
    private int _ticks;
    public string? progressMessage;
    public int progress;
    private readonly Queue<PendingCommand> _pendingCommands = new();
    private readonly object _pendingCommandsLock = new();
    public EntityTracker[] entityTrackers = new EntityTracker[2];
    public bool onlineMode;
    public bool spawnAnimals;
    public bool pvpEnabled;
    public bool flightEnabled;
    protected bool logHelp = true;

    private readonly ILogger<BetaSharpServer> _logger = Log.Instance.For<BetaSharpServer>();
    private readonly object _tpsLock = new();
    private readonly ServerDiagnosticsOptions _diagnosticsOptions;
    private long _lastTpsTime;
    private long _lastStatsLogTime;
    private int _ticksThisSecond;
    private float _currentTps;

    private volatile bool _isPaused;

    public float Tps
    {
        get
        {
            lock (_tpsLock)
            {
                return _currentTps;
            }
        }
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
    }

    protected BetaSharpServer(IServerConfiguration config)
    {
        this.config = config;
        _diagnosticsOptions = ServerDiagnosticsOptions.FromConfiguration(config);
        Profiler.DetailLevel = _diagnosticsOptions.ProfilingDetail;

        if (_diagnosticsOptions.ProfilingDetailCappedToBuild)
        {
            _logger.LogWarning(
                "Profiling detail requested '{RequestedRaw}' ({RequestedCanonical}) but effective level is '{Effective}' for this build.",
                _diagnosticsOptions.RequestedProfilingDetailRaw,
                _diagnosticsOptions.RequestedProfilingDetail,
                _diagnosticsOptions.ProfilingDetail);
        }
        else if (!_diagnosticsOptions.ProfilingDetailValueRecognized)
        {
            _logger.LogWarning(
                "Profiling detail value '{RequestedRaw}' is not recognized. Falling back to '{Effective}'.",
                _diagnosticsOptions.RequestedProfilingDetailRaw,
                _diagnosticsOptions.ProfilingDetail);
        }
        else
        {
            _logger.LogInformation(
                "Profiling detail requested '{RequestedCanonical}', effective level '{Effective}'.",
                _diagnosticsOptions.RequestedProfilingDetail,
                _diagnosticsOptions.ProfilingDetail);
        }
    }

    public ServerDiagnosticsOptions DiagnosticsOptions => _diagnosticsOptions;

    protected virtual bool Init()
    {
        _commandHandler = new ServerCommandHandler(this);

        RegisterReloadListener(new DefaultGameModeListener(this));

        onlineMode = config.GetOnlineMode(true);
        spawnAnimals = config.GetSpawnAnimals(true);
        pvpEnabled = config.GetPvpEnabled(true);
        flightEnabled = config.GetAllowFlight(false);

        playerManager = CreatePlayerManager();
        entityTrackers[0] = new EntityTracker(this, 0);
        entityTrackers[1] = new EntityTracker(this, -1);

        var startupSw = Stopwatch.StartNew();

        string worldName = config.GetLevelName("world");
        string seedString = config.GetLevelSeed("");
        long seed = Random.Shared.NextInt64();

        if (!string.IsNullOrEmpty(seedString))
        {
            if (!long.TryParse(seedString, out seed))
            {
                // Java-compatible String.hashCode() behavior
                int hash = 0;
                foreach (char c in seedString)
                {
                    hash = 31 * hash + c;
                }

                seed = hash;
            }
        }

        string typeString = config.GetLevelType("DEFAULT");
        WorldType worldType = WorldType.ParseWorldType(typeString) ?? WorldType.Default;
        string optionsString = config.GetLevelOptions("");

        _logger.LogInformation("Preparing level \"{WorldName}\"", worldName);
        loadWorld(worldName, new WorldSettings(seed, worldType, optionsString));

        foreach (IRegistryReloadListener listener in _reloadListeners)
        {
            listener.OnRegistriesRebuilt(RegistryAccess);
        }

        if (logHelp)
        {
            _logger.LogInformation(
                "Done ({ElapsedMs}ms)! For help, type \"help\" or \"?\"",
                startupSw.ElapsedMilliseconds);
        }

        return true;
    }

    private void loadWorld(string worldDir, WorldSettings settings)
    {
        worlds = new ServerWorld[2];
        var dir = new DirectoryInfo(Path.Combine(GetFile(".").FullName, worldDir));
        RegionWorldStorage worldStorage = new(dir, true);
        RegistryAccess = RegistryAccess.WithWorldDatapacks(dir.FullName);

        for (int i = 0; i < worlds.Length; i++)
        {
            if (i == 0)
            {
                worlds[i] = new ServerWorld(this, worldStorage, worldDir, 0, settings, null);
            }
            else
            {
                worlds[i] = new ReadOnlyServerWorld(this, worldStorage, worldDir, -1, settings, worlds[0]);
            }

            worlds[i].EventListeners.Add(new ServerWorldEventListener(this, worlds[i]));
            worlds[i].SetDifficulty(config.GetSpawnMonsters(true) ? 1 : 0);
            worlds[i].allowSpawning(config.GetSpawnMonsters(true), spawnAnimals);
            playerManager.saveAllPlayers(worlds);
        }

        int startRegionSize = config.GetSpawnRegionSize(196);
        long lastTimeLogged = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (int i = 0; i < worlds.Length; i++)
        {
            _logger.LogInformation("Preparing start region for level {Level}", i);

            // Only pre-generate the overworld spawn region. The nether is only accessible
            // via portal (which implies a teleport/load anyway), so on-demand generation
            // there is fine and avoids the 40+ second lava-sea light propagation cost.
            if (i == 0)
            {
                ServerWorld world = worlds[i];
                Vec3i spawnPos = world.Properties.GetSpawnPos();

                var chunkList = new List<Vector2D<int>>();
                for (int x = -startRegionSize; x <= startRegionSize; x += 16)
                {
                    for (int z = -startRegionSize; z <= startRegionSize; z += 16)
                    {
                        chunkList.Add(new Vector2D<int>((spawnPos.X + x) >> 4, (spawnPos.Z + z) >> 4));
                    }
                }

                int totalChunks = chunkList.Count;
                var preGenerated = new Chunk[totalChunks];

                // Phase 1: Parallel terrain generation
                var sw1 = Stopwatch.StartNew();
                var threadLocalGen = new ThreadLocal<IChunkSource>(world.ChunkCache.CreateParallelGenerator, trackAllValues: false);
                Parallel.For(0, totalChunks, idx =>
                {
                    if (!running)
                    {
                        return;
                    }

                    Vector2D<int> chunkPos = chunkList[idx];
                    preGenerated[idx] = threadLocalGen.Value!.GetChunk(chunkPos.X, chunkPos.Y);
                });

                threadLocalGen.Dispose();
                sw1.Stop();
                _logger.LogInformation("  Level {Level} terrain: {ElapsedMs}ms", i, sw1.ElapsedMilliseconds);

                // Phase 2a: Insert all chunks first (required so decoration can write to neighbors without hitting EmptyChunk)
                var sw2 = Stopwatch.StartNew();
                for (int idx = 0; idx < totalChunks && running; idx++)
                {
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (currentTime > lastTimeLogged + 1000L)
                    {
                        logProgress("Preparing spawn area", (idx + 1) * 100 / totalChunks);
                        lastTimeLogged = currentTime;
                    }

                    Vector2D<int> chunkPos = chunkList[idx];
                    world.ChunkCache.InsertPreGeneratedChunk(chunkPos.X, chunkPos.Y, preGenerated[idx]);
                    world.ChunkCache.DecorateIfReady(chunkPos.X, chunkPos.Y);
                }

                sw2.Stop();
                _logger.LogInformation("  Level {Level} decoration: {ElapsedMs}ms", i, sw2.ElapsedMilliseconds);

                // Phase 3: Batch lighting drain — all neighbors already loaded so sky-light
                // propagates without border re-queuing.
                var sw3 = Stopwatch.StartNew();
                while (world.Lighting.DoLightingUpdates() && running) { }
                sw3.Stop();
                _logger.LogInformation("  Level {Level} lighting: {ElapsedMs}ms", i, sw3.ElapsedMilliseconds);
            }
        }

        clearProgress();
    }

    private void logProgress(string progressType, int progress)
    {
        progressMessage = progressType;
        this.progress = progress;
        _logger.LogInformation("{ProgressType}: {Progress}%", progressType, progress);
    }

    private void clearProgress()
    {
        progressMessage = null;
        progress = 0;
    }

    private void saveWorlds()
    {
        _logger.LogInformation("Saving chunks");

        foreach (ServerWorld world in worlds)
        {
            world.SaveWithLoadingDisplay(true, null);
            world.forceSave();
        }
    }

    private void shutdown()
    {
        if (stopped)
        {
            return;
        }

        _logger.LogInformation("Stopping server");

        playerManager?.savePlayers();

        foreach (ServerWorld world in worlds)
        {
            if (world != null)
            {
                saveWorlds();
                break;
            }
        }

        if (this is InternalServer)
        {
            RegistryAccess = RegistryAccess.WithoutWorldDatapacks();
        }

        OnShutdown();
    }

    public void Stop()
    {
        running = false;
    }

    public void RunThreaded(string threadName)
    {
        Thread thread = new(run)
        {
            Name = threadName
        };
        thread.Start();
    }

    private void run()
    {
        Profiler.RegisterServerThread();
        try
        {
            if (Init())
            {
                long lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long accumulatedTime = 0L;
                _lastTpsTime = lastTime;
                _lastStatsLogTime = lastTime;
                _ticksThisSecond = 0;
                var tickStopwatch = new Stopwatch();

                while (running)
                {
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long tickLength = currentTime - lastTime;
                    if (tickLength > 2000L)
                    {
                        _logger.LogWarning("Can't keep up! Did the system time change, or is the server overloaded?");
                        tickLength = 2000L;
                    }

                    if (tickLength < 0L)
                    {
                        _logger.LogWarning("Time ran backwards! Did the system time change?");
                        tickLength = 0L;
                    }

                    accumulatedTime += tickLength;
                    lastTime = currentTime;
                    Profiler.Update(tickLength / 1000.0);

                    if (_isPaused)
                    {
                        accumulatedTime = 0L;
                        lock (_tpsLock)
                        {
                            _currentTps = 0.0f;
                        }
                        MetricRegistry.Set(ServerMetrics.Tps, 0.0f);
                        Thread.Sleep(50);
                        continue;
                    }

                    while (accumulatedTime >= 50L && running)
                    {
                        accumulatedTime -= 50L;
                        tickStopwatch.Restart();
                        using (Profiler.Begin("Tick"))
                        {
                            tick();
                        }
                        tickStopwatch.Stop();
                        MetricRegistry.Set(ServerMetrics.Mspt, (float)tickStopwatch.Elapsed.TotalMilliseconds);
                        _ticksThisSecond++;
                    }

                    long tpsNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long tpsElapsed = tpsNow - _lastTpsTime;
                    if (tpsElapsed >= 1000L)
                    {
                        lock (_tpsLock)
                        {
                            _currentTps = _ticksThisSecond * 1000.0f / tpsElapsed;
                        }
                        _ticksThisSecond = 0;
                        _lastTpsTime = tpsNow;
                        RefreshDiagnosticsMetrics();
                        MaybeLogStats(tpsNow);
                    }

                    Profiler.CaptureFrame();

                    Thread.Sleep(1);
                }
            }
            else
            {
                while (running)
                {
                    RunPendingCommands();

                    try
                    {
                        Thread.Sleep(10);
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        _logger.LogWarning(ex, "Server thread interrupted while idle.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception");

            while (running)
            {
                RunPendingCommands();

                try
                {
                    Thread.Sleep(10);
                }
                catch (ThreadInterruptedException interruptedEx)
                {
                    _logger.LogWarning(interruptedEx, "Server thread interrupted after failure.");
                }
            }
        }
        finally
        {
            try
            {
                shutdown();
                stopped = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during shutdown.");
            }
            finally
            {
                if (this is not InternalServer)
                {
                    Environment.Exit(0);
                }
            }
        }
    }

    private void tick()
    {
        using (Profiler.Begin("Cooldowns", ProfilingDetailLevel.Detailed))
        {
            // Snapshot keys to allow safe mutation during iteration.
            var keysSnapshot = new List<string>(GIVE_COMMANDS_COOLDOWNS.Keys);
            foreach (var key in keysSnapshot)
            {
                if (GIVE_COMMANDS_COOLDOWNS.TryGetValue(key, out int cooldown))
                {
                    if (cooldown > 0)
                        GIVE_COMMANDS_COOLDOWNS[key] = cooldown - 1;
                    else
                        GIVE_COMMANDS_COOLDOWNS.Remove(key);
                }
            }
        }

        _ticks++;

        using (Profiler.Begin("Worlds"))
        {
            for (int i = 0; i < worlds.Length; i++)
            {
                if (i == 0 || config.GetAllowNether(true))
                {
                    ServerWorld world = worlds[i];
                    using (Profiler.Begin(world.Dimension.Id == 0 ? "Overworld" : "Nether", ProfilingDetailLevel.Detailed))
                    {
                        if (_ticks % 20 == 0)
                        {
                            playerManager.sendToDimension(WorldTimeUpdateS2CPacket.Get(world.GetTime()), world.Dimension.Id);
                        }

                        using (Profiler.Begin("TickWorld"))
                        {
                            world.Tick();
                        }

                        // Cap lighting updates to avoid spending the entire tick (and beyond)
                        // draining the queue. The nether's lava seas can generate thousands
                        // of lighting entries per tick; processing them all in one go causes
                        // >2-second stalls and "Can't keep up" spam. Any remaining work
                        // carries over and is processed across subsequent ticks.
                        using (Profiler.Begin("Lighting"))
                        {
                            var lightSw = Stopwatch.StartNew();
                            while (lightSw.ElapsedMilliseconds < 15L && world.Lighting.DoLightingUpdates())
                            {
                            }
                        }

                        using (Profiler.Begin("TickEntities"))
                        {
                            world.Entities.TickEntities();
                        }
                    }
                }
            }
        }

        using (Profiler.Begin("Connections"))
        {
            connections?.Tick();
        }

        using (Profiler.Begin("ChunkTracking"))
        {
            playerManager.updateAllChunks();
            playerManager.flushPendingChunkUpdates();
        }

        using (Profiler.Begin("EntityTracking"))
        {
            foreach (EntityTracker t in entityTrackers)
            {
                t.tick();
            }
        }

        try
        {
            using (Profiler.Begin("Commands"))
            {
                RunPendingCommands();
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning($"Unexpected exception while parsing console command: {e}");
        }
    }

    public void QueueCommands(string str, ICommandOutput cmd)
    {
        lock (_pendingCommandsLock)
        {
            _pendingCommands.Enqueue(new PendingCommand(str, cmd));
        }
    }

    private void RunPendingCommands()
    {
        while (true)
        {
            PendingCommand cmd;
            lock (_pendingCommandsLock)
            {
                if (_pendingCommands.Count == 0) break;
                cmd = _pendingCommands.Dequeue();
            }
            _commandHandler.ExecuteCommand(cmd);
        }
    }

    public abstract FileInfo GetFile(string path);

    protected virtual void OnShutdown()
    {
    }

    public ServerDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        int overworldEntityCount = worlds.Length > 0 && worlds[0] != null ? worlds[0].Entities.Entities.Count : 0;
        int netherEntityCount = worlds.Length > 1 && worlds[1] != null ? worlds[1].Entities.Entities.Count : 0;
        int entityCount = overworldEntityCount + netherEntityCount;
        int blockEntityCount = 0;
        int lightingQueue = 0;
        int scheduledBlockTicks = 0;

        for (int i = 0; i < worlds.Length; i++)
        {
            if (worlds[i] == null)
            {
                continue;
            }

            blockEntityCount += worlds[i].Entities.BlockEntities.Count;
            lightingQueue += worlds[i].Lighting.PendingUpdateCount;
            scheduledBlockTicks += worlds[i].TickScheduler.PendingCount;
        }

        ConnectionListener.ConnectionTotals connectionTotals = connections?.GetTotals() ?? default;
        (int pendingChunkSends, int maxPendingChunkSends) = playerManager.GetPendingChunkSendCounts();

        return new ServerDiagnosticsSnapshot(
            DateTimeOffset.UtcNow,
            Tps,
            MetricRegistry.Get(ServerMetrics.Mspt),
            playerManager.players.Count,
            entityCount,
            overworldEntityCount,
            netherEntityCount,
            blockEntityCount,
            connections?.PendingConnectionCount ?? 0,
            connections?.ActiveConnectionCount ?? 0,
            connectionTotals.BytesRead,
            connectionTotals.BytesWritten,
            connectionTotals.PacketsRead,
            connectionTotals.PacketsWritten,
            playerManager.PendingChunkCount,
            playerManager.TrackedChunkCount,
            playerManager.DirtyTrackedChunkCount,
            pendingChunkSends,
            maxPendingChunkSends,
            lightingQueue,
            scheduledBlockTicks,
            entityTrackers[0]?.Count ?? 0,
            entityTrackers[1]?.Count ?? 0,
            Environment.WorkingSet,
            GC.GetTotalMemory(false));
    }

    public string GetDiagnosticsSummary() => ServerDiagnosticsFormatter.FormatSummary(GetDiagnosticsSnapshot());

    public string GetDiagnosticsJson() => ServerDiagnosticsFormatter.FormatJson(GetDiagnosticsSnapshot());

    public string GetMetricsText() => ServerDiagnosticsFormatter.FormatMetrics();

    public string GetProfilerText() => ServerDiagnosticsFormatter.FormatProfiler();

    public void SendMessage(string message)
    {
        _logger.LogInformation(message);
    }

    public void Warn(string message)
    {
        _logger.LogWarning(message);
    }

    public string Name => "CONSOLE";
    public byte PermissionLevel => 255;

    public ServerWorld getWorld(int dimensionId)
    {
        return dimensionId == -1 ? worlds[1] : worlds[0];
    }

    public EntityTracker getEntityTracker(int dimensionId)
    {
        return dimensionId == -1 ? entityTrackers[1] : entityTrackers[0];
    }

    protected virtual PlayerManager CreatePlayerManager()
    {
        return new PlayerManager(this);
    }

    /// <summary>
    /// Registers a listener that will be notified whenever datapacks are reloaded.
    /// Use this to refresh any data cached from registry lookups.
    /// </summary>
    public void RegisterReloadListener(IRegistryReloadListener listener)
        => _reloadListeners.Add(listener);

    /// <summary>
    /// Sends all reloadable registry data packets followed by <see cref="FinishConfigurationS2CPacket"/>
    /// </summary>
    public void SendConfigurationTo(Action<Packet> send)
    {
        foreach (RegistryDataS2CPacket packet in RegistryAccess.BuildSyncPackets())
        {
            send(packet);
        }

        send(Packet.Get<FinishConfigurationS2CPacket>(PacketId.FinishConfigurationS2C));
    }

    /// <summary>
    /// Reloads all data-driven content from disk. Re-reads base assets, global datapacks, and
    /// world datapacks, then broadcasts a status message to all connected players.
    /// </summary>
    public void ReloadDatapacks()
    {
        _logger.LogInformation("Reloading datapacks...");
        playerManager.sendToAll(ChatMessagePacket.Get("§eReloading datapacks..."));
        try
        {
            RegistryAccess = RegistryAccess.Rebuild();

            RegistryReloadPipeline.SyncToPlayers(RegistryAccess, _reloadListeners, playerManager.players);

            _logger.LogInformation("Datapacks reloaded.");
            playerManager.sendToAll(ChatMessagePacket.Get("§aDatapacks reloaded."));
        }
        catch (AssetLoadException ex)
        {
            _logger.LogError("Datapack reload failed: {Message}.", ex.Message);

            if (this is InternalServer)
            {
                playerManager.sendToAll(ChatMessagePacket.Get($"§cReload failed! See console for details."));
            }
        }
    }

    private void RefreshDiagnosticsMetrics()
    {
        ServerDiagnosticsSnapshot snapshot = GetDiagnosticsSnapshot();

        MetricRegistry.Set(ServerMetrics.Tps, snapshot.Tps);
        MetricRegistry.Set(ServerMetrics.PlayerCount, snapshot.PlayerCount);
        MetricRegistry.Set(ServerMetrics.EntityCount, snapshot.EntityCount);
        MetricRegistry.Set(ServerMetrics.OverworldEntityCount, snapshot.OverworldEntityCount);
        MetricRegistry.Set(ServerMetrics.NetherEntityCount, snapshot.NetherEntityCount);
        MetricRegistry.Set(ServerMetrics.BlockEntityCount, snapshot.BlockEntityCount);
        MetricRegistry.Set(ServerMetrics.PendingConnections, snapshot.PendingConnections);
        MetricRegistry.Set(ServerMetrics.ActiveConnections, snapshot.ActiveConnections);
        MetricRegistry.Set(ServerMetrics.BytesRead, snapshot.BytesRead);
        MetricRegistry.Set(ServerMetrics.BytesWritten, snapshot.BytesWritten);
        MetricRegistry.Set(ServerMetrics.PacketsRead, snapshot.PacketsRead);
        MetricRegistry.Set(ServerMetrics.PacketsWritten, snapshot.PacketsWritten);
        MetricRegistry.Set(ServerMetrics.ChunkLoadsPending, snapshot.PendingChunkLoads);
        MetricRegistry.Set(ServerMetrics.TrackedChunks, snapshot.TrackedChunks);
        MetricRegistry.Set(ServerMetrics.DirtyTrackedChunks, snapshot.DirtyTrackedChunks);
        MetricRegistry.Set(ServerMetrics.PendingChunkSends, snapshot.PendingChunkSends);
        MetricRegistry.Set(ServerMetrics.MaxPendingChunkSends, snapshot.MaxPendingChunkSends);
        MetricRegistry.Set(ServerMetrics.LightingQueue, snapshot.LightingQueue);
        MetricRegistry.Set(ServerMetrics.ScheduledBlockTicks, snapshot.ScheduledBlockTicks);
        MetricRegistry.Set(ServerMetrics.OverworldTrackedEntities, snapshot.OverworldTrackedEntities);
        MetricRegistry.Set(ServerMetrics.NetherTrackedEntities, snapshot.NetherTrackedEntities);
        MetricRegistry.Set(ServerMetrics.WorkingSetBytes, snapshot.WorkingSetBytes);
        MetricRegistry.Set(ServerMetrics.HeapBytes, snapshot.HeapBytes);
    }

    private void MaybeLogStats(long currentTimeMs)
    {
        if (_diagnosticsOptions.StatsLogIntervalSeconds <= 0)
        {
            return;
        }

        long intervalMs = _diagnosticsOptions.StatsLogIntervalSeconds * 1000L;
        if (currentTimeMs - _lastStatsLogTime < intervalMs)
        {
            return;
        }

        _lastStatsLogTime = currentTimeMs;
        _logger.LogInformation("{Stats}", GetDiagnosticsSummary());
    }
}
