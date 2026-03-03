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
using BetaSharp.Worlds.Biomes;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Chunks.Light;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace BetaSharp.Worlds;

public abstract class World : IBlockAccess
{
    private readonly ILogger<World> _logger = Log.Instance.For<World>();
    protected readonly IWorldStorage Storage;
    public readonly Dimension dimension;
    private ChunkSource _chunkSource;
    public PersistentStateManager persistentStateManager;
    protected WorldProperties Properties;
    protected List<IWorldAccess> EventListeners = [];

    public RuleSet Rules { get; protected set; }
    public bool isNewWorld;
    public bool isRemote = false;
    public int difficulty;
    private bool _spawnHostileMobs = true;
    private bool _spawnPeacefulMobs = true;


    private static readonly int s_autosavePeriod = 40;
    protected int AutosavePeriod = s_autosavePeriod;
    public bool pauseTicking = false;
    public bool eventProcessingEnabled;
    private bool _processingDeferred;
    public bool instantBlockUpdateEnabled = false;
    private readonly PriorityQueue<BlockUpdate, (long, long)> _scheduledUpdates = new();

    private long _eventDeltaTime = 0; // difference between world time and the scheduled time of the block events so things don't break when using the time command

    private readonly long _worldTimeMask = 0xFFFFFFL;

    public List<EntityPlayer> players = [];
    private bool _allPlayersSleeping;
    public List<Entity> entities = [];
    private readonly Dictionary<int, Entity> _entitiesById = new();
    public List<Entity> globalEntities = [];
    private readonly List<Entity> _entitiesToUnload = [];

    [ThreadStatic]
    private static List<Entity>? _tempCollisionEntities;

    // Reusable scratch list for GetEntityCollisionsScratch — avoids per-call List<Box> allocation.
    [ThreadStatic]
    private static List<Box>? _tempCollisionBoxes;

    // Reusable scratch list for GetEntitiesScratch — avoids per-call List<Entity> allocation.
    [ThreadStatic]
    private static List<Entity>? _tempCollisionEntitiesResult;

    private readonly HashSet<ChunkPos> _activeChunks = new();
    public List<BlockEntity> blockEntities = [];
    private readonly List<BlockEntity> _blockEntityUpdateQueue = [];

    public int ambientDarkness = 0;
    private readonly List<LightUpdate> _lightingQueue = [];
    private int _lightingUpdatesCounter = 0;
    private int _lightingUpdatesScheduled;

    protected float PrevRainingStrength;
    protected float RainingStrength;
    protected float PrevThunderingStrength;
    protected float ThunderingStrength;
    protected int TicksSinceLightning = 0;
    public int lightningTicksLeft = 0;

    public JavaRandom random = new();
    private int _lcgBlockSeed = Random.Shared.Next();
    private int _soundCounter = Random.Shared.Next(12000);

    private PathFinder _pathFinder;

    protected World(IWorldStorage worldStorage, string levelName, Dimension dim, long seed)
    {
        _pathFinder = new(this);
        Storage = worldStorage;
        persistentStateManager = new PersistentStateManager(worldStorage);
        Properties = new WorldProperties(seed, levelName);
        dimension = dim;
        dim.SetWorld(this);
        _chunkSource = CreateChunkCache();
        Rules = Properties.RulesTag != null
            ? RuleSet.FromNBT(RuleRegistry.Instance, Properties.RulesTag)
            : new RuleSet(RuleRegistry.Instance);
        updateSkyBrightness();
        prepareWeather();
    }

    protected World(IWorldStorage worldStorage, string levelName, long seed, Dimension? dim)
    {
        _pathFinder = new(this);
        Storage = worldStorage;
        persistentStateManager = new PersistentStateManager(worldStorage);
        Properties = worldStorage.LoadProperties();

        isNewWorld = Properties == null;

        if (dim != null)
        {
            dimension = dim;
        }
        else if (Properties != null && Properties.Dimension == -1)
        {
            dimension = Dimension.FromId(-1);
        }
        else
        {
            dimension = Dimension.FromId(0);
        }

        bool shouldInitializeSpawn = false;
        if (Properties == null)
        {
            Properties = new WorldProperties(seed, levelName);
            shouldInitializeSpawn = true;
        }
        else
        {
            Properties.LevelName = levelName;
        }

        dimension.SetWorld(this);
        _chunkSource = CreateChunkCache();

        Rules = Properties.RulesTag != null
            ? RuleSet.FromNBT(RuleRegistry.Instance, Properties.RulesTag)
            : new RuleSet(RuleRegistry.Instance);

        if (shouldInitializeSpawn)
        {
            initializeSpawnPoint();
        }

        updateSkyBrightness();
        prepareWeather();
    }

    public BiomeSource getBiomeSource()
    {
        return dimension.BiomeSource;
    }

    public IWorldStorage getWorldStorage()
    {
        return Storage;
    }

    protected abstract ChunkSource CreateChunkCache();

    private void initializeSpawnPoint()
    {
        eventProcessingEnabled = true;
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

            SpawnEntity(player);
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

        Storage.Save(Properties, players.ToList());
        Profiler.Stop("saveWorldInfoAndPlayer");

        Profiler.Start("saveAllData");
        persistentStateManager.SaveAllData();
        Profiler.Stop("saveAllData");
    }

    public bool attemptSaving(int i)
    {
        if (!_chunkSource.CanSave())
        {
            return true;
        }
        else
        {
            if (i == 0)
            {
                save();
            }

            return _chunkSource.Save(false, null);
        }
    }

    public int getBlockId(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128) return 0;
        return GetChunk(x >> 4, z >> 4).GetBlockId(x & 15, y, z & 15);
    }

    public bool isAir(int x, int y, int z)
    {
        return getBlockId(x, y, z) == 0;
    }

    public bool isPosLoaded(int x, int y, int z)
    {
        return y >= 0 && y < 128 ? hasChunk(x >> 4, z >> 4) : false;
    }

    public bool isRegionLoaded(int x, int y, int z, int range)
    {
        return isRegionLoaded(x - range, y - range, z - range, x + range, y + range, z + range);
    }

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
        else
        {
            return false;
        }
    }

    private bool hasChunk(int x, int z)
    {
        return _chunkSource.IsChunkLoaded(x, z);
    }

    public Chunk GetChunkFromPos(int x, int z)
    {
        return GetChunk(x >> 4, z >> 4);
    }

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        return _chunkSource.GetChunk(chunkX, chunkZ);
    }

    public virtual bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128) return false;
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
            else if (y >= 128)
            {
                return false;
            }
            else
            {
                Chunk chunk = GetChunk(x >> 4, z >> 4);
                return chunk.SetBlock(x & 15, y, z & 15, blockId);
            }
        }
        else
        {
            return false;
        }
    }

    public Material getMaterial(int x, int y, int z)
    {
        int blockId = getBlockId(x, y, z);
        return blockId == 0 ? Material.Air : Block.Blocks[blockId].material;
    }

    public int getBlockMeta(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128) return 0;
        return GetChunk(x >> 4, z >> 4).GetBlockMeta(x & 15, y, z & 15);
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
            else if (y >= 128)
            {
                return false;
            }
            else
            {
                Chunk chunk = GetChunk(x >> 4, z >> 4);
                x &= 15;
                z &= 15;
                chunk.SetBlockMeta(x, y, z, meta);
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    public bool setBlock(int x, int y, int z, int blockId)
    {
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId))
        {
            blockUpdate(x, y, z, blockId);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool setBlock(int x, int y, int z, int blockId, int meta)
    {
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, meta))
        {
            blockUpdate(x, y, z, blockId);
            return true;
        }
        else
        {
            return false;
        }
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
        if (!pauseTicking && !isRemote)
        {
            Block block = Block.Blocks[getBlockId(x, y, z)];
            if (block != null)
            {
                block.neighborUpdate(this, x, y, z, blockId);
            }
        }
    }

    public bool hasSkyLight(int x, int y, int z)
    {
        return GetChunk(x >> 4, z >> 4).IsAboveMaxHeight(x & 15, y, z & 15);
    }

    public int getBrightness(int x, int y, int z)
    {
        if (y < 0)
        {
            return 0;
        }
        else
        {
            if (y >= 128)
            {
                return !dimension.HasCeiling ? 15 : 0;
            }

            return GetChunk(x >> 4, z >> 4).GetLight(x & 15, y, z & 15, 0);
        }
    }

    public int getLightLevel(int x, int y, int z)
    {
        return getLightLevel(x, y, z, true);
    }

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

                if (lightPosX > neighborMaxLight) neighborMaxLight = lightPosX;
                if (lightNegX > neighborMaxLight) neighborMaxLight = lightNegX;
                if (lightPosZ > neighborMaxLight) neighborMaxLight = lightPosZ;
                if (lightNegZ > neighborMaxLight) neighborMaxLight = lightNegZ;

                return neighborMaxLight;
            }
        }

        if (y < 0) return 0;

        if (y >= 128)
        {
            return !dimension.HasCeiling ? 15 - ambientDarkness : 0;
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
            else if (y >= 128)
            {
                return true;
            }
            else if (!hasChunk(x >> 4, z >> 4))
            {
                return false;
            }
            else
            {
                Chunk chunk = GetChunk(x >> 4, z >> 4);
                x &= 15;
                z &= 15;
                return chunk.IsAboveMaxHeight(x, y, z);
            }
        }
        else
        {
            return false;
        }
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
        if (dimension.HasCeiling && lightType == LightType.Sky)
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

    public float getNaturalBrightness(int x, int y, int z, int blockLight)
    {
        int lightLevel = getLightLevel(x, y, z);
        if (lightLevel < blockLight)
        {
            lightLevel = blockLight;
        }

        return dimension.LightLevelToLuminance[lightLevel];
    }

    public float getLuminance(int x, int y, int z)
    {
        return dimension.LightLevelToLuminance[getLightLevel(x, y, z)];
    }

    public bool canMonsterSpawn()
    {
        return ambientDarkness < 4;
    }

    public HitResult raycast(Vec3D start, Vec3D end)
    {
        return raycast(start, end, false, false);
    }

    public HitResult raycast(Vec3D start, Vec3D end, bool bl)
    {
        return raycast(start, end, bl, false);
    }

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
            if (result.Type != HitResultType.MISS) return result;
        }

        int iterationsRemaining = 200;
        while (iterationsRemaining-- >= 0)
        {
            if (double.IsNaN(start.x) || double.IsNaN(start.y) || double.IsNaN(start.z))
                return new HitResult(HitResultType.MISS);
            if (currentX == targetX && currentY == targetY && currentZ == targetZ)
                return new HitResult(HitResultType.MISS);

            bool canMoveX = true, canMoveY = true, canMoveZ = true;
            double nextBoundaryX = 999.0D, nextBoundaryY = 999.0D, nextBoundaryZ = 999.0D;

            if (targetX > currentX) nextBoundaryX = currentX + 1.0D;
            else if (targetX < currentX) nextBoundaryX = currentX + 0.0D;
            else canMoveX = false;

            if (targetY > currentY) nextBoundaryY = currentY + 1.0D;
            else if (targetY < currentY) nextBoundaryY = currentY + 0.0D;
            else canMoveY = false;

            if (targetZ > currentZ) nextBoundaryZ = currentZ + 1.0D;
            else if (targetZ < currentZ) nextBoundaryZ = currentZ + 0.0D;
            else canMoveZ = false;

            double deltaX = target.x - start.x;
            double deltaY = target.y - start.y;
            double deltaZ = target.z - start.z;

            double scaleX = 999.0D, scaleY = 999.0D, scaleZ = 999.0D;
            if (canMoveX) scaleX = (nextBoundaryX - start.x) / deltaX;
            if (canMoveY) scaleY = (nextBoundaryY - start.y) / deltaY;
            if (canMoveZ) scaleZ = (nextBoundaryZ - start.z) / deltaZ;

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

            Vec3D currentStepPos = new Vec3D(start.x, start.y, start.z);
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
                if (hit.Type != HitResultType.MISS) return hit;
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

    public virtual bool spawnGlobalEntity(Entity entity)
    {
        globalEntities.Add(entity);
        return true;
    }

    public virtual bool SpawnEntity(Entity entity)
    {
        int chunkX = MathHelper.Floor(entity.x / 16.0D);
        int chunkZ = MathHelper.Floor(entity.z / 16.0D);
        bool isPlayer = entity is EntityPlayer;

        if (!isPlayer && !hasChunk(chunkX, chunkZ))
        {
            return false;
        }

        if (entity is EntityPlayer player)
        {
            players.Add(player);
            updateSleepingPlayers();
        }

        GetChunk(chunkX, chunkZ).AddEntity(entity);
        entities.Add(entity);
        _entitiesById[entity.id] = entity;
        NotifyEntityAdded(entity);
        return true;
    }

    protected virtual void NotifyEntityAdded(Entity entity)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].notifyEntityAdded(entity);
        }
    }

    protected virtual void NotifyEntityRemoved(Entity entity)
    {
        for (int i = 0; i < EventListeners.Count; ++i)
        {
            EventListeners[i].notifyEntityRemoved(entity);
        }
    }

    public virtual void Remove(Entity entity)
    {
        if (entity.passenger != null)
        {
            entity.passenger.setVehicle(null);
        }

        if (entity.vehicle != null)
        {
            entity.setVehicle(null);
        }

        entity.markDead();
        if (entity is EntityPlayer)
        {
            players.Remove((EntityPlayer)entity);
            updateSleepingPlayers();
        }
    }

    public void serverRemove(Entity entity)
    {
        entity.markDead();
        if (entity is EntityPlayer player)
        {
            players.Remove(player);
            this.updateSleepingPlayers();
        }

        int chunkX = entity.chunkX;
        int chunkZ = entity.chunkZ;
        if (entity.isPersistent && hasChunk(chunkX, chunkZ))
        {
            GetChunk(chunkX, chunkZ).RemoveEntity(entity);
        }

        entities.Remove(entity);
        _entitiesById.Remove(entity.id);
        NotifyEntityRemoved(entity);
    }

    public void addWorldAccess(IWorldAccess worldAccess)
    {
        EventListeners.Add(worldAccess);
    }

    public void removeWorldAccess(IWorldAccess worldAccess)
    {
        EventListeners.Remove(worldAccess);
    }

    public List<Box> GetEntityCollisions(Entity entity, Box area)
    {
        return GetEntityCollisions(entity, area, new List<Box>());
    }

    /// <summary>
    /// Like <see cref="GetEntityCollisions(Entity,Box)"/> but writes into a reused thread-static
    /// scratch list instead of allocating a new one. The returned list is only valid until the
    /// next call to this method on the same thread — do not store it.
    /// </summary>
    internal List<Box> GetEntityCollisionsScratch(Entity entity, Box area)
    {
        _tempCollisionBoxes ??= new List<Box>();
        _tempCollisionBoxes.Clear();
        return GetEntityCollisions(entity, area, _tempCollisionBoxes);
    }

    private List<Box> GetEntityCollisions(Entity entity, Box area, List<Box> collidingBoundingBoxes)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0D);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0D);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0D);

        for (int x = minX; x < maxX; ++x)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                if (isPosLoaded(x, 64, z))
                {
                    for (int y = minY - 1; y < maxY; ++y)
                    {
                        Block block = Block.Blocks[getBlockId(x, y, z)];
                        if (block != null)
                        {
                            block.addIntersectingBoundingBox(this, x, y, z, area, collidingBoundingBoxes);
                        }
                    }
                }
            }
        }

        const double expansion = 0.25D;
        _tempCollisionEntities ??= new List<Entity>();
        _tempCollisionEntities.Clear();

        getEntities(entity, area.Expand(expansion, expansion, expansion), _tempCollisionEntities);

        int collisionCount = 0;
        int maxCollisions = Rules.GetInt(DefaultRules.MaxCollisions);

        for (int i = 0; i < _tempCollisionEntities.Count; ++i)
        {
            if (collisionCount >= maxCollisions)
            {
                break;
            }

            Box? entityBox = _tempCollisionEntities[i].getBoundingBox();
            if (entityBox != null && entityBox.Value.Intersects(area))
            {
                collidingBoundingBoxes.Add(entityBox.Value);
                collisionCount++;
            }

            entityBox = entity.getCollisionAgainstShape(_tempCollisionEntities[i]);
            if (entityBox != null && entityBox.Value.Intersects(area))
            {
                collidingBoundingBoxes.Add(entityBox.Value);
                collisionCount++;
            }
        }

        return collidingBoundingBoxes;
    }

    protected int getAmbientDarkness(float delta)
    {
        float timeOfDay = getTime(delta);

        float sunIntensity = 1.0F - (MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 0.5F);
        sunIntensity = Math.Clamp(sunIntensity, 0.0F, 1.0F);

        float lightLevel = 1.0F - sunIntensity;
        lightLevel = (float)(lightLevel * (1.0D - (getRainGradient(delta) * 5.0F) / 16.0D));
        lightLevel = (float)(lightLevel * (1.0D - (getThunderGradient(delta) * 5.0F) / 16.0D));

        float finalDarknessFactor = 1.0F - lightLevel;
        return (int)(finalDarknessFactor * 11.0F);
    }

    public Vector3D<double> getSkyColor(Entity entity, float partialTicks)
    {
        float timeOfDay = getTime(partialTicks);

        float sunIntensity = MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 0.5F;
        sunIntensity = Math.Clamp(sunIntensity, 0.0F, 1.0F);

        int blockX = MathHelper.Floor(entity.x);
        int blockZ = MathHelper.Floor(entity.z);
        float temperature = (float)getBiomeSource().GetTemperature(blockX, blockZ);
        int biomeSkyColorInt = getBiomeSource().GetBiome(blockX, blockZ).GetSkyColorByTemp(temperature);

        float red = (biomeSkyColorInt >> 16 & 255) / 255.0F;
        float green = (biomeSkyColorInt >> 8 & 255) / 255.0F;
        float blue = (biomeSkyColorInt & 255) / 255.0F;

        red *= sunIntensity;
        green *= sunIntensity;
        blue *= sunIntensity;

        float rainStrength = getRainGradient(partialTicks);
        if (rainStrength > 0.0F)
        {
            float grayscaleLuminance = (red * 0.3F + green * 0.59F + blue * 0.11F) * 0.6F;
            float rainFactor = 1.0F - rainStrength * (12.0F / 16.0F);

            red = red * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
            green = green * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
            blue = blue * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
        }

        float thunderStrength = getThunderGradient(partialTicks);
        if (thunderStrength > 0.0F)
        {
            float grayscaleLuminance = (red * 0.3F + green * 0.59F + blue * 0.11F) * 0.2F;
            float thunderFactor = 1.0F - thunderStrength * (12.0F / 16.0F);

            red = red * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
            green = green * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
            blue = blue * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
        }

        if (lightningTicksLeft > 0)
        {
            float lightningFactor = lightningTicksLeft - partialTicks;
            if (lightningFactor > 1.0F) lightningFactor = 1.0F;

            lightningFactor *= 0.45F;

            red = red * (1.0F - lightningFactor) + 0.8F * lightningFactor;
            green = green * (1.0F - lightningFactor) + 0.8F * lightningFactor;
            blue = blue * (1.0F - lightningFactor) + 1.0F * lightningFactor;
        }

        return new(red, green, blue);
    }

    public float getTime(float delta)
    {
        return dimension.GetTimeOfDay(Properties.WorldTime, delta);
    }

    public Vector3D<double> getCloudColor(float partialTicks)
    {
        float timeOfDay = getTime(partialTicks);

        float sunIntensity = MathHelper.Cos(timeOfDay * (float)Math.PI * 2.0F) * 2.0F + 0.5F;
        sunIntensity = Math.Clamp(sunIntensity, 0.0F, 1.0F);

        float red = (_worldTimeMask >> 16 & 255L) / 255.0F;
        float green = (_worldTimeMask >> 8 & 255L) / 255.0F;
        float blue = (_worldTimeMask & 255L) / 255.0F;

        float rainStrength = getRainGradient(partialTicks);
        if (rainStrength > 0.0F)
        {
            float grayscaleLuminance = (red * 0.3F + green * 0.59F + blue * 0.11F) * 0.6F;
            float rainFactor = 1.0F - rainStrength * 0.95F;

            red = red * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
            green = green * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
            blue = blue * rainFactor + grayscaleLuminance * (1.0F - rainFactor);
        }

        red *= sunIntensity * 0.9F + 0.1F;
        green *= sunIntensity * 0.9F + 0.1F;
        blue *= sunIntensity * 0.85F + 0.15F;

        float thunderStrength = getThunderGradient(partialTicks);
        if (thunderStrength > 0.0F)
        {
            float grayscaleLuminance = (red * 0.3F + green * 0.59F + blue * 0.11F) * 0.2F;
            float thunderFactor = 1.0F - thunderStrength * 0.95F;

            red = red * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
            green = green * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
            blue = blue * thunderFactor + grayscaleLuminance * (1.0F - thunderFactor);
        }

        return new(red, green, blue);
    }

    public Vector3D<double> getFogColor(float partialTicks)
    {
        float timeOfDay = getTime(partialTicks);
        return dimension.GetFogColor(timeOfDay, partialTicks);
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

    public void tickEntities()
    {
        Profiler.Start("updateEntites.updateWeatherEffects");
        for (int i = 0; i < globalEntities.Count; ++i)
        {
            Entity globalEntity = globalEntities[i];
            globalEntity.tick();

            if (globalEntity.dead)
            {
                globalEntities.RemoveAt(i--);
            }
        }
        Profiler.Stop("updateEntites.updateWeatherEffects");

        Profiler.Start("updateEntites.clearUnloadedEntities");
        for (int i = 0; i < _entitiesToUnload.Count; ++i)
        {
            Entity entityToUnload = _entitiesToUnload[i];
            int chunkX = entityToUnload.chunkX;
            int chunkZ = entityToUnload.chunkZ;

            if (entityToUnload.isPersistent && hasChunk(chunkX, chunkZ))
            {
                GetChunk(chunkX, chunkZ).RemoveEntity(entityToUnload);
            }
        }

        for (int i = 0; i < _entitiesToUnload.Count; ++i)
        {
            NotifyEntityRemoved(_entitiesToUnload[i]);
        }

        _entitiesToUnload.Clear();
        Profiler.Stop("updateEntites.clearUnloadedEntities");

        Profiler.Start("updateEntites.updateLoadedEntities");
        for (int i = 0; i < entities.Count; ++i)
        {
            Entity entity = entities[i];

            if (entity.vehicle != null)
            {
                if (!entity.vehicle.dead && entity.vehicle.passenger == entity)
                {
                    continue;
                }

                entity.vehicle.passenger = null;
                entity.vehicle = null;
            }

            if (!entity.dead)
            {
                updateEntity(entity);
            }

            if (entity.dead)
            {
                int chunkX = entity.chunkX;
                int chunkZ = entity.chunkZ;

                if (entity.isPersistent && hasChunk(chunkX, chunkZ))
                {
                    GetChunk(chunkX, chunkZ).RemoveEntity(entity);
                }

                entities.RemoveAt(i--);
                _entitiesById.Remove(entity.id);
                NotifyEntityRemoved(entity);
            }
        }

        Profiler.Stop("updateEntites.updateLoadedEntities");

        _processingDeferred = true;
        Profiler.Start("updateEntites.updateLoadedTileEntities");

        for (int i = blockEntities.Count - 1; i >= 0; i--)
        {
            BlockEntity blockEntity = blockEntities[i];
            if (!blockEntity.isRemoved())
            {
                blockEntity.tick();
            }

            if (blockEntity.isRemoved())
            {
                blockEntities.RemoveAt(i);
                Chunk chunk = GetChunk(blockEntity.X >> 4, blockEntity.Z >> 4);
                if (chunk != null)
                {
                    chunk.RemoveBlockEntityAt(blockEntity.X & 15, blockEntity.Y, blockEntity.Z & 15);
                }
            }
        }

        _processingDeferred = false;

        if (_blockEntityUpdateQueue.Count > 0)
        {
            foreach (BlockEntity queuedBlockEntity in _blockEntityUpdateQueue)
            {
                if (!queuedBlockEntity.isRemoved())
                {
                    if (!blockEntities.Contains(queuedBlockEntity))
                    {
                        blockEntities.Add(queuedBlockEntity);
                    }

                    Chunk chunk = GetChunk(queuedBlockEntity.X >> 4, queuedBlockEntity.Z >> 4);
                    if (chunk != null)
                    {
                        chunk.SetBlockEntity(queuedBlockEntity.X & 15, queuedBlockEntity.Y, queuedBlockEntity.Z & 15,
                            queuedBlockEntity);
                    }

                    blockUpdateEvent(queuedBlockEntity.X, queuedBlockEntity.Y, queuedBlockEntity.Z);
                }
            }

            _blockEntityUpdateQueue.Clear();
        }

        Profiler.Stop("updateEntites.updateLoadedTileEntities");
    }

    public void processBlockUpdates(IEnumerable<BlockEntity> blockUpdates)
    {
        if (_processingDeferred)
        {
            _blockEntityUpdateQueue.AddRange(blockUpdates);
        }
        else
        {
            blockEntities.AddRange(blockUpdates);
        }
    }

    public void updateEntity(Entity entity)
    {
        updateEntity(entity, true);
    }

    public virtual void updateEntity(Entity entity, bool requireLoaded)
    {
        int blockX = MathHelper.Floor(entity.x);
        int blockZ = MathHelper.Floor(entity.z);

        const byte loadRadius = 32;
        if (!requireLoaded || isRegionLoaded(blockX - loadRadius, 0, blockZ - loadRadius,
                blockX + loadRadius, 128, blockZ + loadRadius))
        {
            entity.lastTickX = entity.x;
            entity.lastTickY = entity.y;
            entity.lastTickZ = entity.z;
            entity.prevYaw = entity.yaw;
            entity.prevPitch = entity.pitch;

            if (requireLoaded && entity.isPersistent)
            {
                if (entity.vehicle != null)
                {
                    entity.tickRiding();
                }
                else
                {
                    entity.tick();
                }
            }

            if (double.IsNaN(entity.x) || double.IsInfinity(entity.x)) entity.x = entity.lastTickX;
            if (double.IsNaN(entity.y) || double.IsInfinity(entity.y)) entity.y = entity.lastTickY;
            if (double.IsNaN(entity.z) || double.IsInfinity(entity.z)) entity.z = entity.lastTickZ;
            if (double.IsNaN(entity.pitch) || double.IsInfinity(entity.pitch)) entity.pitch = entity.prevPitch;
            if (double.IsNaN(entity.yaw) || double.IsInfinity(entity.yaw)) entity.yaw = entity.prevYaw;

            int newChunkX = MathHelper.Floor(entity.x / 16.0D);
            int newChunkY = MathHelper.Floor(entity.y / 16.0D);
            int newChunkZ = MathHelper.Floor(entity.z / 16.0D);

            if (!entity.isPersistent || entity.chunkX != newChunkX ||
                entity.chunkSlice != newChunkY || entity.chunkZ != newChunkZ)
            {
                if (entity.isPersistent && hasChunk(entity.chunkX, entity.chunkZ))
                {
                    GetChunk(entity.chunkX, entity.chunkZ).RemoveEntity(entity, entity.chunkSlice);
                }


                if (hasChunk(newChunkX, newChunkZ))
                {
                    entity.isPersistent = true;
                    GetChunk(newChunkX, newChunkZ).AddEntity(entity);
                }
                else
                {
                    entity.isPersistent = false;
                }
            }

            if (requireLoaded && entity.isPersistent && entity.passenger != null)
            {
                if (!entity.passenger.dead && entity.passenger.vehicle == entity)
                {
                    updateEntity(entity.passenger);
                }
                else
                {
                    entity.passenger.vehicle = null;
                    entity.passenger = null;
                }
            }
        }
    }

    public bool canSpawnEntity(Box spawnArea)
    {
        List<Entity> nearbyEntities = new List<Entity>();
        getEntities(null, spawnArea, nearbyEntities);

        for (int i = 0; i < nearbyEntities.Count; ++i)
        {
            Entity entity = nearbyEntities[i];
            if (!entity.dead && entity.preventEntitySpawning)
            {
                return false;
            }
        }

        return true;
    }

    public bool isAnyBlockInBox(Box area)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0);

        if (area.MinX < 0.0) minX--;
        if (area.MinY < 0.0) minY--;
        if (area.MinZ < 0.0) minZ--;

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    if (getBlockId(x, y, z) > 0) return true;
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

        if (area.MinX < 0.0D) minX--;
        if (area.MinY < 0.0D) minY--;
        if (area.MinZ < 0.0D) minZ--;

        for (int x = minX; x < maxX; ++x)
        {
            for (int y = minY; y < maxY; ++y)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    Block block = Block.Blocks[getBlockId(x, y, z)];
                    if (block != null && block.material.IsFluid) return true;
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

        if (!isRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ)) return false;

        bool isSubmerged = false;
        Vec3D flowVector = new Vec3D(0.0D, 0.0D, 0.0D);

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
                    if (block != null && block.material == material) return true;
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
                        double waterLevel = (y + 1);
                        if (meta < 8)
                        {
                            waterLevel = (y + 1) - meta / 8.0D;
                        }

                        if (waterLevel >= area.MinY) return true;
                    }
                }
            }
        }
        return false;
    }

    public Explosion createExplosion(Entity source, double x, double y, double z, float power)
    {
        return createExplosion(source, x, y, z, power, false);
    }

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

    public Entity? getPlayerForProxy(Type type)
    {
        return null;
    }

    public string getEntityCount()
    {
        return "All: " + entities.Count;
    }

    public string getDebugInfo()
    {
        return _chunkSource.GetDebugInfo();
    }

    public BlockEntity? getBlockEntity(int x, int y, int z)
    {
        Chunk? chunk = GetChunk(x >> 4, z >> 4);
        var entity = chunk?.GetBlockEntity(x & 15, y, z & 15) ?? blockEntities.FirstOrDefault(e => e.X == x && e.Y == y && e.Z == z);

        return entity;
    }

    public void setBlockEntity(int x, int y, int z, BlockEntity blockEntity)
    {
        if (!blockEntity.isRemoved())
        {
            if (_processingDeferred)
            {
                blockEntity.X = x;
                blockEntity.Y = y;
                blockEntity.Z = z;
                _blockEntityUpdateQueue.Add(blockEntity);
            }
            else
            {
                blockEntities.Add(blockEntity);
                Chunk chunk = GetChunk(x >> 4, z >> 4);
                if (chunk != null)
                {
                    chunk.SetBlockEntity(x & 15, y, z & 15, blockEntity);
                }
            }
        }
    }

    public void removeBlockEntity(int x, int y, int z)
    {
        BlockEntity? entity = getBlockEntity(x, y, z);
        if (entity != null && _processingDeferred)
        {
            entity.markRemoved();
        }
        else
        {
            Chunk? chunk = GetChunk(x >> 4, z >> 4);
            chunk?.RemoveBlockEntityAt(x & 15, y, z & 15);

            if (entity != null) blockEntities.Remove(entity);
        }
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

    public void savingProgress(LoadingDisplay display)
    {
        saveWithLoadingDisplay(true, display);
    }

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

    public void queueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        queueLightUpdate(type, minX, minY, minZ, maxX, maxY, maxZ, true);
    }

    public void queueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ,
        bool attemptMerge)
    {
        if (dimension.HasCeiling && type == LightType.Sky)
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
                var span = CollectionsMarshal.AsSpan(_lightingQueue);

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

    public void updateSkyBrightness()
    {
        int darkness = getAmbientDarkness(1.0F);
        if (darkness != ambientDarkness)
        {
            ambientDarkness = darkness;
        }
    }

    public void allowSpawning(bool allowMonsterSpawning, bool allowMobSpawning)
    {
        _spawnHostileMobs = allowMonsterSpawning;
        _spawnPeacefulMobs = allowMobSpawning;
    }

    public virtual void Tick()
    {
        UpdateWeatherCycles();

        long nextWorldTime;

        if (canSkipNight())
        {
            bool wasSpawnInterrupted = false;

            if (_spawnHostileMobs && difficulty >= 1)
            {
                wasSpawnInterrupted = NaturalSpawner.SpawnMonstersAndWakePlayers(this, _pathFinder, players);
            }

            if (!wasSpawnInterrupted)
            {
                nextWorldTime = Properties.WorldTime + 24000L;
                Properties.WorldTime = nextWorldTime - (nextWorldTime % 24000L);
                afterSkipNight();
            }
        }

        Profiler.Start("performSpawning");
        NaturalSpawner.DoSpawning(this, _pathFinder, _spawnHostileMobs, _spawnPeacefulMobs);
        Profiler.Stop("performSpawning");

        Profiler.Start("unload100OldestChunks");
        _chunkSource.Tick();
        Profiler.Stop("unload100OldestChunks");

        Profiler.Start("updateSkylightSubtracted");
        int currentAmbientDarkness = getAmbientDarkness(1.0F);
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

    private void prepareWeather()
    {
        if (Properties.IsRaining)
        {
            RainingStrength = 1.0F;
            if (Properties.IsThundering)
            {
                ThunderingStrength = 1.0F;
            }
        }
    }

    protected virtual void UpdateWeatherCycles()
    {
        if (dimension.HasCeiling) return;

        if (TicksSinceLightning > 0)
        {
            --TicksSinceLightning;
        }

        int thunderTime = Properties.ThunderTime;
        if (thunderTime <= 0)
        {
            if (Properties.IsThundering)
            {
                Properties.ThunderTime = random.NextInt(12000) + 3600;
            }
            else
            {
                Properties.ThunderTime = random.NextInt(168000) + 12000;
            }
        }
        else
        {
            --thunderTime;
            Properties.ThunderTime = thunderTime;
            if (thunderTime <= 0)
            {
                Properties.IsThundering = !Properties.IsThundering;
            }
        }

        int rainTime = Properties.RainTime;
        if (rainTime <= 0)
        {
            if (Properties.IsRaining)
            {
                Properties.RainTime = random.NextInt(12000) + 12000;
            }
            else
            {
                Properties.RainTime = random.NextInt(168000) + 12000;
            }
        }
        else
        {
            --rainTime;
            Properties.RainTime = rainTime;
            if (rainTime <= 0)
            {
                Properties.IsRaining = !Properties.IsRaining;
            }
        }

        PrevRainingStrength = RainingStrength;
        if (Properties.IsRaining)
        {
            RainingStrength = (float)(RainingStrength + 0.01D);
        }
        else
        {
            RainingStrength = (float)(RainingStrength - 0.01D);
        }

        RainingStrength = Math.Clamp(RainingStrength, 0.0F, 1.0F);

        PrevThunderingStrength = ThunderingStrength;
        if (Properties.IsThundering)
        {
            ThunderingStrength = (float)(ThunderingStrength + 0.01D);
        }
        else
        {
            ThunderingStrength = (float)(ThunderingStrength - 0.01D);
        }

        ThunderingStrength = Math.Clamp(ThunderingStrength, 0.0F, 1.0F);
    }

    private void clearWeather()
    {
        Properties.RainTime = 0;
        Properties.IsRaining = false;
        Properties.ThunderTime = 0;
        Properties.IsThundering = false;
    }

    protected virtual void ManageChunkUpdatesAndEvents()
    {
        _activeChunks.Clear();

        for (int i = 0; i < players.Count; ++i)
        {
            EntityPlayer player = players[i];
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

        foreach (var chunkPos in _activeChunks)
        {
            int worldXBase = chunkPos.X * 16;
            int worldZBase = chunkPos.Z * 16;
            Chunk currentChunk = GetChunk(chunkPos.X, chunkPos.Z);

            if (_soundCounter == 0)
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomVal = _lcgBlockSeed >> 2;
                int localX = randomVal & 15;
                int localZ = randomVal >> 8 & 15;
                int localY = randomVal >> 16 & 127;

                int blockId = currentChunk.GetBlockId(localX, localY, localZ);
                int worldX = localX + worldXBase;
                int worldZ = localZ + worldZBase;
                if (blockId == 0 && getBrightness(worldX, localY, worldZ) <= random.NextInt(8) &&
                    getBrightness(LightType.Sky, worldX, localY, worldZ) <= 0)
                {
                    EntityPlayer closest = getClosestPlayer(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D, 8.0D);
                    if (closest != null &&
                        closest.getSquaredDistance(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D) > 4.0D)
                    {
                        playSound(worldX + 0.5D, localY + 0.5D, worldZ + 0.5D, "ambient.cave.cave", 0.7F,
                            0.8F + random.NextFloat() * 0.2F);
                        _soundCounter = random.NextInt(12000) + 6000;
                    }
                }
            }

            if (random.NextInt(100000) == 0 && isRaining() && isThundering())
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomVal = _lcgBlockSeed >> 2;
                int worldX = worldXBase + (randomVal & 15);
                int worldZ = worldZBase + (randomVal >> 8 & 15);
                int worldY = getTopSolidBlockY(worldX, worldZ);

                if (isRaining(worldX, worldY, worldZ))
                {
                    spawnGlobalEntity(new EntityLightningBolt(this, worldX, worldY, worldZ));
                    TicksSinceLightning = 2;
                }
            }

            if (random.NextInt(16) == 0)
            {
                _lcgBlockSeed = _lcgBlockSeed * 3 + 1013904223;
                int randomVal = _lcgBlockSeed >> 2;
                int localX = randomVal & 15;
                int localZ = randomVal >> 8 & 15;
                int worldX = localX + worldXBase;
                int worldZ = localZ + worldZBase;
                int worldY = getTopSolidBlockY(worldX, worldZ);

                if (getBiomeSource().GetBiome(worldX, worldZ).GetEnableSnow() && worldY >= 0 && worldY < 128 &&
                    currentChunk.GetLight(LightType.Block, localX, worldY, localZ) < 10)
                {
                    int blockBelowId = currentChunk.GetBlockId(localX, worldY - 1, localZ);
                    int currentBlockId = currentChunk.GetBlockId(localX, worldY, localZ);

                    if (isRaining() && currentBlockId == 0 && Block.Snow.canPlaceAt(this, worldX, worldY, worldZ) &&
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
                int localZ = randomTickVal >> 8 & 15;
                int localY = randomTickVal >> 16 & 127;

                int blockId = currentChunk.Blocks[localX << 11 | localZ << 7 | localY] & 255;
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
            if (_scheduledUpdates.Count == 0) break;

            if (!forceFlush && _scheduledUpdates.Peek().ScheduledTime > GetEventTime()) break;

            var blockUpdate = _scheduledUpdates.Dequeue();

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

    public List<Entity> getEntities(Entity? excludeEntity, Box area)
    {
        return getEntities(excludeEntity, area, new List<Entity>());
    }

    /// <summary>
    /// Like <see cref="getEntities(Entity?,Box)"/> but writes into a reused thread-static
    /// scratch list. The returned list is only valid until the next call on the same thread.
    /// </summary>
    internal List<Entity> GetEntitiesScratch(Entity? excludeEntity, Box area)
    {
        _tempCollisionEntitiesResult ??= new List<Entity>();
        _tempCollisionEntitiesResult.Clear();
        return getEntities(excludeEntity, area, _tempCollisionEntitiesResult);
    }

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



    public List<Entity> getEntities()
    {
        return entities;
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

    public int CountEntitiesOfType(Type type)
    {
        int res = 0;

        foreach (var entity in entities)
        {
            if (type.IsInstanceOfType(entity)) res++;
        }

        return res;
    }

    public void addEntities(List<Entity> entities)
    {
        this.entities.AddRange(entities);
        for (int i = 0; i < entities.Count; ++i)
        {
            _entitiesById[entities[i].id] = entities[i];
            NotifyEntityAdded(entities[i]);
        }
    }

    public void unloadEntities(List<Entity> entities)
    {
        _entitiesToUnload.AddRange(entities);
    }

    public void tickChunks()
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

        if (collisionBox != null && !canSpawnEntity(collisionBox.Value))
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
        if (isStrongPoweringSide(x, y - 1, z, 0)) return true; // Down
        if (isStrongPoweringSide(x, y + 1, z, 1)) return true; // Up
        if (isStrongPoweringSide(x, y, z - 1, 2)) return true; // North
        if (isStrongPoweringSide(x, y, z + 1, 3)) return true; // South
        if (isStrongPoweringSide(x - 1, y, z, 4)) return true; // West
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
        if (isPoweringSide(x, y - 1, z, 0)) return true; // Down
        if (isPoweringSide(x, y + 1, z, 1)) return true; // Up
        if (isPoweringSide(x, y, z - 1, 2)) return true; // North
        if (isPoweringSide(x, y, z + 1, 3)) return true; // South
        if (isPoweringSide(x - 1, y, z, 4)) return true; // West
        return isPoweringSide(x + 1, y, z, 5); // East
    }

    public EntityPlayer getClosestPlayer(Entity entity, double range)
    {
        return getClosestPlayer(entity.x, entity.y, entity.z, range);
    }

    public EntityPlayer getClosestPlayer(double x, double y, double z, double range)
    {
        double minDistanceSquared = -1.0D;
        EntityPlayer closestPlayer = null;

        for (int i = 0; i < players.Count; ++i)
        {
            EntityPlayer player = players[i];
            double distanceSquared = player.getSquaredDistance(x, y, z);

            bool withinRange = range < 0.0D || distanceSquared < range * range;
            bool isClosestSoFar = minDistanceSquared == -1.0D || distanceSquared < minDistanceSquared;

            if (withinRange && isClosestSoFar)
            {
                minDistanceSquared = distanceSquared;
                closestPlayer = player;
            }
        }

        return closestPlayer;
    }

    public EntityPlayer getPlayer(string name)
    {
        for (int i = 0; i < players.Count; ++i)
        {
            if (name.Equals(players[i].name))
            {
                return players[i];
            }
        }

        return null;
    }

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
            int localStartX = Math.Max(0, x - (chunkX * 16));
            int localEndX = Math.Min(16, x + sizeX - (chunkX * 16));

            for (int chunkZ = startChunkZ; chunkZ <= endChunkZ; ++chunkZ)
            {
                int localStartZ = Math.Max(0, z - (chunkZ * 16));
                int localEndZ = Math.Min(16, z + sizeZ - (chunkZ * 16));

                currentBufferOffset = GetChunk(chunkX, chunkZ).LoadFromPacket(
                    chunkData,
                    localStartX, minY, localStartZ,
                    localEndX, maxY, localEndZ,
                    currentBufferOffset);

                setBlocksDirty(
                    (chunkX * 16) + localStartX, minY, (chunkZ * 16) + localStartZ,
                    (chunkX * 16) + localEndX, maxY, (chunkZ * 16) + localEndZ);
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
            int localStartX = Math.Max(0, x - (chunkX * 16));
            int localEndX = Math.Min(16, x + sizeX - (chunkX * 16));

            for (int chunkZ = startChunkZ; chunkZ <= endChunkZ; chunkZ++)
            {
                int localStartZ = Math.Max(0, z - (chunkZ * 16));
                int localEndZ = Math.Min(16, z + sizeZ - (chunkZ * 16));

                currentBufferOffset = GetChunk(chunkX, chunkZ).ToPacket(
                    chunkData,
                    localStartX, minY, localStartZ,
                    localEndX, maxY, localEndZ,
                    currentBufferOffset);
            }
        }

        return chunkData;
    }

    public void checkSessionLock()
    {
        Storage.CheckSessionLock();
    }

    public void setTime(long time)
    {
        Properties.WorldTime = time;
    }

    public void synchronizeTimeAndUpdates(long time)
    {
        long deltaTime = time - Properties.WorldTime;
        _eventDeltaTime -= deltaTime;
        setTime(time);
    }

    public long getSeed()
    {
        return Properties.RandomSeed;
    }

    public long getTime()
    {
        return Properties.WorldTime;
    }

    private long GetEventTime()
    {
        return Properties.WorldTime + _eventDeltaTime;
    }

    public Vec3i getSpawnPos()
    {
        return new Vec3i(Properties.SpawnX, Properties.SpawnY, Properties.SpawnZ);
    }

    public void setSpawnPos(Vec3i pos)
    {
        Properties.SetSpawn(pos.X, pos.Y, pos.Z);
    }

    public void LoadChunksNearEntity(Entity entity)
    {
        int chunkX = MathHelper.Floor(entity.x / 16.0D);
        int chunkZ = MathHelper.Floor(entity.z / 16.0D);

        // 5x5 area
        const byte loadRadius = 2;

        for (int x = chunkX - loadRadius; x <= chunkX + loadRadius; ++x)
        {
            for (int z = chunkZ - loadRadius; z <= chunkZ + loadRadius; ++z)
            {
                GetChunk(x, z);
            }
        }

        if (!entities.Contains(entity))
        {
            entities.Add(entity);
        }
    }

    public virtual bool canInteract(EntityPlayer player, int x, int y, int z)
    {
        return true;
    }

    public virtual void broadcastEntityEvent(Entity entity, byte @event)
    {
    }

    public void updateEntityLists()
    {
        foreach (var entity in _entitiesToUnload)
        {
            entities.Remove(entity);
        }

        for (int i = 0; i < _entitiesToUnload.Count; ++i)
        {
            var entity = _entitiesToUnload[i];
            var chunkX = entity.chunkX;
            var chunkZ = entity.chunkZ;
            if (entity.isPersistent && hasChunk(chunkX, chunkZ))
            {
                GetChunk(chunkX, chunkZ).RemoveEntity(entity);
            }
            _entitiesById.Remove(entity.id);
            NotifyEntityRemoved(entity);
        }
        _entitiesToUnload.Clear();

        for (int i = 0; i < entities.Count; ++i)
        {
            var entity = entities[i];
            if (entity.vehicle != null)
            {
                if (!entity.vehicle.dead && entity.vehicle.passenger == entity)
                {
                    continue;
                }
                entity.vehicle.passenger = null;
                entity.vehicle = null;
            }

            if (entity.dead)
            {
                var chunkX = entity.chunkX;
                var chunkZ = entity.chunkZ;
                if (entity.isPersistent && hasChunk(chunkX, chunkZ))
                {
                    GetChunk(chunkX, chunkZ).RemoveEntity(entity);
                }

                entities.RemoveAt(i--);
                _entitiesById.Remove(entity.id);
                NotifyEntityRemoved(entity);
            }
        }
    }

    public ChunkSource GetChunkSource()
    {
        return _chunkSource;
    }

    public virtual void playNoteBlockActionAt(int x, int y, int z, int soundType, int pitch)
    {
        int blockId = getBlockId(x, y, z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].onBlockAction(this, x, y, z, soundType, pitch);
        }
    }

    public WorldProperties getProperties()
    {
        return Properties;
    }

    public void updateSleepingPlayers()
    {
        _allPlayersSleeping = players.Count > 0;
        foreach (var player in players)
        {
            if (!player.isSleeping())
            {
                _allPlayersSleeping = false;
                break;
            }
        }
    }

    private void afterSkipNight()
    {
        _allPlayersSleeping = false;
        foreach (var player in players)
        {
            if (player.isSleeping())
            {
                player.wakeUp(false, false, true);
            }
        }

        clearWeather();
    }

    public bool canSkipNight()
    {
        if (!_allPlayersSleeping || isRemote)
        {
            return false;
        }

        return players.All(player => player.isPlayerFullyAsleep());
    }

    public float getThunderGradient(float delta)
    {
        return (PrevThunderingStrength + (ThunderingStrength - PrevThunderingStrength) * delta) *
               getRainGradient(delta);
    }

    public float getRainGradient(float delta)
    {
        return PrevRainingStrength + (RainingStrength - PrevRainingStrength) * delta;
    }

    public void setRainGradient(float rainGradient)
    {
        PrevRainingStrength = rainGradient;
        RainingStrength = rainGradient;
    }

    public bool isThundering()
    {
        return getThunderGradient(1.0F) > 0.9D;
    }

    public bool isRaining()
    {
        return getRainGradient(1.0F) > 0.2D;
    }

    public bool isRaining(int x, int y, int z)
    {
        if (!isRaining())
        {
            return false;
        }
        else if (!hasSkyLight(x, y, z))
        {
            return false;
        }
        else if (getTopSolidBlockY(x, z) > y)
        {
            return false;
        }
        else
        {
            Biome biome = getBiomeSource().GetBiome(x, z);
            return biome.GetEnableSnow() ? false : biome.CanSpawnLightningBolt();
        }
    }

    public void setState(string id, PersistentState state)
    {
        persistentStateManager.SetData(id, state);
    }

    public PersistentState? getOrCreateState(Type type, string id)
    {
        return persistentStateManager.LoadData(type, id);
    }

    public int getIdCount(string id)
    {
        return persistentStateManager.GetUniqueDataId(id);
    }

    public void worldEvent(int @event, int x, int y, int z, int data)
    {
        worldEvent(null, @event, x, y, z, data);
    }

    public void worldEvent(EntityPlayer player, int @event, int x, int y, int z, int data)
    {
        for (int index = 0; index < EventListeners.Count; ++index)
        {
            EventListeners[index].worldEvent(player, @event, x, y, z, data);
        }
    }

    public Entity? getEntityByID(int id)
    {
        return _entitiesById.TryGetValue(id, out var entity) ? entity : null;
    }
}
