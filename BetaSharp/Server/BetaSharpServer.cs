using System.Diagnostics;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server.Command;
using BetaSharp.Server.Entities;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Network;
using BetaSharp.Server.Worlds;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using BetaSharp.Worlds;
using ServerWorld = BetaSharp.Worlds.Core.ServerWorld;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Server;

public abstract class BetaSharpServer : ICommandOutput
{
    public Dictionary<string, int> GIVE_COMMANDS_COOLDOWNS = [];
    public ConnectionListener connections;
    public IServerConfiguration config;
    public ServerWorld[] worlds;
    public PlayerManager playerManager;
    private ServerCommandHandler commandHandler;
    public bool running = true;
    public bool stopped;
    private int ticks;
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
    private long _lastTpsTime;
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
    }

    protected virtual bool Init()
    {
        commandHandler = new ServerCommandHandler(this);

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
        RegionWorldStorage worldStorage = new(GetFile(".").FullName, worldDir, true);

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
        try
        {
            if (Init())
            {
                long lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long accumulatedTime = 0L;
                _lastTpsTime = lastTime;
                _ticksThisSecond = 0;

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

                    if (_isPaused)
                    {
                        accumulatedTime = 0L;
                        lock (_tpsLock)
                        {
                            _currentTps = 0.0f;
                        }
                        Thread.Sleep(50);
                        continue;
                    }

                    while (accumulatedTime >= 50L && running)
                    {
                        accumulatedTime -= 50L;
                        tick();
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
                    }

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

        ticks++;

        for (int i = 0; i < worlds.Length; i++)
        {
            if (i == 0 || config.GetAllowNether(true))
            {
                ServerWorld world = worlds[i];
                if (ticks % 20 == 0)
                {
                    playerManager.sendToDimension(WorldTimeUpdateS2CPacket.Get(world.GetTime()), world.Dimension.Id);
                }

                world.Tick();

                // Cap lighting updates to avoid spending the entire tick (and beyond)
                // draining the queue. The nether's lava seas can generate thousands
                // of lighting entries per tick; processing them all in one go causes
                // >2-second stalls and "Can't keep up" spam. Any remaining work
                // carries over and is processed across subsequent ticks.
                var lightSw = Stopwatch.StartNew();
                while (lightSw.ElapsedMilliseconds < 15L && world.Lighting.DoLightingUpdates())
                {
                }

                world.Entities.TickEntities();
            }
        }

        connections?.Tick();
        playerManager.updateAllChunks();

        foreach (EntityTracker t in entityTrackers)
        {
            t.tick();
        }

        try
        {
            RunPendingCommands();
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
            commandHandler.ExecuteCommand(cmd);
        }
    }

    public abstract FileInfo GetFile(string path);

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
}
