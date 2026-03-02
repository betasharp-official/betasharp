using System.Diagnostics;
using System.Threading;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Server.Commands;
using BetaSharp.Server.Entities;
using BetaSharp.Server.Internal;
using BetaSharp.Server.Network;
using BetaSharp.Server.Worlds;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

public abstract class MinecraftServer : CommandOutput
{
    public Dictionary<string, int> GIVE_COMMANDS_COOLDOWNS = [];
    public ConnectionListener Connections;
    public IServerConfiguration Config;
    public ServerWorld[] Worlds;
    public PlayerManager PlayerManager;
    private ServerCommandHandler commandHandler;
    public bool Running = true;
    public bool Stopped;
    private int ticks;
    public string ProgressMessage;
    public int Progress;
    private readonly List<Command> _pendingCommands = [];
    private readonly object _pendingCommandsLock = new();
    public EntityTracker[] EntityTrackers = new EntityTracker[2];
    public bool OnlineMode;
    public bool SpawnAnimals;
    public bool PvpEnabled;
    public bool FlightEnabled;
    protected bool LogHelp = true;

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

    public MinecraftServer(IServerConfiguration Config)
    {
        this.Config = Config;
    }

    protected virtual bool Init()
    {
        commandHandler = new ServerCommandHandler(this);

        OnlineMode = Config.GetOnlineMode(true);
        SpawnAnimals = Config.GetSpawnAnimals(true);
        PvpEnabled = Config.GetPvpEnabled(true);
        FlightEnabled = Config.GetAllowFlight(false);

        PlayerManager = CreatePlayerManager();
        EntityTrackers[0] = new EntityTracker(this, 0);
        EntityTrackers[1] = new EntityTracker(this, -1);
        long startTimestamp = Stopwatch.GetTimestamp();
        string worldName = Config.GetLevelName("world");
        string seedString = Config.GetLevelSeed("");
        long seed = Random.Shared.NextInt64();
        if (seedString.Length > 0)
        {
            try
            {
                seed = long.Parse(seedString);
            }
            catch (FormatException)
            {
                // Java-compatible string hashing
                int hash = 0;
                foreach (char c in seedString)
                {
                    hash = 31 * hash + c;
                }
                seed = hash;
            }
        }

        _logger.LogInformation($"Preparing level \"{worldName}\"");
        loadWorld(new RegionWorldStorageSource(GetFilePath(".")), worldName, seed);

        if (LogHelp)
        {
            long elapsedNs = (long)((Stopwatch.GetTimestamp() - startTimestamp) * (1_000_000_000.0 / Stopwatch.Frequency));
            _logger.LogInformation($"Done ({elapsedNs}ns)! For help, type \"help\" or \"?\"");
        }

        return true;
    }

    private void loadWorld(IWorldStorageSource storageSource, string worldDir, long seed)
    {
        Worlds = new ServerWorld[2];
        RegionWorldStorage worldStorage = new RegionWorldStorage(GetFilePath("."), worldDir, true);

        for (int i = 0; i < Worlds.Length; i++)
        {
            if (i == 0)
            {
                Worlds[i] = new ServerWorld(this, worldStorage, worldDir, i == 0 ? 0 : -1, seed);
            }
            else
            {
                Worlds[i] = new ReadOnlyServerWorld(this, worldStorage, worldDir, i == 0 ? 0 : -1, seed, Worlds[0]);
            }

            Worlds[i].addWorldAccess(new ServerWorldEventListener(this, Worlds[i]));
            Worlds[i].difficulty = Config.GetSpawnMonsters(true) ? 1 : 0;
            Worlds[i].allowSpawning(Config.GetSpawnMonsters(true), SpawnAnimals);
            PlayerManager.SaveAllPlayers(Worlds);
        }

        short startRegionSize = 196;
        long lastTimeLogged = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;

        for (int i = 0; i < Worlds.Length; i++)
        {
            _logger.LogInformation($"Preparing start region for level {i}");
            if (i == 0 || Config.GetAllowNether(true))
            {
                ServerWorld world = Worlds[i];
                Vec3i spawnPos = world.getSpawnPos();

                for (int x = -startRegionSize; x <= startRegionSize && Running; x += 16)
                {
                    for (int z = -startRegionSize; z <= startRegionSize && Running; z += 16)
                    {
                        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
                        if (currentTime < lastTimeLogged)
                        {
                            lastTimeLogged = currentTime;
                        }

                        if (currentTime > lastTimeLogged + 1000L)
                        {
                            int total = (startRegionSize * 2 + 1) * (startRegionSize * 2 + 1);
                            int complete = (x + startRegionSize) * (startRegionSize * 2 + 1) + z + 1;
                            logProgress("Preparing spawn area", complete * 100 / total);
                            lastTimeLogged = currentTime;
                        }

                        world.chunkCache.LoadChunk(spawnPos.X + x >> 4, spawnPos.Z + z >> 4);

                        while (world.doLightingUpdates() && Running)
                        {
                        }
                    }
                }
            }
        }

        clearProgress();
    }

    private void logProgress(string progressType, int progress)
    {
        ProgressMessage = progressType;
        this.Progress = progress;
        _logger.LogInformation($"{progressType}: {progress}%");
    }

    private void clearProgress()
    {
        ProgressMessage = null;
        Progress = 0;
    }

    private void saveWorlds()
    {
        _logger.LogInformation("Saving chunks");

        foreach (ServerWorld world in Worlds)
        {
            world.saveWithLoadingDisplay(true, null);
            world.forceSave();
        }
    }

    private void shutdown()
    {
        if (Stopped)
        {
            return;
        }

        _logger.LogInformation("Stopping server");

        if (PlayerManager != null)
        {
            PlayerManager.SavePlayers();
        }

        foreach (ServerWorld world in Worlds)
        {
            if (world != null)
            {
                saveWorlds();
            }
        }
    }

    public void Stop()
    {
        Running = false;
    }

    public void Run()
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

                while (Running)
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

                    if (Worlds[0].canSkipNight())
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

                    Thread.Sleep(1);
                }
            }
            else
            {
                while (Running)
                {
                    RunPendingCommands();
                    Thread.Sleep(10);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception");

            while (Running)
            {
                RunPendingCommands();
                Thread.Sleep(10);
            }
        }
        finally
        {
            try
            {
                shutdown();
                Stopped = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during shutdown");
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
        List<string> completeCooldowns = [];

        foreach (string key in GIVE_COMMANDS_COOLDOWNS.Keys.ToList())
        {
            if (GIVE_COMMANDS_COOLDOWNS[key] > 0)
            {
                GIVE_COMMANDS_COOLDOWNS[key]--;
            }
            else
            {
                completeCooldowns.Add(key);
            }
        }

        foreach (string key in completeCooldowns)
        {
            GIVE_COMMANDS_COOLDOWNS.Remove(key);
        }

        ticks++;

        for (int i = 0; i < Worlds.Length; i++)
        {
            if (i == 0 || Config.GetAllowNether(true))
            {
                ServerWorld world = Worlds[i];
                if (ticks % 20 == 0)
                {
                    PlayerManager.SendToDimension(new WorldTimeUpdateS2CPacket(world.getTime()), world.dimension.Id);
                }

                world.Tick();

                while (world.doLightingUpdates())
                {
                }

                world.tickEntities();
            }
        }

        if (Connections != null)
        {
            Connections.Tick();
        }
        PlayerManager.UpdateAllChunks();

        foreach (EntityTracker t in EntityTrackers)
        {
            t.Tick();
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

    public void QueueCommands(string str, CommandOutput cmd)
    {
        lock (_pendingCommandsLock)
        {
            _pendingCommands.Add(new Command(str, cmd));
        }
    }

    public void RunPendingCommands()
    {
        while (true)
        {
            Command cmd;
            lock (_pendingCommandsLock)
            {
                if (_pendingCommands.Count == 0) break;
                cmd = _pendingCommands[0];
                _pendingCommands.RemoveAt(0);
            }
            commandHandler.ExecuteCommand(cmd);
        }
    }

    public abstract string GetFilePath(string path);

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

    public ServerWorld GetWorld(int dimensionId)
    {
        return dimensionId == -1 ? Worlds[1] : Worlds[0];
    }

    public EntityTracker GetEntityTracker(int dimensionId)
    {
        return dimensionId == -1 ? EntityTrackers[1] : EntityTrackers[0];
    }
    protected virtual PlayerManager CreatePlayerManager()
    {
        return new PlayerManager(this);
    }

}
