using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.PathFinding;
using BetaSharp.Profiling;
using BetaSharp.Rules;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Generation.Biomes.Source;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace BetaSharp.Worlds.Core;

public abstract class World : IBlockWorldContext
{
    private static readonly int s_autosavePeriod = 40;

    private readonly HashSet<ChunkPos> _activeChunks = new();
    private readonly ILogger<World> _logger = Log.Instance.For<World>();

    private readonly PathFinder _pathFinder;
    private readonly PriorityQueue<BlockUpdate, (long, long)> _scheduledUpdates = new();

    private readonly long _worldTimeMask = 0xFFFFFFL;
    public readonly Dimension dimension;

    protected readonly List<IWorldAccess> EventListeners = [];

    public readonly EntityManager Entities;
    public readonly WorldTickScheduler TickScheduler;
    public readonly LightingEngine Lighting;
    public readonly EnvironmentManager Environment;
    private readonly RedstoneEngine Redstone;
    private readonly BlockHost _blockHost;
    public readonly WorldBlockView BlocksReader;
    public readonly WorldBlockWrite BlockWriter;
    protected readonly IWorldStorage Storage;
    public readonly WorldEventBroadcaster WorldEventBroadcaster;

    private int _lcgBlockSeed = Random.Shared.Next();
    private int _soundCounter = Random.Shared.Next(12000);
    private bool _spawnHostileMobs = true;
    private bool _spawnPeacefulMobs = true;

    protected int AutosavePeriod = s_autosavePeriod;
    public int difficulty;
    public bool EventProcessingEnabled;
    public bool IsNewWorld;
    public bool isRemote { set; get; } = false;

    public bool PauseTicking { get => BlocksReader.PauseTicking; set => BlocksReader.PauseTicking = value; }

    public bool InstantBlockUpdateEnabled { get => TickScheduler.instantBlockUpdateEnabled; set => TickScheduler.instantBlockUpdateEnabled = value; }

    public PersistentStateManager PersistentStateManager;

    public WorldProperties Properties { get; protected set; }
    public JavaRandom random { get; } = new();

    protected World(IWorldStorage worldStorage, string levelName, Dimension dim, long seed)
    {
        _pathFinder = new PathFinder(this);
        Storage = worldStorage;
        PersistentStateManager = new PersistentStateManager(worldStorage);
        Properties = new WorldProperties(seed, levelName);
        dim.SetWorld(this);

        IChunkSource chunkSource = CreateChunkCache();

        Rules = Properties.RulesTag != null
            ? RuleSet.FromNBT(RuleRegistry.Instance, Properties.RulesTag)
            : new RuleSet(RuleRegistry.Instance);

        _blockHost = new BlockHost(chunkSource, dim, EventListeners);
        BlockWriter = new WorldBlockWrite(_blockHost);
        BlockWriter.OnBlockChanged += this.BlockUpdate;
        BlockWriter.OnNeighborsShouldUpdate += notifyNeighbors;
        BlocksReader = new WorldBlockView(_blockHost, BlockWriter, isRemote, this);
        WorldEventBroadcaster = new WorldEventBroadcaster(EventListeners);

        Redstone = new RedstoneEngine(BlocksReader);
        Lighting = new LightingEngine(BlocksReader, dim);
        Lighting.OnLightUpdated += (x, y, z) => blockUpdateEvent(x, y, z);

        TickScheduler = new WorldTickScheduler(BlocksReader, random, isRemote, WorldEventBroadcaster);

        Environment = new EnvironmentManager(Properties, dim, BlocksReader, random);
        Entities = new EntityManager(BlocksReader, Rules);

        Entities.OnBlockUpdateRequired += (x, y, z) => blockUpdateEvent(x, y, z);

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
    }

    protected World(IWorldStorage worldStorage, string levelName, long seed, Dimension? dim)
    {
        _pathFinder = new PathFinder(this);
        Storage = worldStorage;
        PersistentStateManager = new PersistentStateManager(worldStorage);

        Properties = worldStorage.LoadProperties();
        IsNewWorld = Properties == null;
        bool shouldInitializeSpawn = IsNewWorld;
        if (IsNewWorld)
        {
            Properties = new WorldProperties(seed, levelName);
        }
        else
        {
            Properties.LevelName = levelName;
        }

        Dimension dimension = dim ?? Dimension.FromId(Properties.Dimension == -1 ? -1 : 0);
        dimension.SetWorld(this);

        IChunkSource chunkSource = CreateChunkCache();

        Rules = Properties.RulesTag != null
            ? RuleSet.FromNBT(RuleRegistry.Instance, Properties.RulesTag)
            : new RuleSet(RuleRegistry.Instance);

        _blockHost = new BlockHost(chunkSource);
        BlockWriter = new WorldBlockWrite(_blockHost);
        BlockWriter.OnBlockChanged += BlockUpdate;
        BlockWriter.OnNeighborsShouldUpdate += notifyNeighbors;
        BlocksReader = new WorldBlockView(_blockHost, this);
        WorldEventBroadcaster = new WorldEventBroadcaster(EventListeners);

        Redstone = new RedstoneEngine(BlocksReader);
        Lighting = new LightingEngine(BlocksReader, dimension);
        Lighting.OnLightUpdated += (x, y, z) => blockUpdateEvent(x, y, z);

        TickScheduler = new WorldTickScheduler(BlocksReader, random, isRemote, WorldEventBroadcaster);

        Environment = new EnvironmentManager(Properties, dimension, BlocksReader, random);
        Entities = new EntityManager(BlocksReader, Rules);

        Entities.OnBlockUpdateRequired += (x, y, z) => blockUpdateEvent(x, y, z);

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

    public RuleSet Rules { get; protected set; }


    public BiomeSource GetBiomeSource() => dimension.BiomeSource;

    public float GetNaturalBrightness(int x, int y, int z, int blockLight) => Lighting.GetNaturalBrightness(x, y, z, blockLight);

    public float GetLuminance(int x, int y, int z) => Lighting.getLuminance(x, y, z);

    public IWorldStorage GetWorldStorage() => Storage;

    protected abstract IChunkSource CreateChunkCache();

    private void InitializeSpawnPoint()
    {
        EventProcessingEnabled = true;
        int x = 0;
        byte y = 64;

        int z;
        for (
            z = 0;
            !dimension.IsValidSpawnPoint(x, z);
            z += random.NextInt(64) - random.NextInt(64))
        {
            x += random.NextInt(64) - random.NextInt(64);
        }

        Properties.SetSpawn(x, y, z);
        EventProcessingEnabled = false;
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
             spawnZ += random.NextInt(8) - random.NextInt(8))
        {
            spawnX += random.NextInt(8) - random.NextInt(8);
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
        for (y = 63; !BlocksReader.IsAir(x, y + 1, z); ++y)
        {
        }

        return BlocksReader.GetBlockId(x, y, z);
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
        if (_blockHost.ChunkSource.CanSave())
        {
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
            _blockHost.ChunkSource.Save(saveEntities, loadingDisplay);
            Profiler.Stop("saveChunks");
        }
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
        PersistentStateManager.SaveAllData();
        Profiler.Stop("saveAllData");
    }

    public bool AttemptSaving(int i)
    {
        if (_blockHost.ChunkSource.CanSave())
        {
            return true;
        }

        if (i == 0)
        {
            Save();
        }

        return _blockHost.ChunkSource.Save(false, null);
    }

    public bool canMonsterSpawn() => Environment.AmbientDarkness < 4;

    public HitResult raycast(Vec3D start, Vec3D end) => raycast(start, end, false, false);

    public HitResult raycast(Vec3D start, Vec3D end, bool bl) => raycast(start, end, bl, false);

    public HitResult raycast(Vec3D start, Vec3D target, bool includeFluids, bool ignoreNonSolid)
    {
        if (double.IsNaN(start.x) || double.IsNaN(start.y) || double.IsNaN(start.z) ||
            double.IsNaN(target.x) || double.IsNaN(target.y) || double.IsNaN(target.z))
        {
            return new HitResult(HitResultType.MISS);
        }

        int targetX = MathHelper.Floor(target.x);
        int targetY = MathHelper.Floor(target.y);
        int targetZ = MathHelper.Floor(target.z);
        int currentX = MathHelper.Floor(start.x);
        int currentY = MathHelper.Floor(start.y);
        int currentZ = MathHelper.Floor(start.z);

        int initialId = BlocksReader.GetBlockId(currentX, currentY, currentZ);
        int initialMeta = BlocksReader.GetBlockMeta(currentX, currentY, currentZ);
        Block? initialBlock = Block.Blocks[initialId];

        if ((!ignoreNonSolid || initialBlock == null ||
             initialBlock.getCollisionShape(BlocksReader, currentX, currentY, currentZ) != null) &&
            initialId > 0 && initialBlock.hasCollision(initialMeta, includeFluids))
        {
            HitResult result = initialBlock.raycast(BlocksReader, currentX, currentY, currentZ, start, target);
            if (result.Type != HitResultType.MISS)
            {
                return result;
            }
        }

        int iterationsRemaining = 200;
        while (iterationsRemaining-- >= 0)
        {
            if (double.IsNaN(start.x) || double.IsNaN(start.y) || double.IsNaN(start.z))
            {
                return new HitResult(HitResultType.MISS);
            }

            if (currentX == targetX && currentY == targetY && currentZ == targetZ)
            {
                return new HitResult(HitResultType.MISS);
            }

            bool canMoveX = true, canMoveY = true, canMoveZ = true;
            double nextBoundaryX = 999.0D, nextBoundaryY = 999.0D, nextBoundaryZ = 999.0D;

            if (targetX > currentX)
            {
                nextBoundaryX = currentX + 1.0D;
            }
            else if (targetX < currentX)
            {
                nextBoundaryX = currentX + 0.0D;
            }
            else
            {
                canMoveX = false;
            }

            if (targetY > currentY)
            {
                nextBoundaryY = currentY + 1.0D;
            }
            else if (targetY < currentY)
            {
                nextBoundaryY = currentY + 0.0D;
            }
            else
            {
                canMoveY = false;
            }

            if (targetZ > currentZ)
            {
                nextBoundaryZ = currentZ + 1.0D;
            }
            else if (targetZ < currentZ)
            {
                nextBoundaryZ = currentZ + 0.0D;
            }
            else
            {
                canMoveZ = false;
            }

            double deltaX = target.x - start.x;
            double deltaY = target.y - start.y;
            double deltaZ = target.z - start.z;

            double scaleX = 999.0D, scaleY = 999.0D, scaleZ = 999.0D;
            if (canMoveX)
            {
                scaleX = (nextBoundaryX - start.x) / deltaX;
            }

            if (canMoveY)
            {
                scaleY = (nextBoundaryY - start.y) / deltaY;
            }

            if (canMoveZ)
            {
                scaleZ = (nextBoundaryZ - start.z) / deltaZ;
            }

            byte hitSide;
            if (scaleX < scaleY && scaleX < scaleZ)
            {
                hitSide = (byte)(targetX > currentX ? 4 : 5);
                start.x = nextBoundaryX;
                start.y += deltaY * scaleX;
                start.z += deltaZ * scaleX;
            }
            else if (scaleY < scaleZ)
            {
                hitSide = (byte)(targetY > currentY ? 0 : 1);
                start.x += deltaX * scaleY;
                start.y = nextBoundaryY;
                start.z += deltaZ * scaleY;
            }
            else
            {
                hitSide = (byte)(targetZ > currentZ ? 2 : 3);
                start.x += deltaX * scaleZ;
                start.y += deltaY * scaleZ;
                start.z = nextBoundaryZ;
            }

            Vec3D currentStepPos = new(start.x, start.y, start.z);
            currentX = (int)(currentStepPos.x = MathHelper.Floor(start.x));
            if (hitSide == 5)
            {
                currentX--;
                currentStepPos.x++;
            }

            currentY = (int)(currentStepPos.y = MathHelper.Floor(start.y));
            if (hitSide == 1)
            {
                currentY--;
                currentStepPos.y++;
            }

            currentZ = (int)(currentStepPos.z = MathHelper.Floor(start.z));
            if (hitSide == 3)
            {
                currentZ--;
                currentStepPos.z++;
            }

            int blockIdAtStep = BlocksReader.GetBlockId(currentX, currentY, currentZ);
            int metaAtStep = BlocksReader.GetBlockMeta(currentX, currentY, currentZ);
            Block blockAtStep = Block.Blocks[blockIdAtStep];

            if ((!ignoreNonSolid || blockAtStep == null ||
                 blockAtStep.getCollisionShape(BlocksReader, currentX, currentY, currentZ) != null) &&
                blockIdAtStep > 0 && blockAtStep.hasCollision(metaAtStep, includeFluids))
            {
                HitResult hit = blockAtStep.raycast(BlocksReader, currentX, currentY, currentZ, start, target);
                if (hit.Type != HitResultType.MISS)
                {
                    return hit;
                }
            }
        }

        return new HitResult(HitResultType.MISS);
    }

    [Obsolete("Use Blocks.NotifyNeighbors instead.")]
    public void notifyNeighbors(int x, int y, int z, int blockId) => WorldEventBroadcaster.NotifyNeighbors(x, y, z, blockId);

    [Obsolete("Use SoundManager.blockUpdateEvent instead.")]
    public void blockUpdateEvent(int x, int y, int z) => WorldEventBroadcaster.BlockUpdateEvent(x, y, z);

    [Obsolete("Use SoundManager.PlaySoundToEntity instead.")]
    public void playSound(Entity entity, string sound, float volume, float pitch) => WorldEventBroadcaster.PlaySoundToEntity(entity, sound, volume, pitch);

    [Obsolete("Use SoundManager.PlaySoundAtPos instead.")]
    public void playSound(double x, double y, double z, string sound, float volume, float pitch) => WorldEventBroadcaster.PlaySoundAtPos(x, y, z, sound, volume, pitch);

    [Obsolete("Use SoundManager.PlayStreamingAtPos instead.")]
    public void playStreaming(string? music, int x, int y, int z) => WorldEventBroadcaster.PlayStreamingAtPos(music, x, y, z);

    [Obsolete("Use SoundManager.AddParticle instead.")]
    public void addParticle(string particle, double x, double y, double z, double velocityX, double velocityY, double velocityZ) => WorldEventBroadcaster.AddParticle(particle, x, y, z, velocityX, velocityY, velocityZ);

    [Obsolete("Use SoundManager.AddWorldAccess instead.")]
    public void AddWorldAccess(IWorldAccess worldAccess) => WorldEventBroadcaster.AddWorldAccess(worldAccess);

    [Obsolete("Use SoundManager.RemoveWorldAccess instead.")]
    public void RemoveWorldAccess(IWorldAccess worldAccess) => WorldEventBroadcaster.RemoveWorldAccess(worldAccess);

    public float GetTime(float partialTicks) => dimension.GetTimeOfDay(Properties.WorldTime, partialTicks);

    protected void BlockUpdate(int x, int y, int z, int blockId)
    {
        blockUpdateEvent(x, y, z);
        notifyNeighbors(x, y, z, blockId);
    }

    public Vector3D<double> GetFogColor(float partialTicks)
    {
        float timeOfDay = GetTime(partialTicks);
        return dimension.GetFogColor(timeOfDay, partialTicks);
    }

    public float CalculateSkyLightIntensity(float partialTicks)
    {
        float timeOfDay = GetTime(partialTicks);
        float intensityFactor = 1.0F - (MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 12.0F / 16.0F);
        intensityFactor = Math.Clamp(intensityFactor, 0.0F, 1.0F);

        return intensityFactor * intensityFactor * 0.5F;
    }

    public bool IsAnyBlockInBox(Box area)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0);

        if (area.MinX < 0.0)
        {
            minX--;
        }

        if (area.MinY < 0.0)
        {
            minY--;
        }

        if (area.MinZ < 0.0)
        {
            minZ--;
        }

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    if (BlocksReader.GetBlockId(x, y, z) > 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool isBoxSubmergedInFluid(Box area)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0D);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0D);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0D);

        if (area.MinX < 0.0D)
        {
            minX--;
        }

        if (area.MinY < 0.0D)
        {
            minY--;
        }

        if (area.MinZ < 0.0D)
        {
            minZ--;
        }

        for (int x = minX; x < maxX; ++x)
        {
            for (int y = minY; y < maxY; ++y)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    Block block = Block.Blocks[BlocksReader.GetBlockId(x, y, z)];
                    if (block != null && block.material.IsFluid)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool isFireOrLavaInBox(Box area)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0D);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0D);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0D);

        if (_blockHost.IsRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            for (int x = minX; x < maxX; ++x)
            {
                for (int y = minY; y < maxY; ++y)
                {
                    for (int z = minZ; z < maxZ; ++z)
                    {
                        int blockId = BlocksReader.GetBlockId(x, y, z);
                        if (blockId == Block.Fire.id || blockId == Block.FlowingLava.id || blockId == Block.Lava.id)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public bool updateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity)
    {
        int minX = MathHelper.Floor(entityBox.MinX);
        int maxX = MathHelper.Floor(entityBox.MaxX + 1.0D);
        int minY = MathHelper.Floor(entityBox.MinY);
        int maxY = MathHelper.Floor(entityBox.MaxY + 1.0D);
        int minZ = MathHelper.Floor(entityBox.MinZ);
        int maxZ = MathHelper.Floor(entityBox.MaxZ + 1.0D);

        if (!_blockHost.IsRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            return false;
        }

        bool isSubmerged = false;
        Vec3D flowVector = new(0.0D, 0.0D, 0.0D);

        for (int x = minX; x < maxX; ++x)
        {
            for (int y = minY; y < maxY; ++y)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    Block block = Block.Blocks[BlocksReader.GetBlockId(x, y, z)];
                    if (block != null && block.material == fluidMaterial)
                    {
                        double fluidSurfaceY = y + 1 - BlockFluid.getFluidHeightFromMeta(BlocksReader.GetBlockMeta(x, y, z));

                        if (maxY >= fluidSurfaceY)
                        {
                            isSubmerged = true;
                            block.applyVelocity(this, x, y, z, entity, flowVector);
                        }
                    }
                }
            }
        }

        if (flowVector.magnitude() > 0.0D)
        {
            flowVector = flowVector.normalize();
            const double flowStrength = 0.014D;
            entity.velocityX += flowVector.x * flowStrength;
            entity.velocityY += flowVector.y * flowStrength;
            entity.velocityZ += flowVector.z * flowStrength;
        }

        return isSubmerged;
    }

    public bool isMaterialInBox(Box area, Material material)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0D);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0D);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0D);

        for (int x = minX; x < maxX; ++x)
        {
            for (int y = minY; y < maxY; ++y)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    Block block = Block.Blocks[BlocksReader.GetBlockId(x, y, z)];
                    if (block != null && block.material == material)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool isFluidInBox(Box area, Material fluid)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0D);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0D);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0D);

        for (int x = minX; x < maxX; ++x)
        {
            for (int y = minY; y < maxY; ++y)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    Block block = Block.Blocks[BlocksReader.GetBlockId(x, y, z)];
                    if (block != null && block.material == fluid)
                    {
                        int meta = BlocksReader.GetBlockMeta(x, y, z);
                        double waterLevel = y + 1;
                        if (meta < 8)
                        {
                            waterLevel = y + 1 - meta / 8.0D;
                        }

                        if (waterLevel >= area.MinY)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public Explosion createExplosion(Entity source, double x, double y, double z, float power) => createExplosion(source, x, y, z, power, false);

    public virtual Explosion createExplosion(Entity source, double x, double y, double z, float power, bool fire)
    {
        Explosion explosion = new(this, source, x, y, z, power) { isFlaming = fire };
        explosion.doExplosionA();
        explosion.doExplosionB(true);
        return explosion;
    }

    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox)
    {
        double stepSizeX = 1.0D / ((targetBox.MaxX - targetBox.MinX) * 2.0D + 1.0D);
        double stepSizeY = 1.0D / ((targetBox.MaxY - targetBox.MinY) * 2.0D + 1.0D);
        double stepSizeZ = 1.0D / ((targetBox.MaxZ - targetBox.MinZ) * 2.0D + 1.0D);

        int visiblePoints = 0;
        int totalPoints = 0;

        for (float progressX = 0.0F; progressX <= 1.0F; progressX = (float)(progressX + stepSizeX))
        {
            for (float progressY = 0.0F; progressY <= 1.0F; progressY = (float)(progressY + stepSizeY))
            {
                for (float progressZ = 0.0F; progressZ <= 1.0F; progressZ = (float)(progressZ + stepSizeZ))
                {
                    double sampleX = targetBox.MinX + (targetBox.MaxX - targetBox.MinX) * progressX;
                    double sampleY = targetBox.MinY + (targetBox.MaxY - targetBox.MinY) * progressY;
                    double sampleZ = targetBox.MinZ + (targetBox.MaxZ - targetBox.MinZ) * progressZ;

                    if (raycast(new Vec3D(sampleX, sampleY, sampleZ), sourcePosition).Type == HitResultType.MISS)
                    {
                        visiblePoints++;
                    }

                    totalPoints++;
                }
            }
        }

        return visiblePoints / totalPoints;
    }

    public void ExtinguishFire(EntityPlayer player, int x, int y, int z, int direction)
    {
        if (direction == 0)
        {
            --y;
        }

        if (direction == 1)
        {
            ++y;
        }

        if (direction == 2)
        {
            --z;
        }

        if (direction == 3)
        {
            ++z;
        }

        if (direction == 4)
        {
            --x;
        }

        if (direction == 5)
        {
            ++x;
        }

        if (BlocksReader.GetBlockId(x, y, z) == Block.Fire.id)
        {
            WorldEventBroadcaster.WorldEvent(player, 1004, x, y, z, 0);
            BlockWriter.SetBlock(x, y, z, 0);
        }
    }

    public Entity? GetPlayerForProxy(Type type) => null;

    public string GetDebugInfo() => _blockHost.ChunkSource.GetDebugInfo();

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

        if (!isRemote && Entities.AreAllPlayersAsleep())
        {
            bool wasSpawnInterrupted = false;

            if (_spawnHostileMobs && difficulty >= 1)
            {
                wasSpawnInterrupted = NaturalSpawner.SpawnMonstersAndWakePlayers(this, _pathFinder, Entities.Players);
            }

            if (!wasSpawnInterrupted)
            {
                Environment.SkipNightAndClearWeather();
                Entities.WakeAllPlayers();
            }
        }

        Profiler.Start("performSpawning");
        NaturalSpawner.DoSpawning(this, _pathFinder, _spawnHostileMobs, _spawnPeacefulMobs);
        Profiler.Stop("performSpawning");

        Profiler.Start("unload100OldestChunks");
        _blockHost.ChunkSource.Tick();
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
            Chunk currentChunk = _blockHost.GetChunk(chunkPos.X, chunkPos.Z);

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
                if (blockId == 0 && BlocksReader.GetBrightness(worldX, localY, worldZ) <= random.NextInt(8) &&
                    Lighting.GetBrightness(LightType.Sky, worldX, localY, worldZ) <= 0)
                {
                    EntityPlayer closest = Entities.GetClosestPlayer(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D, 8.0D);
                    if (closest != null &&
                        closest.getSquaredDistance(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D) > 4.0D)
                    {
                        playSound(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D, "ambient.cave.cave", 0.7F,
                            0.8F + random.NextFloat() * 0.2F);
                        _soundCounter = random.NextInt(12000) + 6000;
                    }
                }
            }

            if (random.NextInt(100000) == 0 && Environment.IsRaining && Environment.IsThundering())
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomVal = _lcgBlockSeed >> 2;
                int worldX = worldXBase + (randomVal & 15);
                int worldZ = worldZBase + ((randomVal >> 8) & 15);
                int worldY = BlocksReader.GetTopSolidBlockY(worldX, worldZ);

                if (Environment.IsRainingAt(worldX, worldY, worldZ))
                {
                    Entities.SpawnGlobalEntity(new EntityLightningBolt(this, worldX, worldY, worldZ));
                    Environment.LightningTicksLeft = 2; // Error
                }
            }

            if (random.NextInt(16) == 0)
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomVal = _lcgBlockSeed >> 2;
                int localX = randomVal & 15;
                int localZ = (randomVal >> 8) & 15;
                int worldX = localX + worldXBase;
                int worldZ = localZ + worldZBase;
                int worldY = BlocksReader.GetTopSolidBlockY(worldX, worldZ);

                if (GetBiomeSource().GetBiome(worldX, worldZ).GetEnableSnow() && worldY >= 0 && worldY < 128 &&
                    currentChunk.GetLight(LightType.Block, localX, worldY, localZ) < 10)
                {
                    int blockBelowId = currentChunk.GetBlockId(localX, worldY - 1, localZ);
                    int currentBlockId = currentChunk.GetBlockId(localX, worldY, localZ);

                    if (Environment.IsRaining && currentBlockId == 0 && Block.Snow.canPlaceAt(BlocksReader, worldX, worldY, worldZ) &&
                        blockBelowId != 0 && blockBelowId != Block.Ice.id &&
                        Block.Blocks[blockBelowId].material.BlocksMovement)
                    {
                        BlockWriter.SetBlock(worldX, worldY, worldZ, Block.Snow.id);
                    }

                    if (blockBelowId == Block.Water.id && currentChunk.GetBlockMeta(localX, worldY - 1, localZ) == 0)
                    {
                        BlockWriter.SetBlock(worldX, worldY - 1, worldZ, Block.Ice.id);
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
                    Block.Blocks[blockId].onTick(new WorldTickContext(this, BlocksReader, BlockWriter, WorldEventBroadcaster, isRemote, localX + worldXBase, localY, localZ + worldZBase, random));
                }
            }
        }
    }

    public void displayTick(int centerX, int centerY, int centerZ)
    {
        const byte searchRadius = 16;
        JavaRandom particleRandom = new();

        for (int i = 0; i < 1000; ++i)
        {
            int targetX = centerX + random.NextInt(searchRadius) - random.NextInt(searchRadius);
            int targetY = centerY + random.NextInt(searchRadius) - random.NextInt(searchRadius);
            int targetZ = centerZ + random.NextInt(searchRadius) - random.NextInt(searchRadius);

            int blockId = BlocksReader.GetBlockId(targetX, targetY, targetZ);
            if (blockId > 0)
            {
                Block.Blocks[blockId].randomDisplayTick(this, targetX, targetY, targetZ, particleRandom);
            }
        }
    }

    public void UpdateBlockEntity(int x, int y, int z, BlockEntity blockEntity)
    {
        if (_blockHost.IsPosLoaded(x, y, z))
        {
            _blockHost.GetChunkFromPos(x, z).MarkDirty();
        }

        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].updateBlockEntity(x, y, z, blockEntity);
        }
    }

    public void TickChunks()
    {
        while (_blockHost.ChunkSource.Tick())
        {
        }
    }

    public bool canPlace(int blockId, int x, int y, int z, bool isFallingBlock, int side)
    {
        int existingBlockId = BlocksReader.GetBlockId(x, y, z);
        Block? existingBlock = Block.Blocks[existingBlockId];
        Block? newBlock = Block.Blocks[blockId];

        Box? collisionBox = newBlock?.getCollisionShape(BlocksReader, x, y, z);

        if (isFallingBlock)
        {
            collisionBox = null;
        }

        if (collisionBox != null && !Entities.CanSpawnEntity(collisionBox.Value))
        {
            return false;
        }

        if (existingBlock == Block.FlowingWater || existingBlock == Block.Water ||
            existingBlock == Block.FlowingLava || existingBlock == Block.Lava ||
            existingBlock == Block.Fire || existingBlock == Block.Snow)
        {
            existingBlock = null;
        }

        return blockId > 0 && existingBlock == null && newBlock != null && newBlock.canPlaceAt(
            new CanPlaceAtCtx(
                BlocksReader,
                BlockWriter,
                x,
                y,
                z
            )
        );
    }

    internal PathEntity findPath(Entity entity, Entity target, float range)
    {
        Profiler.Start("AI.PathFinding.FindPathToTarget");
        int entityX = MathHelper.Floor(entity.x);
        int entityY = MathHelper.Floor(entity.y);
        int entityZ = MathHelper.Floor(entity.z);
        int searchRadius = (int)(range + 16.0F);

        int minX = entityX - searchRadius;
        int minY = entityY - searchRadius;
        int minZ = entityZ - searchRadius;
        int maxX = entityX + searchRadius;
        int maxY = entityY + searchRadius;
        int maxZ = entityZ + searchRadius;

        WorldRegion region = new(this, minX, minY, minZ, maxX, maxY, maxZ);

        PathEntity result = _pathFinder.CreateEntityPathTo(entity, target, range);
        Profiler.Stop("AI.PathFinding.FindPathToTarget");

        return result;
    }

    internal PathEntity findPath(Entity entity, int x, int y, int z, float range)
    {
        Profiler.Start("AI.PathFinding.FindPathToPosition");
        int entityX = MathHelper.Floor(entity.x);
        int entityY = MathHelper.Floor(entity.y);
        int entityZ = MathHelper.Floor(entity.z);
        int searchRadius = (int)(range + 8.0F);

        int minX = entityX - searchRadius;
        int minY = entityY - searchRadius;
        int minZ = entityZ - searchRadius;
        int maxX = entityX + searchRadius;
        int maxY = entityY + searchRadius;
        int maxZ = entityZ + searchRadius;

        WorldRegion region = new(this, minX, minY, minZ, maxX, maxY, maxZ);


        PathEntity result = _pathFinder.CreateEntityPathTo(entity, x, y, z, range);
        Profiler.Stop("AI.PathFinding.FindPathToPosition");

        return result;
    }

    public EntityPlayer GetClosestPlayer(Entity entity, double range) => Entities.GetClosestPlayer(entity.x, entity.y, entity.z, range);

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

                    currentBufferOffset = _blockHost.GetChunk(chunkX, chunkZ).LoadFromPacket(
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

    public byte[] GetChunkData(int x, int y, int z, int sizeX, int sizeY, int sizeZ)
    {
        byte[] chunkData = new byte[sizeX * sizeY * sizeZ * 5 / 2];

        int startChunkX = x >> 4;
        int startChunkZ = z >> 4;
        int endChunkX = (x + sizeX - 1) >> 4;
        int endChunkZ = (z + sizeZ - 1) >> 4;

        int currentBufferOffset = 0;
        int minY = Math.Max(0, y);
        int maxY = Math.Min(128, y + sizeY);

        for (int chunkX = startChunkX; chunkX <= endChunkX; chunkX++)
        {
            int localStartX = Math.Max(0, x - chunkX * 16);
            int localEndX = Math.Min(16, x + sizeX - chunkX * 16);

            for (int chunkZ = startChunkZ; chunkZ <= endChunkZ; chunkZ++)
            {
                int localStartZ = Math.Max(0, z - chunkZ * 16);
                int localEndZ = Math.Min(16, z + sizeZ - chunkZ * 16);

                currentBufferOffset = _blockHost.GetChunk(chunkX, chunkZ).ToPacket(
                    chunkData,
                    localStartX, minY, localStartZ,
                    localEndX, maxY, localEndZ,
                    currentBufferOffset);
            }
        }

        return chunkData;
    }

    public void SetTime(long time) => Properties.WorldTime = time;

    public long GetSeed() => Properties.RandomSeed;

    public long GetTime() => Properties.WorldTime;

    public Vec3i GetSpawnPos() => new(Properties.SpawnX, Properties.SpawnY, Properties.SpawnZ);

    public void SetSpawnPos(Vec3i pos) => Properties.SetSpawn(pos.X, pos.Y, pos.Z);

    public virtual bool canInteract(EntityPlayer player, int x, int y, int z) => true;

    public virtual void BroadcastEntityEvent(Entity entity, byte @event)
    {
    }

    public virtual void playNoteBlockActionAt(int x, int y, int z, int soundType, int pitch)
    {
        int blockId = BlocksReader.GetBlockId(x, y, z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].onBlockAction(this, x, y, z, soundType, pitch);
        }
    }

    public void setBlocksDirty(int x, int z, int minY, int maxY)
    {
        if (minY > maxY)
        {
            (maxY, minY) = (minY, maxY);
        }

        setBlocksDirty(x, minY, z, x, maxY, z);
    }

    public void setBlocksDirty(int x, int y, int z)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].setBlocksDirty(x, y, z, x, y, z);
        }
    }

    public void setBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].setBlocksDirty(minX, minY, minZ, maxX, maxY, maxZ);
        }
    }

    public void getState(string id, PersistentState state) => PersistentStateManager.SetData(id, state);

    public PersistentState? getOrCreateState(Type type, string id) => PersistentStateManager.LoadData(type, id);

    public int getIdCount(string id) => PersistentStateManager.GetUniqueDataId(id);

    // TODO: These are marked [Obsolete]. In future PRs, update the calling code
    // to use the specific engines (e.g., world.Blocks.GetBlockId) and delete these.

    #region Block & Chunk Proxy (Routes to WorldBlockView)

    public IChunkSource getChunkSource() => _blockHost.ChunkSource;

    [Obsolete("Use Blocks.GetBlockId instead.")]
    public int getBlockId(int x, int y, int z) => BlocksReader.GetBlockId(x, y, z);

    [Obsolete("Use Blocks.IsAir instead.")]
    public bool isAir(int x, int y, int z) => BlocksReader.IsAir(x, y, z);

    [Obsolete("Use Blocks.IsPosLoaded instead.")]
    public bool isPosLoaded(int x, int y, int z) => _blockHost.IsPosLoaded(x, y, z);

    [Obsolete("Use Blocks.IsRegionLoaded instead.")]
    public bool isRegionLoaded(int x, int y, int z, int range) => _blockHost.IsRegionLoaded(x, y, z, range);

    [Obsolete("Use Blocks.IsRegionLoaded instead.")]
    public bool isRegionLoaded(int minX, int minY, int minZ, int maxX, int maxY, int maxZ) => _blockHost.IsRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ);

    [Obsolete("Use Blocks.HasChunk instead.")]
    private bool hasChunk(int x, int z) => _blockHost.HasChunk(x, z);

    [Obsolete("Use Blocks.GetChunkFromPos instead.")]
    public Chunk GetChunkFromPos(int x, int z) => _blockHost.GetChunkFromPos(x, z);

    [Obsolete("Use Blocks.GetChunk instead.")]
    public Chunk GetChunk(int chunkX, int chunkZ) => _blockHost.GetChunk(chunkX, chunkZ);

    [Obsolete("Use Blocks.SetBlockWithoutNotifyingNeighbors instead.")]
    public virtual bool setBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta) => BlockWriter.SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, meta);

    [Obsolete("Use Blocks.SetBlockWithoutNotifyingNeighbors instead.")]
    public virtual bool setBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId) => BlockWriter.SetBlockWithoutNotifyingNeighbors(x, y, z, blockId);

    [Obsolete("Use Blocks.GetMaterial instead.")]
    public Material getMaterial(int x, int y, int z) => BlocksReader.GetMaterial(x, y, z);

    [Obsolete("Use Blocks.GetBlockMeta instead.")]
    public int getBlockMeta(int x, int y, int z) => BlocksReader.GetBlockMeta(x, y, z);

    [Obsolete("Use Blocks.SetBlockMeta instead.")]
        public void setBlockMeta(int x, int y, int z, int meta) => BlockWriter.SetBlockMeta(x, y, z, meta);

    [Obsolete("Use Blocks.SetBlockMetaWithoutNotifyingNeighbors instead.")]
    public virtual bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta) => BlockWriter.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, meta);

    [Obsolete("Use Blocks.SetBlock instead.")]
    public bool setBlock(int x, int y, int z, int blockId) => BlockWriter.SetBlock(x, y, z, blockId);

    [Obsolete("Use Blocks.SetBlock instead.")]
    public bool setBlock(int x, int y, int z, int blockId, int meta) => BlockWriter.SetBlock(x, y, z, blockId, meta);

    [Obsolete("Use Blocks.IsTopY instead.")]
    public bool isTopY(int x, int y, int z) => BlocksReader.IsTopY(x, y, z);

    [Obsolete("Use Blocks.GetTopY instead.")]
    public int getTopY(int x, int z) => BlocksReader.GetTopY(x, z);

    [Obsolete("Use Blocks.GetTopSolidBlockY instead.")]
    public int getTopSolidBlockY(int x, int z) => BlocksReader.GetTopSolidBlockY(x, z);

    [Obsolete("Use Blocks.GetSpawnPositionValidityY instead.")]
    public int getSpawnPositionValidityY(int x, int z) => BlocksReader.GetSpawnPositionValidityY(x, z);

    [Obsolete("Use Blocks.IsOpaque instead.")]
    public bool isOpaque(int x, int y, int z) => BlocksReader.IsOpaque(x, y, z);

    [Obsolete("Use Blocks.ShouldSuffocate instead.")]
    public bool shouldSuffocate(int x, int y, int z) => BlocksReader.ShouldSuffocate(x, y, z);

    [Obsolete("Use Blocks.GetBlockEntity instead.")]
    public BlockEntity? getBlockEntity(int x, int y, int z) => BlocksReader.GetBlockEntity(x, y, z);

    #endregion

    #region Lighting Proxy (Routes to LightingEngine)

    [Obsolete("Use Lighting.HasSkyLight instead.")]
    public bool hasSkyLight(int x, int y, int z) => Lighting.HasSkyLight(x, y, z);

    [Obsolete("Use Lighting.GetBrightness instead.")]
    public int getBrightness(int x, int y, int z) => Lighting.GetBrightness(x, y, z);

    [Obsolete("Use Lighting.GetLightLevel instead.")]
    public int getLightLevel(int x, int y, int z) => Lighting.GetLightLevel(x, y, z);

    [Obsolete("Use Lighting.GetLightLevel instead.")]
    public int getLightLevel(int x, int y, int z, bool checkNeighbors) => Lighting.GetLightLevel(x, y, z, checkNeighbors);

    [Obsolete("Use Lighting.UpdateLight instead.")]
    public void updateLight(LightType lightType, int x, int y, int z, int targetLuminance) => Lighting.UpdateLight(lightType, x, y, z, targetLuminance);

    [Obsolete("Use Lighting.GetBrightness instead.")]
    public int getBrightness(LightType type, int x, int y, int z) => Lighting.GetBrightness(type, x, y, z);

    [Obsolete("Use Lighting.SetLight instead.")]
    public void setLight(LightType lightType, int x, int y, int z, int value) => Lighting.SetLight(lightType, x, y, z, value);

    [Obsolete("Use Lighting.GetNaturalBrightness instead.")]
    public float getNaturalBrightness(int x, int y, int z, int blockLight) => Lighting.GetNaturalBrightness(x, y, z, blockLight);

    [Obsolete("Use Lighting.GetLuminance instead.")]
    public float getLuminance(int x, int y, int z) => Lighting.getLuminance(x, y, z);

    [Obsolete("Use Lighting.DoLightingUpdates instead.")]
    public bool doLightingUpdates() => Lighting.DoLightingUpdates();

    [Obsolete("Use Lighting.QueueLightUpdate instead.")]
    public void queueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ) => Lighting.QueueLightUpdate(type, minX, minY, minZ, maxX, maxY, maxZ);

    [Obsolete("Use Lighting.QueueLightUpdate instead.")]
    public void queueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, bool attemptMerge) => Lighting.QueueLightUpdate(type, minX, minY, minZ, maxX, maxY, maxZ, attemptMerge);

    #endregion

    #region Environment Proxy (Routes to EnvironmentManager)

    [Obsolete("Use Environment.IsRainingAt instead.")]
    public bool isRaining(int x, int y, int z) => Environment.IsRainingAt(x, y, z);

    #endregion

    #region Entity Proxy (Routes to EntityManager)

    [Obsolete("Use Entities.SpawnEntity instead.")]
    public virtual bool SpawnEntity(Entity entity) => Entities.SpawnEntity(entity);

    void IBlockWorldContext.SpawnEntity(Entity entity) => Entities.SpawnEntity(entity);

    void IBlockWorldContext.SpawnItemDrop(double x, double y, double z, ItemStack itemStack)
    {
        var droppedItem = new EntityItem(this, x, y, z, itemStack);
        droppedItem.delayBeforeCanPickup = 10;
        Entities.SpawnEntity(droppedItem);
    }

    [Obsolete("Use Entities.GetEntityCollisions instead.")]
    public List<Box> getEntityCollisionsScratch(Entity entity, Box area) => Entities.GetEntityCollisions(entity, area);

    [Obsolete("Use Entities.GetEntities instead.")]
    public List<Entity> getEntities(Entity? excludeEntity, Box area) => Entities.GetEntities(excludeEntity, area);

    [Obsolete("Use Entities.CollectEntitiesOfType instead.")]
    public List<T> CollectEntitiesOfType<T>(Box area) where T : Entity => Entities.CollectEntitiesOfType<T>(area);

    [Obsolete("Use Entities.GetClosestPlayer instead.")]
    public EntityPlayer getClosestPlayer(double x, double y, double z, double range) => Entities.GetClosestPlayer(x, y, z, range);

    #endregion

    #region Redstone Proxy (Routes to RedstoneEngine)

    [Obsolete("Use Redstone.IsPoweringSide instead.")]
    public bool isPoweringSide(int x, int y, int z, int side) => Redstone.IsPoweringSide(x, y, z, side);

    [Obsolete("Use Redstone.IsPowered instead.")]
    public bool isPowered(int x, int y, int z) => Redstone.IsPowered(x, y, z);

    #endregion

    #region Scheduler Proxy (Routes to WorldTickScheduler)

    [Obsolete("Use TickScheduler.ScheduleBlockUpdate instead.")]
    public virtual void ScheduleBlockUpdate(int x, int y, int z, int blockId, int tickRate) => TickScheduler.ScheduleBlockUpdate(x, y, z, blockId, tickRate);

    #endregion
}
