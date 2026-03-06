using System.Runtime.InteropServices;
using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.PathFinding;
using BetaSharp.Profiling;
using BetaSharp.Rules;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Generation.Biomes.Source;
using BetaSharp.Worlds.Lighting;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace BetaSharp.Worlds.Core;

public abstract class World : IBlockAccess
{
    private static readonly int s_autosavePeriod = 40;

    private readonly HashSet<ChunkPos> _activeChunks = new();
    private readonly ChunkSource _chunkSource;
    private readonly List<LightUpdate> _lightingQueue = [];
    private readonly ILogger<World> _logger = Log.Instance.For<World>();

    private readonly PathFinder _pathFinder;
    private readonly PriorityQueue<BlockUpdate, (long, long)> _scheduledUpdates = new();

    private readonly long _worldTimeMask = 0xFFFFFFL;
    public readonly Dimension Dimension;
    public readonly EntityManager Entities;

    public readonly EnvironmentManager Environment;
    protected readonly List<IWorldAccess> EventListeners = [];
    protected readonly IWorldStorage Storage;

    private long _eventDeltaTime; // difference between world time and the scheduled time of the block events so things don't break when using the time command
    private int _lcgBlockSeed = Random.Shared.Next();
    private int _lightingUpdatesCounter;
    private int _lightingUpdatesScheduled;
    private int _soundCounter = Random.Shared.Next(12000);
    private bool _spawnHostileMobs = true;
    private bool _spawnPeacefulMobs = true;

    public int ambientDarkness;
    protected int AutosavePeriod = s_autosavePeriod;
    public int Difficulty;
    public bool eventProcessingEnabled;
    public bool instantBlockUpdateEnabled = false;
    public bool IsNewWorld;
    public bool IsRemote = false;
    public int lightningTicksLeft = 0;
    public bool pauseTicking = false;
    public PersistentStateManager PersistentStateManager;

    protected float PrevRainingStrength;
    protected float PrevThunderingStrength;
    public WorldProperties Properties { get; protected set; }
    protected float RainingStrength;

    public JavaRandom random = new();
    protected float ThunderingStrength;
    protected int TicksSinceLightning;

    protected World(IWorldStorage worldStorage, string levelName, Dimension dim, long seed)
    {
        _pathFinder = new PathFinder(this);
        Storage = worldStorage;
        PersistentStateManager = new PersistentStateManager(worldStorage);
        Properties = new WorldProperties(seed, levelName);
        Dimension = dim;
        dim.SetWorld(this);
        _chunkSource = CreateChunkCache();
        Rules = Properties.RulesTag != null
            ? RuleSet.FromNBT(RuleRegistry.Instance, Properties.RulesTag)
            : new RuleSet(RuleRegistry.Instance);

        Entities = new EntityManager(this);
        Environment = new EnvironmentManager(this);

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

        Dimension = dim ?? Dimension.FromId(Properties.Dimension == -1 ? -1 : 0);
        Dimension.SetWorld(this);

        Dimension.SetWorld(this);
        _chunkSource = CreateChunkCache();

        Rules = Properties.RulesTag != null
            ? RuleSet.FromNBT(RuleRegistry.Instance, Properties.RulesTag)
            : new RuleSet(RuleRegistry.Instance);

        if (shouldInitializeSpawn)
        {
            initializeSpawnPoint();
        }

        Entities = new EntityManager(this);
        Environment = new EnvironmentManager(this);

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

    public RuleSet Rules { get; protected set; }

    public BiomeSource getBiomeSource() => Dimension.BiomeSource;

    public int getBlockId(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128)
        {
            return 0;
        }

        return GetChunk(x >> 4, z >> 4).GetBlockId(x & 15, y, z & 15);
    }

    public Material getMaterial(int x, int y, int z)
    {
        int blockId = getBlockId(x, y, z);
        return blockId == 0 ? Material.Air : Block.Blocks[blockId].material;
    }

    public int getBlockMeta(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128)
        {
            return 0;
        }

        return GetChunk(x >> 4, z >> 4).GetBlockMeta(x & 15, y, z & 15);
    }

    public float getNaturalBrightness(int x, int y, int z, int blockLight)
    {
        int lightLevel = getLightLevel(x, y, z);
        if (lightLevel < blockLight)
        {
            lightLevel = blockLight;
        }

        return Dimension.LightLevelToLuminance[lightLevel];
    }

    public float getLuminance(int x, int y, int z) => Dimension.LightLevelToLuminance[getLightLevel(x, y, z)];

    public BlockEntity? getBlockEntity(int x, int y, int z)
    {
        Chunk? chunk = GetChunk(x >> 4, z >> 4);
        BlockEntity? entity = chunk?.GetBlockEntity(x & 15, y, z & 15) ?? Entities.BlockEntities.FirstOrDefault(e => e.X == x && e.Y == y && e.Z == z);

        return entity;
    }

    public bool isOpaque(int x, int y, int z)
    {
        Block? block = Block.Blocks[getBlockId(x, y, z)];
        return block == null ? false : block.isOpaque();
    }

    public bool shouldSuffocate(int x, int y, int z)
    {
        Block? block = Block.Blocks[getBlockId(x, y, z)];
        return block == null ? false : block.material.Suffocates && block.isFullCube();
    }

    public IWorldStorage getWorldStorage() => Storage;

    protected abstract ChunkSource CreateChunkCache();

    private void initializeSpawnPoint()
    {
        eventProcessingEnabled = true;
        int x = 0;
        byte y = 64;

        int z;
        for (
            z = 0;
            !Dimension.IsValidSpawnPoint(x, z);
            z += random.NextInt(64) - random.NextInt(64))
        {
            x += random.NextInt(64) - random.NextInt(64);
        }

        Properties.SetSpawn(x, y, z);
        eventProcessingEnabled = false;
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
             getSpawnBlockId(spawnX, spawnZ) == 0;
             spawnZ += random.NextInt(8) - random.NextInt(8))
        {
            spawnX += random.NextInt(8) - random.NextInt(8);
        }

        Properties.SpawnX = spawnX;
        Properties.SpawnZ = spawnZ;
    }

    public int getSpawnBlockId(int x, int z)
    {
        int y;
        for (y = 63; !isAir(x, y + 1, z); ++y)
        {
        }

        return getBlockId(x, y, z);
    }

    public void saveWorldData()
    {
    }

    public void addPlayer(EntityPlayer player)
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

    public void saveWithLoadingDisplay(bool saveEntities, LoadingDisplay? loadingDisplay)
    {
        if (_chunkSource.CanSave())
        {
            if (loadingDisplay != null)
            {
                loadingDisplay.progressStartNoAbort("Saving level");
            }

            Profiler.PushGroup("saveLevel");
            save();
            Profiler.PopGroup();
            if (loadingDisplay != null)
            {
                loadingDisplay.progressStage("Saving chunks");
            }

            Profiler.Start("saveChunks");
            _chunkSource.Save(saveEntities, loadingDisplay);
            Profiler.Stop("saveChunks");
        }
    }

    private void save()
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

    public bool attemptSaving(int i)
    {
        if (!_chunkSource.CanSave())
        {
            return true;
        }

        if (i == 0)
        {
            save();
        }

        return _chunkSource.Save(false, null);
    }

    public bool isAir(int x, int y, int z) => getBlockId(x, y, z) == 0;

    public bool isPosLoaded(int x, int y, int z) => y >= 0 && y < 128 ? hasChunk(x >> 4, z >> 4) : false;

    public bool isRegionLoaded(int x, int y, int z, int range) => isRegionLoaded(x - range, y - range, z - range, x + range, y + range, z + range);

    public bool isRegionLoaded(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        if (maxY >= 0 && minY < 128)
        {
            minX >>= 4;
            minY >>= 4;
            minZ >>= 4;
            maxX >>= 4;
            maxY >>= 4;
            maxZ >>= 4;

            for (int x = minX; x <= maxX; ++x)
            {
                for (int z = minZ; z <= maxZ; ++z)
                {
                    if (!hasChunk(x, z))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return false;
    }

    private bool hasChunk(int x, int z) => _chunkSource.IsChunkLoaded(x, z);

    public Chunk GetChunkFromPos(int x, int z) => GetChunk(x >> 4, z >> 4);

    public Chunk GetChunk(int chunkX, int chunkZ) => _chunkSource.GetChunk(chunkX, chunkZ);

    public virtual bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128)
        {
            return false;
        }

        return GetChunk(x >> 4, z >> 4).SetBlock(x & 15, y, z & 15, blockId, meta);
    }

    public virtual bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            if (y < 0)
            {
                return false;
            }

            if (y >= 128)
            {
                return false;
            }

            Chunk chunk = GetChunk(x >> 4, z >> 4);
            return chunk.SetBlock(x & 15, y, z & 15, blockId);
        }

        return false;
    }

    public void setBlockMeta(int x, int y, int z, int meta)
    {
        if (SetBlockMetaWithoutNotifyingNeighbors(x, y, z, meta))
        {
            int blockId = getBlockId(x, y, z);
            if (Block.BlocksIngoreMetaUpdate[blockId & 255])
            {
                blockUpdate(x, y, z, blockId);
            }
            else
            {
                notifyNeighbors(x, y, z, blockId);
            }
        }
    }

    public virtual bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            if (y < 0)
            {
                return false;
            }

            if (y >= 128)
            {
                return false;
            }

            Chunk chunk = GetChunk(x >> 4, z >> 4);
            x &= 15;
            z &= 15;
            chunk.SetBlockMeta(x, y, z, meta);
            return true;
        }

        return false;
    }

    public bool setBlock(int x, int y, int z, int blockId)
    {
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId))
        {
            blockUpdate(x, y, z, blockId);
            return true;
        }

        return false;
    }

    public bool setBlock(int x, int y, int z, int blockId, int meta)
    {
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, meta))
        {
            blockUpdate(x, y, z, blockId);
            return true;
        }

        return false;
    }

    public void blockUpdateEvent(int x, int y, int z)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].blockUpdate(x, y, z);
        }
    }

    protected void blockUpdate(int x, int y, int z, int blockId)
    {
        blockUpdateEvent(x, y, z);
        notifyNeighbors(x, y, z, blockId);
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

    public void notifyNeighbors(int x, int y, int z, int blockId)
    {
        notifyUpdate(x - 1, y, z, blockId);
        notifyUpdate(x + 1, y, z, blockId);
        notifyUpdate(x, y - 1, z, blockId);
        notifyUpdate(x, y + 1, z, blockId);
        notifyUpdate(x, y, z - 1, blockId);
        notifyUpdate(x, y, z + 1, blockId);
    }

    private void notifyUpdate(int x, int y, int z, int blockId)
    {
        if (!pauseTicking && !IsRemote)
        {
            Block block = Block.Blocks[getBlockId(x, y, z)];
            if (block != null)
            {
                block.neighborUpdate(this, x, y, z, blockId);
            }
        }
    }

    public bool hasSkyLight(int x, int y, int z) => GetChunk(x >> 4, z >> 4).IsAboveMaxHeight(x & 15, y, z & 15);

    public int getBrightness(int x, int y, int z)
    {
        if (y < 0)
        {
            return 0;
        }

        if (y >= 128)
        {
            return !Dimension.HasCeiling ? 15 : 0;
        }

        return GetChunk(x >> 4, z >> 4).GetLight(x & 15, y, z & 15, 0);
    }

    public int getLightLevel(int x, int y, int z) => getLightLevel(x, y, z, true);

    public int getLightLevel(int x, int y, int z, bool checkNeighbors)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000)
        {
            return 15;
        }

        if (checkNeighbors)
        {
            int blockId = getBlockId(x, y, z);
            if (blockId == Block.Slab.id || blockId == Block.Farmland.id ||
                blockId == Block.CobblestoneStairs.id || blockId == Block.WoodenStairs.id)
            {
                int neighborMaxLight = getLightLevel(x, y + 1, z, false);

                int lightPosX = getLightLevel(x + 1, y, z, false);
                int lightNegX = getLightLevel(x - 1, y, z, false);
                int lightPosZ = getLightLevel(x, y, z + 1, false);
                int lightNegZ = getLightLevel(x, y, z - 1, false);

                if (lightPosX > neighborMaxLight)
                {
                    neighborMaxLight = lightPosX;
                }

                if (lightNegX > neighborMaxLight)
                {
                    neighborMaxLight = lightNegX;
                }

                if (lightPosZ > neighborMaxLight)
                {
                    neighborMaxLight = lightPosZ;
                }

                if (lightNegZ > neighborMaxLight)
                {
                    neighborMaxLight = lightNegZ;
                }

                return neighborMaxLight;
            }
        }

        if (y < 0)
        {
            return 0;
        }

        if (y >= 128)
        {
            return !Dimension.HasCeiling ? 15 - ambientDarkness : 0;
        }

        Chunk chunk = GetChunk(x >> 4, z >> 4);
        return chunk.GetLight(x & 15, y, z & 15, ambientDarkness);
    }

    public bool isTopY(int x, int y, int z)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            if (y < 0)
            {
                return false;
            }

            if (y >= 128)
            {
                return true;
            }

            if (!hasChunk(x >> 4, z >> 4))
            {
                return false;
            }

            Chunk chunk = GetChunk(x >> 4, z >> 4);
            x &= 15;
            z &= 15;
            return chunk.IsAboveMaxHeight(x, y, z);
        }

        return false;
    }

    public int getTopY(int x, int z)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            int chunkX = x >> 4;
            int chunkZ = z >> 4;

            if (!hasChunk(chunkX, chunkZ))
            {
                return 0;
            }

            Chunk chunk = GetChunk(chunkX, chunkZ);
            return chunk.GetHeight(x & 15, z & 15);
        }

        return 0;
    }

    public void updateLight(LightType lightType, int x, int y, int z, int targetLuminance)
    {
        if (Dimension.HasCeiling && lightType == LightType.Sky)
        {
            return;
        }

        if (isPosLoaded(x, y, z))
        {
            if (lightType == LightType.Sky)
            {
                if (isTopY(x, y, z))
                {
                    targetLuminance = 15;
                }
            }
            else if (lightType == LightType.Block)
            {
                int blockId = getBlockId(x, y, z);
                if (Block.BlocksLightLuminance[blockId] > targetLuminance)
                {
                    targetLuminance = Block.BlocksLightLuminance[blockId];
                }
            }

            if (getBrightness(lightType, x, y, z) != targetLuminance)
            {
                queueLightUpdate(lightType, x, y, z, x, y, z);
            }
        }
    }

    public int getBrightness(LightType type, int x, int y, int z)
    {
        if (y < 0)
        {
            y = 0;
        }

        if (y >= 128)
        {
            return type.lightValue;
        }

        if (y >= 0 && y < 128 && x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            int chunkX = x >> 4;
            int chunkZ = z >> 4;
            if (!hasChunk(chunkX, chunkZ))
            {
                return 0;
            }

            Chunk chunk = GetChunk(chunkX, chunkZ);
            return chunk.GetLight(type, x & 15, y, z & 15);
        }

        return type.lightValue;
    }

    public void setLight(LightType lightType, int x, int y, int z, int value)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            if (y >= 0)
            {
                if (y < 128)
                {
                    if (hasChunk(x >> 4, z >> 4))
                    {
                        Chunk chunk = GetChunk(x >> 4, z >> 4);
                        chunk.SetLight(lightType, x & 15, y, z & 15, value);

                        for (int i = 0; i < EventListeners.Count; ++i)
                        {
                            EventListeners[i].blockUpdate(x, y, z);
                        }
                    }
                }
            }
        }
    }

    public bool canMonsterSpawn() => ambientDarkness < 4;

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

        int initialId = getBlockId(currentX, currentY, currentZ);
        int initialMeta = getBlockMeta(currentX, currentY, currentZ);
        Block initialBlock = Block.Blocks[initialId];

        if ((!ignoreNonSolid || initialBlock == null ||
             initialBlock.getCollisionShape(this, currentX, currentY, currentZ) != null) &&
            initialId > 0 && initialBlock.hasCollision(initialMeta, includeFluids))
        {
            HitResult result = initialBlock.raycast(this, currentX, currentY, currentZ, start, target);
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

            int blockIdAtStep = getBlockId(currentX, currentY, currentZ);
            int metaAtStep = getBlockMeta(currentX, currentY, currentZ);
            Block blockAtStep = Block.Blocks[blockIdAtStep];

            if ((!ignoreNonSolid || blockAtStep == null ||
                 blockAtStep.getCollisionShape(this, currentX, currentY, currentZ) != null) &&
                blockIdAtStep > 0 && blockAtStep.hasCollision(metaAtStep, includeFluids))
            {
                HitResult hit = blockAtStep.raycast(this, currentX, currentY, currentZ, start, target);
                if (hit.Type != HitResultType.MISS)
                {
                    return hit;
                }
            }
        }

        return new HitResult(HitResultType.MISS);
    }

    public void playSound(Entity entity, string sound, float volume, float pitch)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].playSound(sound, entity.x, entity.y - entity.standingEyeHeight, entity.z, volume,
                pitch);
        }
    }

    public void playSound(double x, double y, double z, string sound, float volume, float pitch)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].playSound(sound, x, y, z, volume, pitch);
        }
    }

    public void playStreaming(string music, int x, int y, int z)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].playStreaming(music, x, y, z);
        }
    }

    public void addParticle(string particle, double x, double y, double z, double velocityX, double velocityY,
        double velocityZ)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].spawnParticle(particle, x, y, z, velocityX, velocityY, velocityZ);
        }
    }

    public void addWorldAccess(IWorldAccess worldAccess) => EventListeners.Add(worldAccess);

    public void removeWorldAccess(IWorldAccess worldAccess) => EventListeners.Remove(worldAccess);

    public float getTime(float delta) => Dimension.GetTimeOfDay(Properties.WorldTime, delta);


    public Vector3D<double> getFogColor(float partialTicks)
    {
        float timeOfDay = getTime(partialTicks);
        return Dimension.GetFogColor(timeOfDay, partialTicks);
    }

    public int getTopSolidBlockY(int x, int z)
    {
        Chunk chunk = GetChunkFromPos(x, z);
        int currentY = 127;
        int localX = x & 15;
        int localZ = z & 15;

        for (; currentY > 0; --currentY)
        {
            int blockId = chunk.GetBlockId(localX, currentY, localZ);
            Material material = blockId == 0 ? Material.Air : Block.Blocks[blockId].material;

            if (material.BlocksMovement || material.IsFluid)
            {
                return currentY + 1;
            }
        }

        return -1;
    }

    public float calcualteSkyLightIntensity(float partialTicks)
    {
        float timeOfDay = getTime(partialTicks);
        float intensityFactor = 1.0F - (MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 12.0F / 16.0F);
        intensityFactor = Math.Clamp(intensityFactor, 0.0F, 1.0F);

        return intensityFactor * intensityFactor * 0.5F;
    }

    public int getSpawnPositionValidityY(int x, int z)
    {
        Chunk chunk = GetChunkFromPos(x, z);
        int currentY = 127;
        int localX = x & 15;
        int localZ = z & 15;

        for (; currentY > 0; currentY--)
        {
            int blockId = chunk.GetBlockId(localX, currentY, localZ);
            if (blockId != 0 && Block.Blocks[blockId].material.BlocksMovement)
            {
                return currentY + 1;
            }
        }

        return -1;
    }

    public virtual void ScheduleBlockUpdate(int x, int y, int z, int blockId, int tickRate)
    {
        const byte loadRadius = 8;
        if (isRegionLoaded(x - loadRadius, y - loadRadius, z - loadRadius, x + loadRadius, y + loadRadius,
                z + loadRadius))
        {
            if (instantBlockUpdateEnabled)
            {
                int currentBlockId = getBlockId(x, y, z);
                if (currentBlockId == blockId && currentBlockId > 0)
                {
                    Block.Blocks[currentBlockId].onTick(this, x, y, z, random);
                }
            }
            else
            {
                long scheduledTime = GetEventTime() + tickRate;
                BlockUpdate blockUpdate = new(x, y, z, blockId, scheduledTime);
                _scheduledUpdates.Enqueue(blockUpdate, (blockUpdate.ScheduledTime, blockUpdate.ScheduledOrder));
            }
        }
    }

    public bool isAnyBlockInBox(Box area)
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
                    if (getBlockId(x, y, z) > 0)
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
                    Block block = Block.Blocks[getBlockId(x, y, z)];
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

        if (isRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            for (int x = minX; x < maxX; ++x)
            {
                for (int y = minY; y < maxY; ++y)
                {
                    for (int z = minZ; z < maxZ; ++z)
                    {
                        int blockId = getBlockId(x, y, z);
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

        if (!isRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
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
                    Block block = Block.Blocks[getBlockId(x, y, z)];
                    if (block != null && block.material == fluidMaterial)
                    {
                        double fluidSurfaceY = y + 1 - BlockFluid.getFluidHeightFromMeta(getBlockMeta(x, y, z));

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
                    Block block = Block.Blocks[getBlockId(x, y, z)];
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
                    Block block = Block.Blocks[getBlockId(x, y, z)];
                    if (block != null && block.material == fluid)
                    {
                        int meta = getBlockMeta(x, y, z);
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

    public float getVisibilityRatio(Vec3D sourcePosition, Box targetBox)
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

    public void extinguishFire(EntityPlayer player, int x, int y, int z, int direction)
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

        if (getBlockId(x, y, z) == Block.Fire.id)
        {
            worldEvent(player, 1004, x, y, z, 0);
            setBlock(x, y, z, 0);
        }
    }

    public Entity? getPlayerForProxy(Type type) => null;


    public string getDebugInfo() => _chunkSource.GetDebugInfo();

    public void savingProgress(LoadingDisplay display) => saveWithLoadingDisplay(true, display);

    public bool doLightingUpdates()
    {
        if (_lightingUpdatesCounter >= 50)
        {
            return false;
        }

        ++_lightingUpdatesCounter;

        try
        {
            int updatesBudget = 500;

            while (_lightingQueue.Count > 0)
            {
                if (updatesBudget <= 0)
                {
                    return true;
                }

                updatesBudget--;

                int lastIndex = _lightingQueue.Count - 1;
                LightUpdate updateTask = _lightingQueue[lastIndex];

                _lightingQueue.RemoveAt(lastIndex);
                updateTask.updateLight(this);
            }

            return false;
        }
        finally
        {
            --_lightingUpdatesCounter;
        }
    }

    public void queueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ) => queueLightUpdate(type, minX, minY, minZ, maxX, maxY, maxZ, true);

    public void queueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ,
        bool attemptMerge)
    {
        if (Dimension.HasCeiling && type == LightType.Sky)
        {
            return;
        }

        ++_lightingUpdatesScheduled;

        try
        {
            if (_lightingUpdatesScheduled == 50)
            {
                return;
            }

            int centerX = (maxX + minX) / 2;
            int centerZ = (maxZ + minZ) / 2;

            if (isPosLoaded(centerX, 64, centerZ))
            {
                if (GetChunkFromPos(centerX, centerZ).IsEmpty())
                {
                    return;
                }

                int queueSize = _lightingQueue.Count;
                Span<LightUpdate> span = CollectionsMarshal.AsSpan(_lightingQueue);

                if (attemptMerge)
                {
                    int lookbackCount = Math.Min(5, queueSize);

                    for (int i = 0; i < lookbackCount; ++i)
                    {
                        ref LightUpdate existingUpdate = ref span[queueSize - i - 1];
                        if (existingUpdate.lightType == type &&
                            existingUpdate.expand(minX, minY, minZ, maxX, maxY, maxZ))
                        {
                            return;
                        }
                    }
                }

                _lightingQueue.Add(new LightUpdate(type, minX, minY, minZ, maxX, maxY, maxZ));

                const int maxQueueCapacity = 1000000;
                if (_lightingQueue.Count > maxQueueCapacity)
                {
                    _logger.LogInformation($"More than {maxQueueCapacity} updates, aborting lighting updates");
                    _lightingQueue.Clear();
                }
            }
        }
        finally
        {
            --_lightingUpdatesScheduled;
        }
    }

    public void allowSpawning(bool allowMonsterSpawning, bool allowMobSpawning)
    {
        _spawnHostileMobs = allowMonsterSpawning;
        _spawnPeacefulMobs = allowMobSpawning;
    }

    public virtual void Tick()
    {
        Environment.UpdateWeatherCycles();

        long nextWorldTime;

        if (Environment.CanSkipNight())
        {
            bool wasSpawnInterrupted = false;

            if (_spawnHostileMobs && Difficulty >= 1)
            {
                wasSpawnInterrupted = NaturalSpawner.SpawnMonstersAndWakePlayers(this, _pathFinder, Entities.Players);
            }

            if (!wasSpawnInterrupted)
            {
                nextWorldTime = Properties.WorldTime + 24000L;
                Properties.WorldTime = nextWorldTime - nextWorldTime % 24000L;
                Environment.AfterSkipNight();
            }
        }

        Profiler.Start("performSpawning");
        NaturalSpawner.DoSpawning(this, _pathFinder, _spawnHostileMobs, _spawnPeacefulMobs);
        Profiler.Stop("performSpawning");

        Profiler.Start("unload100OldestChunks");
        _chunkSource.Tick();
        Profiler.Stop("unload100OldestChunks");

        Profiler.Start("updateSkylightSubtracted");
        int currentAmbientDarkness = Environment.GetAmbientDarkness(1.0F);
        if (currentAmbientDarkness != ambientDarkness)
        {
            ambientDarkness = currentAmbientDarkness;

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
            saveWithLoadingDisplay(false, null);
            Profiler.PopGroup();
        }

        Properties.WorldTime = nextWorldTime;

        Profiler.Start("tickUpdates");
        ProcessScheduledTicks(false);
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
            Chunk currentChunk = GetChunk(chunkPos.X, chunkPos.Z);

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
                if (blockId == 0 && getBrightness(worldX, localY, worldZ) <= random.NextInt(8) &&
                    getBrightness(LightType.Sky, worldX, localY, worldZ) <= 0)
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
                int worldY = getTopSolidBlockY(worldX, worldZ);

                if (Environment.IsRainingAt(worldX, worldY, worldZ))
                {
                    Entities.SpawnGlobalEntity(new EntityLightningBolt(this, worldX, worldY, worldZ));
                    TicksSinceLightning = 2;
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
                int worldY = getTopSolidBlockY(worldX, worldZ);

                if (getBiomeSource().GetBiome(worldX, worldZ).GetEnableSnow() && worldY >= 0 && worldY < 128 &&
                    currentChunk.GetLight(LightType.Block, localX, worldY, localZ) < 10)
                {
                    int blockBelowId = currentChunk.GetBlockId(localX, worldY - 1, localZ);
                    int currentBlockId = currentChunk.GetBlockId(localX, worldY, localZ);

                    if (Environment.IsRaining && currentBlockId == 0 && Block.Snow.canPlaceAt(this, worldX, worldY, worldZ) &&
                        blockBelowId != 0 && blockBelowId != Block.Ice.id &&
                        Block.Blocks[blockBelowId].material.BlocksMovement)
                    {
                        setBlock(worldX, worldY, worldZ, Block.Snow.id);
                    }

                    if (blockBelowId == Block.Water.id && currentChunk.GetBlockMeta(localX, worldY - 1, localZ) == 0)
                    {
                        setBlock(worldX, worldY - 1, worldZ, Block.Ice.id);
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
                    Block.Blocks[blockId].onTick(this, localX + worldXBase, localY, localZ + worldZBase, random);
                }
            }
        }
    }

    protected virtual void ProcessScheduledTicks(bool forceFlush)
    {
        for (int i = 0; i < 1000; ++i)
        {
            if (_scheduledUpdates.Count == 0)
            {
                break;
            }

            if (!forceFlush && _scheduledUpdates.Peek().ScheduledTime > GetEventTime())
            {
                break;
            }

            BlockUpdate blockUpdate = _scheduledUpdates.Dequeue();

            const byte loadRadius = 8;
            if (isRegionLoaded(blockUpdate.X - loadRadius, blockUpdate.Y - loadRadius, blockUpdate.Z - loadRadius,
                    blockUpdate.X + loadRadius, blockUpdate.Y + loadRadius, blockUpdate.Z + loadRadius))
            {
                int currentBlockId = getBlockId(blockUpdate.X, blockUpdate.Y, blockUpdate.Z);
                if (currentBlockId == blockUpdate.BlockId && currentBlockId > 0)
                {
                    Block.Blocks[currentBlockId].onTick(this, blockUpdate.X, blockUpdate.Y, blockUpdate.Z, random);
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

            int blockId = getBlockId(targetX, targetY, targetZ);
            if (blockId > 0)
            {
                Block.Blocks[blockId].randomDisplayTick(this, targetX, targetY, targetZ, particleRandom);
            }
        }
    }

    public List<Entity> getEntities(Entity? excludeEntity, Box area) => getEntities(excludeEntity, area, new List<Entity>());

    public List<Entity> getEntities(Entity? excludeEntity, Box area, List<Entity> results)
    {
        int minChunkX = MathHelper.Floor((area.MinX - 2.0D) / 16.0D);
        int maxChunkX = MathHelper.Floor((area.MaxX + 2.0D) / 16.0D);
        int minChunkZ = MathHelper.Floor((area.MinZ - 2.0D) / 16.0D);
        int maxChunkZ = MathHelper.Floor((area.MaxZ + 2.0D) / 16.0D);

        for (int chunkX = minChunkX; chunkX <= maxChunkX; ++chunkX)
        {
            for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; ++chunkZ)
            {
                if (hasChunk(chunkX, chunkZ))
                {
                    GetChunk(chunkX, chunkZ).CollectOtherEntities(excludeEntity, area, results);
                }
            }
        }

        return results;
    }

    public List<T> CollectEntitiesOfType<T>(Box area) where T : Entity
    {
        List<T> results = new();

        int minChunkX = MathHelper.Floor((area.MinX - 2.0D) / 16.0D);
        int maxChunkX = MathHelper.Floor((area.MaxX + 2.0D) / 16.0D);
        int minChunkZ = MathHelper.Floor((area.MinZ - 2.0D) / 16.0D);
        int maxChunkZ = MathHelper.Floor((area.MaxZ + 2.0D) / 16.0D);

        for (int chunkX = minChunkX; chunkX <= maxChunkX; ++chunkX)
        {
            for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; ++chunkZ)
            {
                if (hasChunk(chunkX, chunkZ))
                {
                    GetChunk(chunkX, chunkZ).CollectEntitiesOfType(area, results);
                }
            }
        }

        return results;
    }

    public void updateBlockEntity(int x, int y, int z, BlockEntity blockEntity)
    {
        if (isPosLoaded(x, y, z))
        {
            GetChunkFromPos(x, z).MarkDirty();
        }

        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].updateBlockEntity(x, y, z, blockEntity);
        }
    }

    public void TickChunks()
    {
        while (_chunkSource.Tick())
        {
        }
    }

    public bool canPlace(int blockId, int x, int y, int z, bool isFallingBlock, int side)
    {
        int existingBlockId = getBlockId(x, y, z);
        Block? existingBlock = Block.Blocks[existingBlockId];
        Block? newBlock = Block.Blocks[blockId];

        Box? collisionBox = newBlock?.getCollisionShape(this, x, y, z);

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

        return blockId > 0 && existingBlock == null && newBlock != null && newBlock.canPlaceAt(this, x, y, z, side);
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

    private bool isStrongPoweringSide(int x, int y, int z, int side)
    {
        int blockId = getBlockId(x, y, z);
        return blockId != 0 && Block.Blocks[blockId].isStrongPoweringSide(this, x, y, z, side);
    }

    public bool isStrongPowered(int x, int y, int z)
    {
        if (isStrongPoweringSide(x, y - 1, z, 0))
        {
            return true; // Down
        }

        if (isStrongPoweringSide(x, y + 1, z, 1))
        {
            return true; // Up
        }

        if (isStrongPoweringSide(x, y, z - 1, 2))
        {
            return true; // North
        }

        if (isStrongPoweringSide(x, y, z + 1, 3))
        {
            return true; // South
        }

        if (isStrongPoweringSide(x - 1, y, z, 4))
        {
            return true; // West
        }

        return isStrongPoweringSide(x + 1, y, z, 5); // East
    }

    public bool isPoweringSide(int x, int y, int z, int side)
    {
        if (shouldSuffocate(x, y, z))
        {
            return isStrongPowered(x, y, z);
        }

        int blockId = getBlockId(x, y, z);
        return blockId != 0 && Block.Blocks[blockId].isPoweringSide(this, x, y, z, side);
    }

    public bool isPowered(int x, int y, int z)
    {
        if (isPoweringSide(x, y - 1, z, 0))
        {
            return true; // Down
        }

        if (isPoweringSide(x, y + 1, z, 1))
        {
            return true; // Up
        }

        if (isPoweringSide(x, y, z - 1, 2))
        {
            return true; // North
        }

        if (isPoweringSide(x, y, z + 1, 3))
        {
            return true; // South
        }

        if (isPoweringSide(x - 1, y, z, 4))
        {
            return true; // West
        }

        return isPoweringSide(x + 1, y, z, 5); // East
    }

    public EntityPlayer getClosestPlayer(Entity entity, double range) => Entities.GetClosestPlayer(entity.x, entity.y, entity.z, range);

    public void handleChunkDataUpdate(int x, int y, int z, int sizeX, int sizeY, int sizeZ, byte[] chunkData)
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

                currentBufferOffset = GetChunk(chunkX, chunkZ).LoadFromPacket(
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

                currentBufferOffset = GetChunk(chunkX, chunkZ).ToPacket(
                    chunkData,
                    localStartX, minY, localStartZ,
                    localEndX, maxY, localEndZ,
                    currentBufferOffset);
            }
        }

        return chunkData;
    }

    public void checkSessionLock() => Storage.CheckSessionLock();

    public void setTime(long time) => Properties.WorldTime = time;

    public void synchronizeTimeAndUpdates(long time)
    {
        long deltaTime = time - Properties.WorldTime;
        _eventDeltaTime -= deltaTime;
        setTime(time);
    }

    public long getSeed() => Properties.RandomSeed;

    public long getTime() => Properties.WorldTime;

    private long GetEventTime() => Properties.WorldTime + _eventDeltaTime;

    public Vec3i getSpawnPos() => new(Properties.SpawnX, Properties.SpawnY, Properties.SpawnZ);

    public void setSpawnPos(Vec3i pos) => Properties.SetSpawn(pos.X, pos.Y, pos.Z);

    public virtual bool canInteract(EntityPlayer player, int x, int y, int z) => true;

    public virtual void broadcastEntityEvent(Entity entity, byte @event)
    {
    }

    public ChunkSource GetChunkSource() => _chunkSource;

    public virtual void playNoteBlockActionAt(int x, int y, int z, int soundType, int pitch)
    {
        int blockId = getBlockId(x, y, z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].onBlockAction(this, x, y, z, soundType, pitch);
        }
    }

    public void setState(string id, PersistentState state) => PersistentStateManager.SetData(id, state);

    public PersistentState? getOrCreateState(Type type, string id) => PersistentStateManager.LoadData(type, id);

    public int getIdCount(string id) => PersistentStateManager.GetUniqueDataId(id);

    public void worldEvent(int @event, int x, int y, int z, int data) => worldEvent(null, @event, x, y, z, data);

    public void worldEvent(EntityPlayer player, int @event, int x, int y, int z, int data)
    {
        for (int index = 0; index < EventListeners.Count; ++index)
        {
            EventListeners[index].worldEvent(player, @event, x, y, z, data);
        }
    }
}
