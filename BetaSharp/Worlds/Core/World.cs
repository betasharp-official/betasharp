using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.PathFinding;
using BetaSharp.Profiling;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace BetaSharp.Worlds.Core;

public abstract class World : IWorldContext
{
    private static readonly int s_autosavePeriod = 40;

    private readonly HashSet<ChunkPos> _activeChunks = new();
    private readonly ILogger<World> _logger = Log.Instance.For<World>();

    public readonly Dimension Dimension;

    private int _lcgBlockSeed = System.Random.Shared.Next();
    private int _soundCounter = System.Random.Shared.Next(12000);
    private bool _spawnHostileMobs = true;
    private bool _spawnPeacefulMobs = true;

    protected int AutosavePeriod = s_autosavePeriod;
    public bool EventProcessingEnabled;
    public bool IsNewWorld;

    protected World(IWorldStorage worldStorage, string levelName, WorldSettings settings, Dimension? dim = null)
    {
        Pathing = new PathFinder(this);
        Storage = worldStorage;
        StateManager = new PersistentStateManager(worldStorage);

        WorldProperties? loadedProperties = worldStorage.LoadProperties();
        bool shouldInitializeSpawn = loadedProperties == null;
        if (loadedProperties == null)
        {
            Properties = new WorldProperties(settings, levelName);
        }
        else
        {
            Properties = loadedProperties;
        }

        if (dim != null)
        {
            Dimension = dim;
        }
        else if (Properties != null && Properties.Dimension == -1)
        {
            Dimension = Dimension.FromId(-1);
        }
        else
        {
            Dimension = Dimension.FromId(0);
        }



        Properties!.LevelName = levelName;


        if (Dimension is OverworldDimension && Properties.TerrainType == WorldType.Sky)
        {
            Dimension = new SkyDimension();
        }

        shouldInitializeSpawn = shouldInitializeSpawn && this is ServerWorld && Dimension.Id == 0;

        Dimension.SetWorld(this);

        IChunkSource chunkSource = CreateChunkCache();

        Random = new JavaRandom();
        Rules = Properties.RulesTag != null
            ? RuleSet.FromNBT(RuleRegistry.Instance, Properties.RulesTag)
            : new RuleSet(RuleRegistry.Instance);

        BlockHost = new ChunkHost(chunkSource);
        Reader = new WorldReader(this, Dimension);
        Writer = new WorldWriter(BlockHost, Reader);
        Writer.OnBlockChanged += BlockUpdate;

        Broadcaster = new WorldEventBroadcaster(EventListeners, Reader, this);

        Writer.OnNeighborsShouldUpdate += (x, y, z, id) => Broadcaster.NotifyNeighbors(x, y, z, id);

        Redstone = new RedstoneEngine(Reader);
        Lighting = new LightingEngine(this);
        Lighting.OnLightUpdated += (x, y, z) => Broadcaster.BlockUpdateEvent(x, y, z);
        TickScheduler = new WorldTickScheduler(this);
        Environment = new EnvironmentManager(this);
        Entities = new EntityManager(this);

        Entities.OnBlockUpdateRequired += (x, y, z) => Broadcaster.BlockUpdateEvent(x, y, z);

        Environment.PrepareWeather();
        Environment.UpdateSkyBrightness();

        Entities.OnEntityAdded += ent =>
        {
            for (int i = 0; i < EventListeners.Count; ++i)
            {
                EventListeners[i].notifyEntityAdded(ent);
            }
        };
        Entities.OnEntityRemoved += ent =>
        {
            for (int i = 0; i < EventListeners.Count; ++i)
            {
                EventListeners[i].notifyEntityRemoved(ent);
            }
        };


        if (shouldInitializeSpawn)
        {
            InitializeSpawnPoint();
        }
    }

    public ChunkHost BlockHost { get; }
    public IBlockReader Reader { get; }
    public IBlockWriter Writer { get; }
    public WorldEventBroadcaster Broadcaster { get; }

    public EntityManager Entities { get; }
    public EnvironmentManager Environment { get; }

    public List<IWorldEventListener> EventListeners { get; } = [];
    public LightingEngine Lighting { get; }

    private PathFinder Pathing { get; }
    private RedstoneEngine Redstone { get; }
    protected IWorldStorage Storage { get; }
    public long Seed => Properties.RandomSeed;
    public WorldTickScheduler TickScheduler { get; }
    public int Difficulty { get; private set; }

    public PersistentStateManager StateManager { get; protected init; }

    public WorldProperties Properties { get; protected init; }
    public bool IsRemote { init; get; }
    public JavaRandom Random { get; }

    ChunkHost IWorldContext.ChunkHost => BlockHost;
    WorldEventBroadcaster IWorldContext.Broadcaster => Broadcaster;
    RedstoneEngine IWorldContext.Redstone => Redstone;
    EntityManager IWorldContext.Entities => Entities;
    LightingEngine IWorldContext.Lighting => Lighting;
    EnvironmentManager IWorldContext.Environment => Environment;
    Dimension IWorldContext.Dimension => Dimension;
    long IWorldContext.Seed => Properties.RandomSeed;
    PathFinder IWorldContext.Pathing => Pathing;

    public RuleSet Rules { get; protected set; }

    public bool SpawnEntity(Entity entity) => Entities.SpawnEntity(entity);

    public bool SpawnItemDrop(double x, double y, double z, ItemStack itemStack)
    {
        EntityItem droppedItem = new(this, x, y, z, itemStack)
        {
            delayBeforeCanPickup = 10
        };
        return Entities.SpawnEntity(droppedItem);
    }

    public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power) => CreateExplosion(source, x, y, z, power, false);

    public virtual Explosion CreateExplosion(Entity? source, double x, double y, double z, float power, bool fire)
    {
        Explosion explosion = new(this, source, x, y, z, power)
        {
            isFlaming = fire
        };
        explosion.doExplosionA();
        explosion.doExplosionB(true);
        return explosion;
    }

    public long GetTime() => Properties.WorldTime;

    public void SetDifficulty(int difficulty) => Difficulty = difficulty;

    public virtual bool CanInteract(EntityPlayer player, int x, int y, int z) => true;

    public BiomeSource GetBiomeSource() => Dimension.BiomeSource;

    public float GetLuminance(int x, int y, int z) => Lighting.GetLuminance(x, y, z);

    public IWorldStorage GetWorldStorage() => Storage;

    protected abstract IChunkSource CreateChunkCache();

    private void InitializeSpawnPoint()
    {
        EventProcessingEnabled = true;
        try
        {
            int x = 0;
            int z = 0;
            int y = 64;

            const int maxAttempts = 512;
            int attempts = 0;

            while (!Dimension.IsValidSpawnPoint(x, z) && attempts++ < maxAttempts)
            {
                x += Random.NextInt(64) - Random.NextInt(64);
                z += Random.NextInt(64) - Random.NextInt(64);
            }

            if (!Dimension.IsValidSpawnPoint(x, z))
            {
                x = 0;
                z = 0;
                UpdateSpawnPosition();
                x = Properties.SpawnX;
                z = Properties.SpawnZ;
            }

            if (Properties.TerrainType == WorldType.Sky)
            {
                int topY = Reader.GetTopSolidBlockY(x, z);
                if (topY > 0)
                {
                    y = topY;
                }
            }

            Properties.SetSpawn(x, y, z);
        }
        finally
        {
            EventProcessingEnabled = false;
        }
    }

    public virtual void UpdateSpawnPosition()
    {
        if (Properties.SpawnY <= 0)
        {
            Properties.SpawnY = 64;
        }

        int spawnX = Properties.SpawnX;

        int spawnZ;
        for (spawnZ = Properties.SpawnZ;
             GetSpawnBlockId(spawnX, spawnZ) == 0;
             spawnZ += Random.NextInt(8) - Random.NextInt(8))
        {
            spawnX += Random.NextInt(8) - Random.NextInt(8);
        }

        Properties.SpawnX = spawnX;
        Properties.SpawnZ = spawnZ;
    }

    public void SaveWorldData()
    {
    }

    public int GetSpawnBlockId(int x, int z)
    {
        int y;
        for (y = 63; !Reader.IsAir(x, y + 1, z); ++y)
        {
        }

        return Reader.GetBlockId(x, y, z);
    }

    public void AddPlayer(EntityPlayer player)
    {
        try
        {
            NBTTagCompound? tag = Properties.PlayerTag;
            if (tag != null)
            {
                player.read(tag);
                Properties.PlayerTag = null;
            }

            Entities.SpawnEntity(player);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    public void SaveWithLoadingDisplay(bool saveEntities, LoadingDisplay? loadingDisplay)
    {
        if (!BlockHost.ChunkSource.CanSave()) return;

        if (loadingDisplay != null)
        {
            loadingDisplay.progressStartNoAbort("Saving level");
        }

        Profiler.PushGroup("saveLevel");
        Save();
        Profiler.PopGroup();
        if (loadingDisplay != null)
        {
            loadingDisplay.progressStage("Saving chunks");
        }

        Profiler.Start("saveChunks");
        BlockHost.ChunkSource.Save(saveEntities, loadingDisplay);
        Profiler.Stop("saveChunks");
    }

    private void Save()
    {
        Profiler.Start("checkSessionLock");
        Profiler.Stop("checkSessionLock");
        Profiler.Start("saveWorldInfoAndPlayer");

        Properties.RulesTag = new NBTTagCompound();
        Rules.WriteToNBT(Properties.RulesTag);

        Storage.Save(Properties, Entities.Players.ToList());
        Profiler.Stop("saveWorldInfoAndPlayer");

        Profiler.Start("saveAllData");
        StateManager.SaveAllData();
        Profiler.Stop("saveAllData");
    }

    public bool AttemptSaving(int i)
    {
        if (!BlockHost.ChunkSource.CanSave())
        {
            return true;
        }

        if (i == 0)
        {
            Save();
        }

        return BlockHost.ChunkSource.Save(false, null);
    }

    public float GetTime(float partialTicks) => Dimension.GetTimeOfDay(Properties.WorldTime, partialTicks);

    protected void BlockUpdate(int x, int y, int z, int blockId)
    {
        Broadcaster.BlockUpdateEvent(x, y, z);
        Broadcaster.NotifyNeighbors(x, y, z, blockId);
    }

    public Vector3D<double> GetFogColor(float partialTicks)
    {
        float timeOfDay = GetTime(partialTicks);
        return Dimension.GetFogColor(timeOfDay, partialTicks);
    }

    public float CalculateSkyLightIntensity(float partialTicks)
    {
        float timeOfDay = GetTime(partialTicks);
        float intensityFactor = 1.0F - (MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 12.0F / 16.0F);
        intensityFactor = Math.Clamp(intensityFactor, 0.0F, 1.0F);

        return intensityFactor * intensityFactor * 0.5F;
    }

    public bool IsMaterialInBox(Box area, Func<Material, bool> predicate) => Reader.IsMaterialInBox(area, predicate);

    public void ExtinguishFire(EntityPlayer? player, int x, int y, int z, int direction)
    {
        if (direction == 0)
        {
            --y;
        }

        if (direction == 1)
        {
            ++y;
        }

        switch (direction)
        {
            case 2:
                --z;
                break;
            case 3:
                ++z;
                break;
            case 4:
                --x;
                break;
            case 5:
                ++x;
                break;
        }

        if (Reader.GetBlockId(x, y, z) == Block.Fire.id)
        {
            Broadcaster.WorldEvent(player, 1004, x, y, z, 0);
            Writer.SetBlock(x, y, z, 0);
        }
    }

    public Entity? GetPlayerForProxy(Type type) => null;

    public string GetDebugInfo() => BlockHost.ChunkSource.GetDebugInfo();

    public void SavingProgress(LoadingDisplay display) => SaveWithLoadingDisplay(true, display);

    public void allowSpawning(bool allowMonsterSpawning, bool allowMobSpawning)
    {
        _spawnHostileMobs = allowMonsterSpawning;
        _spawnPeacefulMobs = allowMobSpawning;
    }

    public virtual void Tick()
    {
        TickScheduler.Tick();
        Environment.UpdateWeatherCycles();

        long nextWorldTime;

        if (!IsRemote && Entities.AreAllPlayersAsleep())
        {
            bool wasSpawnInterrupted = false;

            if (_spawnHostileMobs && Difficulty >= 1)
            {
                wasSpawnInterrupted = NaturalSpawner.SpawnMonstersAndWakePlayers(this, Entities.Players);
            }

            if (!wasSpawnInterrupted)
            {
                Environment.SkipNightAndClearWeather();
                Entities.WakeAllPlayers();
            }
        }

        Profiler.Start("performSpawning");
        NaturalSpawner.DoSpawning(this, Pathing, _spawnHostileMobs, _spawnPeacefulMobs);
        Profiler.Stop("performSpawning");

        Profiler.Start("unload100OldestChunks");
        BlockHost.ChunkSource.Tick();
        Profiler.Stop("unload100OldestChunks");

        Profiler.Start("updateSkylightSubtracted");
        int currentAmbientDarkness = Environment.GetAmbientDarkness(1.0F);
        if (currentAmbientDarkness != Environment.AmbientDarkness)
        {
            Environment.AmbientDarkness = currentAmbientDarkness;

            for (int i = 0; i < EventListeners.Count; ++i)
            {
                EventListeners[i].notifyAmbientDarknessChanged();
            }
        }

        Profiler.Stop("updateSkylightSubtracted");

        nextWorldTime = Properties.WorldTime + 1L;
        if (nextWorldTime % AutosavePeriod == 0L)
        {
            Profiler.PushGroup("autosave");
            SaveWithLoadingDisplay(false, null);
            Profiler.PopGroup();
        }

        Properties.WorldTime = nextWorldTime;

        Profiler.Start("tickUpdates");
        TickScheduler.Tick();
        Profiler.Stop("tickUpdates");

        ManageChunkUpdatesAndEvents();
    }

    protected virtual void ManageChunkUpdatesAndEvents()
    {
        _activeChunks.Clear();

        for (int i = 0; i < Entities.Players.Count; ++i)
        {
            EntityPlayer player = Entities.Players[i];
            int playerChunkX = MathHelper.Floor(player.x / 16.0D);
            int playerChunkZ = MathHelper.Floor(player.z / 16.0D);
            const byte viewDistance = 9;

            for (int xOffset = -viewDistance; xOffset <= viewDistance; ++xOffset)
            {
                for (int zOffset = -viewDistance; zOffset <= viewDistance; ++zOffset)
                {
                    _activeChunks.Add(new ChunkPos(xOffset + playerChunkX, zOffset + playerChunkZ));
                }
            }
        }

        if (_soundCounter > 0)
        {
            --_soundCounter;
        }

        foreach (ChunkPos chunkPos in _activeChunks)
        {
            int worldXBase = chunkPos.X * 16;
            int worldZBase = chunkPos.Z * 16;
            Chunk currentChunk = BlockHost.GetChunk(chunkPos.X, chunkPos.Z);

            if (_soundCounter == 0)
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomVal = _lcgBlockSeed >> 2;
                int localX = randomVal & 15;
                int localZ = (randomVal >> 8) & 15;
                int localY = (randomVal >> 16) & 127;

                int blockId = currentChunk.GetBlockId(localX, localY, localZ);
                int worldX = localX + worldXBase;
                int worldZ = localZ + worldZBase;
                if (blockId == 0 && Reader.GetBrightness(worldX, localY, worldZ) <= Random.NextInt(8) &&
                    Lighting.GetBrightness(LightType.Sky, worldX, localY, worldZ) <= 0)
                {
                    EntityPlayer? closest = Entities.GetClosestPlayer(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D, 8.0D);
                    if (closest != null &&
                        closest.getSquaredDistance(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D) > 4.0D)
                    {
                        Broadcaster.PlaySoundAtPos(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D, "ambient.cave.cave", 0.7F,
                            0.8F + Random.NextFloat() * 0.2F);
                        _soundCounter = Random.NextInt(12000) + 6000;
                    }
                }
            }

            if (Random.NextInt(100000) == 0 && Environment.IsRaining && Environment.IsThundering())
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomVal = _lcgBlockSeed >> 2;
                int worldX = worldXBase + (randomVal & 15);
                int worldZ = worldZBase + ((randomVal >> 8) & 15);
                int worldY = Reader.GetTopSolidBlockY(worldX, worldZ);

                if (Environment.IsRainingAt(worldX, worldY, worldZ))
                {
                    Entities.SpawnGlobalEntity(new EntityLightningBolt(this, worldX, worldY, worldZ));
                    Environment.LightningTicksLeft = 2;
                }
            }

            if (Random.NextInt(16) == 0)
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomVal = _lcgBlockSeed >> 2;
                int localX = randomVal & 15;
                int localZ = (randomVal >> 8) & 15;
                int worldX = localX + worldXBase;
                int worldZ = localZ + worldZBase;
                int worldY = Reader.GetTopSolidBlockY(worldX, worldZ);

                if (GetBiomeSource().GetBiome(worldX, worldZ).GetEnableSnow() && worldY >= 0 && worldY < 128 &&
                    currentChunk.GetLight(LightType.Block, localX, worldY, localZ) < 10)
                {
                    int blockBelowId = currentChunk.GetBlockId(localX, worldY - 1, localZ);
                    int currentBlockId = currentChunk.GetBlockId(localX, worldY, localZ);

                    if (Environment.IsRaining && currentBlockId == 0 && Block.Snow.canPlaceAt(new CanPlaceAtContext(this, 1, worldX, worldY, worldZ)) &&
                        blockBelowId != 0 && blockBelowId != Block.Ice.id &&
                        Block.Blocks[blockBelowId].material.BlocksMovement)
                    {
                        Writer.SetBlock(worldX, worldY, worldZ, Block.Snow.id);
                    }

                    if (blockBelowId == Block.Water.id && currentChunk.GetBlockMeta(localX, worldY - 1, localZ) == 0)
                    {
                        Writer.SetBlock(worldX, worldY - 1, worldZ, Block.Ice.id);
                    }
                }
            }

            for (int j = 0; j < 80; ++j)
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomTickVal = _lcgBlockSeed >> 2;
                int localX = randomTickVal & 15;
                int localZ = (randomTickVal >> 8) & 15;
                int localY = (randomTickVal >> 16) & 127;

                int blockId = currentChunk.Blocks[(localX << 11) | (localZ << 7) | localY] & 255;
                if (Block.BlocksRandomTick[blockId])
                {
                    Block.Blocks[blockId].onTick(new OnTickEvent(this, localX + worldXBase, localY, localZ + worldZBase, currentChunk.GetBlockMeta(localX, localY, localZ), blockId));
                }
            }
        }
    }

    public void displayTick(int centerX, int centerY, int centerZ)
    {
        const byte searchRadius = 16;

        for (int i = 0; i < 1000; ++i)
        {
            int targetX = centerX + Random.NextInt(searchRadius) - Random.NextInt(searchRadius);
            int targetY = centerY + Random.NextInt(searchRadius) - Random.NextInt(searchRadius);
            int targetZ = centerZ + Random.NextInt(searchRadius) - Random.NextInt(searchRadius);

            int blockId = Reader.GetBlockId(targetX, targetY, targetZ);
            if (blockId > 0)
            {
                Block.Blocks[blockId].randomDisplayTick(new OnTickEvent(this, targetX, targetY, targetZ, Reader.GetBlockMeta(targetX, targetY, targetZ), blockId));
            }
        }
    }

    public void TickChunks()
    {
        while (BlockHost.ChunkSource.Tick())
        {
        }
    }


    public void HandleChunkDataUpdate(int x, int y, int z, int sizeX, int sizeY, int sizeZ, byte[] chunkData)
    {
        int startChunkX = x >> 4;
        int startChunkZ = z >> 4;
        int endChunkX = (x + sizeX - 1) >> 4;
        int endChunkZ = (z + sizeZ - 1) >> 4;

        int currentBufferOffset = 0;
        int minY = Math.Max(0, y);
        int maxY = Math.Min(128, y + sizeY);

        for (int chunkX = startChunkX; chunkX <= endChunkX; ++chunkX)
        {
            int localStartX = Math.Max(0, x - chunkX * 16);
            int localEndX = Math.Min(16, x + sizeX - chunkX * 16);

            for (int chunkZ = startChunkZ; chunkZ <= endChunkZ; ++chunkZ)
            {
                int localStartZ = Math.Max(0, z - chunkZ * 16);
                int localEndZ = Math.Min(16, z + sizeZ - chunkZ * 16);

                currentBufferOffset = BlockHost.GetChunk(chunkX, chunkZ).LoadFromPacket(
                    chunkData,
                    localStartX, minY, localStartZ,
                    localEndX, maxY, localEndZ,
                    currentBufferOffset);

                setBlocksDirty(
                    chunkX * 16 + localStartX, minY, chunkZ * 16 + localStartZ,
                    chunkX * 16 + localEndX, maxY, chunkZ * 16 + localEndZ);
            }
        }
    }

    public virtual void Disconnect()
    {
    }

    public void SetTime(long time) => Properties.WorldTime = time;

    public long GetSeed() => Properties.RandomSeed;

    public void SetSpawnPos(Vec3i pos) => Properties.SetSpawn(pos.X, pos.Y, pos.Z);

    public void setBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].setBlocksDirty(minX, minY, minZ, maxX, maxY, maxZ);
        }
    }
}
