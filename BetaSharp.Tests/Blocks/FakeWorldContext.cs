using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.PathFinding;
using BetaSharp.Rules;
using BetaSharp.Server.Worlds;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;
using BetaSharp.Worlds.Storage.RegionFormat;

namespace BetaSharp.Tests.Blocks;

public sealed class FakeWorldContext : IWorldContext
{
    private readonly World _broadcasterWorld;
    private readonly FakeChunkSource _chunkSource;

    public FakeWorldContext()
    {
        ReaderWriter = new FakeBlockGrid();
        Redstone = new RedstoneEngine(ReaderWriter);
        _chunkSource = new FakeChunkSource(this);
        ChunkHost = new ChunkHost(_chunkSource);
        Entities = new EntityManager(this);
        _broadcasterWorld = new BroadcasterWorldStub();
        Broadcaster = new TestWorldEventBroadcaster(this, _broadcasterWorld);
        TickSchedulerSpy = new RecordingTickScheduler(this);
        Rules = new RuleSet(RuleRegistry.Instance);
    }

    public FakeBlockGrid ReaderWriter { get; }
    public RecordingTickScheduler TickSchedulerSpy { get; }

    public IBlockReader Reader => ReaderWriter;
    public IBlockWriter Writer => ReaderWriter;
    public ChunkHost ChunkHost { get; }
    public WorldEventBroadcaster Broadcaster { get; }
    public RedstoneEngine Redstone { get; }
    public EntityManager Entities { get; }
    public LightingEngine Lighting => throw new NotSupportedException();
    public EnvironmentManager Environment => throw new NotSupportedException();
    public Dimension Dimension => throw new NotSupportedException();
    public WorldTickScheduler TickScheduler => TickSchedulerSpy;
    public long Seed => 0;
    public bool IsRemote { get; set; }
    public RuleSet Rules { get; }
    public PersistentStateManager StateManager => throw new NotSupportedException();
    public int Difficulty => 1;

    /// <summary>Minimal spawn for code paths that need <see cref="EntityPlayer"/> (e.g. dispenser <c>onUse</c> tests).</summary>
    public WorldProperties Properties { get; } = new WorldProperties(0L, "test")
    {
        SpawnX = 0,
        SpawnY = 64,
        SpawnZ = 0
    };
    public JavaRandom Random { get; } = new(1234L);
    PathFinder IWorldContext.Pathing => throw new NotSupportedException();

    /// <summary>Returned by <see cref="GetTime"/> for tests that need advancing world time (e.g. torch burnout history pruning).</summary>
    public long SimulatedWorldTime { get; set; }

    public void SetDifficulty(int difficulty) => throw new NotSupportedException();
    public long GetTime() => SimulatedWorldTime;
    public int GetSpawnBlockId(int x, int z) => 0;
    public bool SpawnEntity(Entity entity) => true;
    public bool SpawnItemDrop(double x, double y, double z, ItemStack itemStack) => true;
    public bool CanInteract(EntityPlayer player, int x, int y, int z) => true;
    public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power, bool fire) => throw new NotSupportedException();
    public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power) => throw new NotSupportedException();
}

public sealed class FakeChunkSource(IWorldContext world) : IChunkSource
{
    private readonly Dictionary<(int X, int Z), Chunk> _chunks = [];

    public bool IsChunkLoaded(int x, int z) => true;

    public Chunk GetChunk(int x, int z)
    {
        if (_chunks.TryGetValue((x, z), out Chunk? chunk))
        {
            return chunk;
        }

        chunk = new Chunk(world, x, z)
        {
            Blocks = new byte[16 * 16 * 128],
            Meta = new ChunkNibbleArray(16 * 16 * 128),
            SkyLight = new ChunkNibbleArray(16 * 16 * 128),
            BlockLight = new ChunkNibbleArray(16 * 16 * 128)
        };
        _chunks[(x, z)] = chunk;
        return chunk;
    }

    public Chunk LoadChunk(int x, int z) => GetChunk(x, z);

    public void DecorateTerrain(IChunkSource source, int x, int z)
    {
    }

    public bool Save(bool saveEntities, LoadingDisplay display) => true;
    public bool Tick() => false;
    public bool CanSave() => false;
    public string GetDebugInfo() => "FakeChunkSource";
}

public sealed class RecordingTickScheduler(IWorldContext context) : WorldTickScheduler(context)
{
    public List<(int X, int Y, int Z, int BlockId, int TickRate)> ScheduledTicks { get; } = [];

    public override void ScheduleBlockUpdate(int x, int y, int z, int blockId, int tickRate, bool instantBlockUpdateEnabled = false) => ScheduledTicks.Add((x, y, z, blockId, tickRate));
}

public sealed class TestWorldEventBroadcaster(IWorldContext ctx, World world) : WorldEventBroadcaster([], ctx.Reader, world)
{
    public override void PlayNote(int x, int y, int z, int soundType, int pitch)
    {
        int blockId = ctx.Reader.GetBlockId(x, y, z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].onBlockAction(new OnBlockActionEvent(ctx, soundType, pitch, x, y, z));
        }
    }
}

sealed file class BroadcasterWorldStub : World
{
    public BroadcasterWorldStub() : base(new DummyWorldStorage(), "test", new WorldSettings(0L, WorldType.Default))
    {
    }

    protected override IChunkSource CreateChunkCache() => new StubChunkSource(this);
}

sealed file class StubChunkSource(IWorldContext world) : IChunkSource
{
    private readonly Dictionary<(int X, int Z), Chunk> _chunks = [];

    public bool IsChunkLoaded(int x, int z) => true;

    public Chunk GetChunk(int x, int z)
    {
        if (_chunks.TryGetValue((x, z), out Chunk? chunk))
        {
            return chunk;
        }

        chunk = new Chunk(world, x, z)
        {
            Blocks = new byte[16 * 16 * 128],
            Meta = new ChunkNibbleArray(16 * 16 * 128),
            SkyLight = new ChunkNibbleArray(16 * 16 * 128),
            BlockLight = new ChunkNibbleArray(16 * 16 * 128)
        };
        _chunks[(x, z)] = chunk;
        return chunk;
    }

    public Chunk LoadChunk(int x, int z) => GetChunk(x, z);

    public void DecorateTerrain(IChunkSource source, int x, int z)
    {
    }

    public bool Save(bool saveEntities, LoadingDisplay display) => true;
    public bool Tick() => false;
    public bool CanSave() => false;
    public string GetDebugInfo() => "StubChunkSource";
}

sealed file class DummyWorldStorage : IWorldStorage
{
    public WorldProperties? LoadProperties() => null;

    public void CheckSessionLock()
    {
    }

    public IChunkStorage? GetChunkStorage(Dimension dimension) => null;

    public void Save(WorldProperties properties, List<EntityPlayer> players)
    {
    }

    public void Save(WorldProperties properties)
    {
    }

    public void ForceSave()
    {
    }

    public IPlayerStorage? GetPlayerStorage() => null;
    public FileInfo? GetWorldPropertiesFile(string name) => null;
}

public sealed class FakeBlockGrid : IBlockReader, IBlockWriter
{
    private readonly Dictionary<(int X, int Y, int Z), (int BlockId, int Meta)> _cells = [];

    public readonly List<(int X, int Y, int Z, int BlockId, int Meta)> SetBlockCalls = [];
    public readonly List<(int X, int Y, int Z, int Meta)> SetMetaCalls = [];

    public int GetBlockId(int x, int y, int z) => _cells.TryGetValue((x, y, z), out (int BlockId, int Meta) state) ? state.BlockId : 0;
    public int GetBlockMeta(int x, int y, int z) => _cells.TryGetValue((x, y, z), out (int BlockId, int Meta) state) ? state.Meta : 0;

    public Material GetMaterial(int x, int y, int z)
    {
        int id = GetBlockId(x, y, z);
        return id == 0 ? Material.Air : Block.Blocks[id].material;
    }

    public bool IsOpaque(int x, int y, int z)
    {
        int id = GetBlockId(x, y, z);
        return id != 0 && Block.BlocksOpaque[id];
    }

    public bool ShouldSuffocate(int x, int y, int z)
    {
        int id = GetBlockId(x, y, z);
        return id != 0 && Block.BlocksOpaque[id];
    }

    public BiomeSource GetBiomeSource() => throw new NotSupportedException();
    public bool IsAir(int x, int y, int z) => GetBlockId(x, y, z) == 0;
    public int GetBrightness(int x, int y, int z) => 0;
    public bool IsTopY(int x, int y, int z) => false;
    public int GetTopY(int x, int z) => 0;
    public int GetTopSolidBlockY(int x, int z) => 0;
    public int GetSpawnPositionValidityY(int x, int z) => 0;

    public void MarkChunkDirty(int x, int z)
    {
    }

    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox) => 0F;
    public HitResult Raycast(Vec3D start, Vec3D end, bool includeFluids = false, bool ignoreNonSolid = false) => throw new NotSupportedException();
    public bool IsPosLoaded(int x, int y, int z) => true;
    public bool IsMaterialInBox(Box area, Func<Material, bool> predicate) => false;
    public bool UpdateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity) => false;

    public event Action<int, int, int, int, int, int, int>? OnBlockChangedWithPrev;
    public event Action<int, int, int, int>? OnBlockChanged;
    public event Action<int, int, int, int>? OnNeighborsShouldUpdate;

    public bool SetBlock(int x, int y, int z, int blockId) => SetBlock(x, y, z, blockId, 0, true);

    public bool SetBlock(int x, int y, int z, int blockId, int meta) => SetBlock(x, y, z, blockId, meta, true);

    public bool SetBlock(int x, int y, int z, int blockId, int meta, bool doUpdate)
    {
        int previousBlockId = GetBlockId(x, y, z);
        int previousMeta = GetBlockMeta(x, y, z);
        _cells[(x, y, z)] = (blockId, meta);
        SetBlockCalls.Add((x, y, z, blockId, meta));
        OnBlockChangedWithPrev?.Invoke(x, y, z, previousBlockId, previousMeta, blockId, meta);
        OnBlockChanged?.Invoke(x, y, z, blockId);
        return true;
    }

    public void SetBlockMeta(int x, int y, int z, int meta)
    {
        int blockId = GetBlockId(x, y, z);
        _cells[(x, y, z)] = (blockId, meta);
        SetMetaCalls.Add((x, y, z, meta));
    }

    public bool SetBlockWithoutCallingOnPlaced(int x, int y, int z, int blockId, int meta) => SetBlock(x, y, z, blockId, meta, true);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta) => SetBlock(x, y, z, blockId, meta, false);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta, bool notifyBlockPlaced) => SetBlock(x, y, z, blockId, meta, false);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId) => SetBlock(x, y, z, blockId, 0, false);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, bool notifyBlockPlaced) => SetBlock(x, y, z, blockId, 0, false);

    public bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta)
    {
        SetBlockMeta(x, y, z, meta);
        return true;
    }

    public bool SetBlockInternal(int x, int y, int z, int id, int meta = 0) => SetBlock(x, y, z, id, meta, false);

    public void SetInitial(int x, int y, int z, int blockId, int meta = 0) => _cells[(x, y, z)] = (blockId, meta);
}
