using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.PathFinding;
using BetaSharp.Rules;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Tests.Fakes;

public class FakeWorld : IWorldContext, IBlockReader, IBlockWriter
{
    private readonly Dictionary<(int x, int y, int z), int> _blocks = new();
    private readonly Dictionary<(int x, int y, int z), int> _light = new();
    private readonly Dictionary<(int x, int y, int z), int> _metadata = new();
    public int GetBlockId(int x, int y, int z) => _blocks.TryGetValue((x, y, z), out int id) ? id : 0;
    public int GetBlockMeta(int x, int y, int z) => _metadata.TryGetValue((x, y, z), out int meta) ? meta : 0;
    public int GetBrightness(int x, int y, int z) => _light.TryGetValue((x, y, z), out int light) ? light : 15;
    public bool IsAir(int x, int y, int z) => GetBlockId(x, y, z) == 0;
    public Material GetMaterial(int x, int y, int z) => BetaSharp.Blocks.Block.Blocks[GetBlockId(x, y, z)]?.Material ?? BetaSharp.Blocks.Materials.Material.Air;
    public bool IsOpaque(int x, int y, int z) => false;
    public bool ShouldSuffocate(int x, int y, int z) 
    {
        int id = GetBlockId(x, y, z);
        return id != 0 && BetaSharp.Blocks.Block.Blocks[id]?.IsOpaque() == true && BetaSharp.Blocks.Block.Blocks[id]?.IsFullCube() == true;
    }
    public BiomeSource GetBiomeSource() => null!;
    public bool IsTopY(int x, int y, int z) => false;
    public int GetTopY(int x, int z) => 0;
    public int GetTopSolidBlockY(int x, int z) => 0;
    public int GetSpawnPositionValidityY(int x, int z) => 0;

    public void MarkChunkDirty(int x, int z)
    {
        // No-op for tests
    }

    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox) => 1.0f;

    public HitResult Raycast(Vec3D start, Vec3D end, bool includeFluids = false, bool ignoreNonSolid = false)
        => new(HitResultType.MISS);

    public bool IsPosLoaded(int x, int y, int z) => true;
    public bool IsMaterialInBox(Box area, Func<Material, bool> predicate) => false;
    public bool UpdateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity) => false;
#pragma warning disable CS0067
    public event Action<int, int, int, int, int, int, int>? OnBlockChangedWithPrev;
    public event Action<int, int, int, int>? OnBlockChanged;
    public event Action<int, int, int, int>? OnNeighborsShouldUpdate;
#pragma warning restore CS0067

    public bool SetBlock(int x, int y, int z, int id)
    {
        _blocks[(x, y, z)] = id;
        return true;
    }

    public bool SetBlock(int x, int y, int z, int id, int meta)
    {
        _blocks[(x, y, z)] = id;
        _metadata[(x, y, z)] = meta;
        return true;
    }

    public bool SetBlock(int x, int y, int z, int blockId, int meta, bool doUpdate)
    {
        _blocks[(x, y, z)] = blockId;
        _metadata[(x, y, z)] = meta;
        return true;
    }

    public void SetBlockMeta(int x, int y, int z, int meta) => _metadata[(x, y, z)] = meta;

    public bool SetBlockWithoutCallingOnPlaced(int x, int y, int z, int blockId, int meta)
    {
        _blocks[(x, y, z)] = blockId;
        _metadata[(x, y, z)] = meta;
        return true;
    }

    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta) => SetBlock(x, y, z, blockId, meta);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta, bool notifyBlockPlaced) => SetBlock(x, y, z, blockId, meta);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId) => SetBlock(x, y, z, blockId);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, bool notifyBlockPlaced) => SetBlock(x, y, z, blockId);

    public bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta)
    {
        SetBlockMeta(x, y, z, meta);
        return true;
    }

    public bool SetBlockInternal(int x, int y, int z, int id, int meta = 0) => SetBlock(x, y, z, id, meta);
    public void SetBlockMetaInternal(int x, int y, int z, int meta) => SetBlockMeta(x, y, z, meta);
    public IBlockReader Reader => this;
    public IBlockWriter Writer => this;
    public ChunkHost ChunkHost { get; }
    public WorldEventBroadcaster Broadcaster { get; }
    public RedstoneEngine Redstone { get; }
    public EntityManager Entities { get; }
    public LightingEngine Lighting { get; } = null!;
    public EnvironmentManager Environment { get; } = null!;
    public Dimension Dimension { get; } = null!;
    public WorldTickScheduler TickScheduler { get; }
    public PersistentStateManager StateManager { get; } = null!;
    
    public FakeWorld()
    {
        Broadcaster = new WorldEventBroadcaster(new(), this, this);
        TickScheduler = new FakeTickScheduler(this);
        ChunkHost = new ChunkHost(new FakeChunkSource(this));
        Entities = new EntityManager(this);
        Redstone = new RedstoneEngine(this);
    }
    public WorldProperties Properties { get; } = null!;
    private PathFinder? _pathFinder;
    public PathFinder Pathing => _pathFinder ??= new PathFinder(this);
    public RuleSet Rules { get; } = new RuleSet(RuleRegistry.Instance);
    public JavaRandom Random { get; } = new();
    public long Seed => 0;
    public bool IsRemote { get; set; } = false;
    public int Difficulty { get; private set; }
    public void SetDifficulty(int difficulty) => Difficulty = difficulty;
    public long GetTime() => 0;
    public int GetSpawnBlockId(int x, int z) => 0;
    public bool SpawnEntity(Entity entity) => true;
    public bool SpawnItemDrop(double x, double y, double z, ItemStack itemStack) => true;
    public bool CanInteract(EntityPlayer player, int x, int y, int z) => true;
    public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power, bool fire) => null!;
    public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power) => null!;
    public void SetLightLevel(int x, int y, int z, int level) => _light[(x, y, z)] = level;
}
