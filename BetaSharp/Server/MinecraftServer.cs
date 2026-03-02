using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server.Commands;
using BetaSharp.Server.Entities;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Network;
using BetaSharp.Server.Worlds;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Storage;
using java.lang;
using java.util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using Exception = System.Exception;

namespace BetaSharp.Server;

public abstract class MinecraftServer : Runnable, CommandOutput
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
    public string progressMessage;
    public int progress;
    private readonly Queue<Command> _pendingCommands = new();
    private readonly object _pendingCommandsLock = new();
    public EntityTracker[] entityTrackers = new EntityTracker[2];
    public bool onlineMode;
    public bool spawnAnimals;
    public bool pvpEnabled;
    public bool flightEnabled;
    protected bool logHelp = true;

    private readonly ILogger<MinecraftServer> _logger = Log.Instance.For<MinecraftServer>();
    private readonly Lock _tpsLock = new();
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

    public MinecraftServer(IServerConfiguration config)
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
        long startTime = java.lang.System.nanoTime();
        string worldName = config.GetLevelName("world");
        string seedString = config.GetLevelSeed("");
        long seed = new java.util.Random().nextLong();
        if (seedString.Length > 0)
        {
            try
            {
                seed = Long.parseLong(seedString);
            }
            catch (NumberFormatException)
            {
                // Java based string hashing
                int hash = 0;
                foreach (char c in seedString)
                {
                    hash = 31 * hash + c;
                }
                seed = hash;
            }
        }

        _logger.LogInformation($"Preparing level \"{worldName}\"");
        loadWorld(new RegionWorldStorageSource(getFile(".").getAbsolutePath()), worldName, seed);

        if (logHelp)
        {
            _logger.LogInformation($"Done ({java.lang.System.nanoTime() - startTime}ns)! For help, type \"help\" or \"?\"");
        }

        return true;
    }

    private void loadWorld(IWorldStorageSource storageSource, string worldDir, long seed)
    {
        worlds = new ServerWorld[2];
        RegionWorldStorage worldStorage = new RegionWorldStorage(getFile(".").getAbsolutePath(), worldDir, true);

        for (int i = 0; i < worlds.Length; i++)
        {
            if (i == 0)
            {
                worlds[i] = new ServerWorld(this, worldStorage, worldDir, i == 0 ? 0 : -1, seed);
            }
            else
            {
                worlds[i] = new ReadOnlyServerWorld(this, worldStorage, worldDir, i == 0 ? 0 : -1, seed, worlds[0]);
            }

            worlds[i].addWorldAccess(new ServerWorldEventListener(this, worlds[i]));
            worlds[i].difficulty = config.GetSpawnMonsters(true) ? 1 : 0;
            worlds[i].allowSpawning(config.GetSpawnMonsters(true), spawnAnimals);
            playerManager.saveAllPlayers(worlds);
        }

        int startRegionSize = config.GetSpawnRegionSize(196);
        long lastTimeLogged = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (int i = 0; i < worlds.Length; i++)
        {
            _logger.LogInformation($"Preparing start region for level {i}");
            // Only pre-generate the overworld spawn region. The nether is only accessible
            // via portal (which implies a teleport/load anyway), so on-demand generation
            // there is fine and avoids the 40+ second lava-sea light propagation cost.
            if (i == 0)
            {
                ServerWorld world = worlds[i];
                Vec3i spawnPos = world.getSpawnPos();

                var chunkList = new List<(int cx, int cz)>();
                for (int x = -startRegionSize; x <= startRegionSize; x += 16)
                    for (int z = -startRegionSize; z <= startRegionSize; z += 16)
                        chunkList.Add(((spawnPos.X + x) >> 4, (spawnPos.Z + z) >> 4));
                int totalChunks = chunkList.Count;
                var preGenerated = new Chunk[totalChunks];

                // Phase 1: Parallel terrain generation
                var sw1 = Stopwatch.StartNew();
                var threadLocalGen = new ThreadLocal<ChunkSource>(
                    () => world.chunkCache.CreateParallelGenerator(), trackAllValues: false);
                Parallel.For(0, totalChunks, idx =>
                {
                    if (!running) return;
                    var (cx, cz) = chunkList[idx];
                    preGenerated[idx] = threadLocalGen.Value!.GetChunk(cx, cz);
                });
                threadLocalGen.Dispose();
                sw1.Stop();
                _logger.LogInformation($"  Level {i} terrain: {sw1.ElapsedMilliseconds}ms");

                // Phase 2: Sequential insert + decoration
                var sw2 = Stopwatch.StartNew();
                for (int idx = 0; idx < totalChunks && running; idx++)
                {
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (currentTime > lastTimeLogged + 1000L)
                    {
                        logProgress("Preparing spawn area", (idx + 1) * 100 / totalChunks);
                        lastTimeLogged = currentTime;
                    }
                    var (cx, cz) = chunkList[idx];
                    world.chunkCache.InsertPreGeneratedChunk(cx, cz, preGenerated[idx]);
                    world.chunkCache.DecorateIfReady(cx, cz);
                }
                sw2.Stop();
                _logger.LogInformation($"  Level {i} decoration: {sw2.ElapsedMilliseconds}ms");

                // Phase 3: Batch lighting drain — all neighbors already loaded so sky-light
                // propagates without border re-queuing.
                var sw3 = Stopwatch.StartNew();
                while (world.doLightingUpdates() && running) { }
                sw3.Stop();
                _logger.LogInformation($"  Level {i} lighting: {sw3.ElapsedMilliseconds}ms");
            }
        }

        clearProgress();
    }

    private void logProgress(string progressType, int progress)
    {
        progressMessage = progressType;
        this.progress = progress;
        _logger.LogInformation($"{progressType}: {progress}%");
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
            world.saveWithLoadingDisplay(true, null);
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

        if (playerManager != null)
        {
            playerManager.savePlayers();
        }

        foreach (ServerWorld world in worlds)
        {
            if (world != null)
            {
                saveWorlds();
                break;
            }
        }
    }

    public void stop()
    {
        running = false;
    }

    public void run()
    {
        try
        {
            if (Init())
            {
                long lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
                long accumulatedTime = 0L;
                _lastTpsTime = lastTime;
                _ticksThisSecond = 0;

                while (running)
                {
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
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
                        continue;
                    }

                    if (worlds[0].canSkipNight())
                    {
                        tick();
                        _ticksThisSecond++;
                        accumulatedTime = 0L;
                    }
                    else
                    {
                        while (accumulatedTime > 50L)
                        {
                            accumulatedTime -= 50L;
                            tick();
                            _ticksThisSecond++;
                        }
                    }

                    long tpsNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
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

                    java.lang.Thread.sleep(1L);
                }
            }
            else
            {
                while (running)
                {
                    runPendingCommands();

                    try
                    {
                        java.lang.Thread.sleep(10L);
                    }
                    catch (InterruptedException ex)
                    {
                        ex.printStackTrace();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception");

            while (running)
            {
                runPendingCommands();

                try
                {
                    java.lang.Thread.sleep(10L);
                }
                catch (InterruptedException interruptedEx)
                {
                    interruptedEx.printStackTrace();
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
            catch (Throwable ex)
            {
                ex.printStackTrace();
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
                    playerManager.sendToDimension(new WorldTimeUpdateS2CPacket(world.getTime()), world.dimension.Id);
                }

                world.Tick();

                // Cap lighting updates to avoid spending the entire tick (and beyond)
                // draining the queue.  The nether's lava seas can generate thousands
                // of lighting entries per tick; processing them all in one go causes
                // >2-second stalls and "Can't keep up" spam.  Any remaining work
                // carries over and is processed across subsequent ticks.
                var lightSw = Stopwatch.StartNew();
                while (lightSw.ElapsedMilliseconds < 15L && world.doLightingUpdates())
                {
                }

                world.tickEntities();
            }
        }

        if (connections != null)
        {
            connections.Tick();
        }
        playerManager.updateAllChunks();

        foreach (EntityTracker t in entityTrackers)
        {
            t.tick();
        }

        try
        {
            runPendingCommands();
        }
        catch (Exception e)
        {
            _logger.LogWarning($"Unexpected exception while parsing console command: {e}");
        }
    }

    public void queueCommands(string str, CommandOutput cmd)
    {
        lock (_pendingCommandsLock)
        {
            _pendingCommands.Enqueue(new Command(str, cmd));
        }
    }

    public void runPendingCommands()
    {
        while (true)
        {
            Command cmd;
            lock (_pendingCommandsLock)
            {
                if (_pendingCommands.Count == 0) break;
                cmd = _pendingCommands.Dequeue();
            }
            commandHandler.ExecuteCommand(cmd);
        }
    }

    public abstract java.io.File getFile(string path);

    public void SendMessage(string message)
    {
        _logger.LogInformation(message);
    }

    public void Warn(string message)
    {
        _logger.LogWarning(message);
    }

    public string GetName()
    {
        return "CONSOLE";
    }

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
