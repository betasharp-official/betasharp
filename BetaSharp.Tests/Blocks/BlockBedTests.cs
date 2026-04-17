using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.PathFinding;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockBedTests
{
    private sealed class TestPlayer : EntityPlayer
    {
        public TestPlayer(IWorldContext world) : base(world)
        {
        }

        public override EntityType Type => EntityRegistry.Player;

        public override void spawn()
        {
        }
    }

    [Fact]
    public void OnBreak_FootHalf_RemovesHeadHalf()
    {
        var world = new FakeWorldContext();

        world.ReaderWriter.SetInitial(10, 64, 10, Block.Bed.id, 0);
        world.ReaderWriter.SetInitial(10, 64, 11, Block.Bed.id, 8);

        Block.Bed.onBreak(new OnBreakEvent(world, null, 10, 64, 10, 0));

        Assert.Equal(0, world.Reader.GetBlockId(10, 64, 11));
    }

    [Fact]
    public void OnBreak_HeadHalf_RemovesFootHalf()
    {
        var world = new FakeWorldContext();

        world.ReaderWriter.SetInitial(10, 64, 10, Block.Bed.id, 0);
        world.ReaderWriter.SetInitial(10, 64, 11, Block.Bed.id, 8);

        Block.Bed.onBreak(new OnBreakEvent(world, null, 10, 64, 11, 8));

        Assert.Equal(0, world.Reader.GetBlockId(10, 64, 10));
    }

    [Fact]
    public void OnAfterBreak_HeadHalf_DropsBedItem()
    {
        var world = new RecordingDropWorldContext();
        var player = new TestPlayer(world);

        Block.Bed.onAfterBreak(new OnAfterBreakEvent(world, player, 8, 10, 64, 11));

        ItemStack drop = Assert.Single(world.DroppedItems);
        Assert.Equal(Item.Bed.id, drop.ItemId);
        Assert.Equal(1, drop.Count);
    }

    private sealed class RecordingDropWorldContext : IWorldContext
    {
        private readonly FakeWorldContext _inner = new();

        public List<ItemStack> DroppedItems { get; } = [];

        public IBlockReader Reader => _inner.Reader;
        public IBlockWriter Writer => _inner.Writer;
        public ChunkHost ChunkHost => _inner.ChunkHost;
        public WorldEventBroadcaster Broadcaster => _inner.Broadcaster;
        public RedstoneEngine Redstone => _inner.Redstone;
        public EntityManager Entities => _inner.Entities;
        public LightingEngine Lighting => _inner.Lighting;
        public EnvironmentManager Environment => _inner.Environment;
        public Dimension Dimension => _inner.Dimension;
        public WorldTickScheduler TickScheduler => _inner.TickScheduler;
        public long Seed => _inner.Seed;
        public bool IsRemote => _inner.IsRemote;
        public RuleSet Rules => _inner.Rules;
        public PersistentStateManager StateManager => _inner.StateManager;
        public int Difficulty => _inner.Difficulty;
        public WorldProperties Properties => _inner.Properties;
        public JavaRandom Random => _inner.Random;
        PathFinder IWorldContext.Pathing => throw new NotSupportedException();

        public void SetDifficulty(int difficulty) => _inner.SetDifficulty(difficulty);
        public long GetTime() => _inner.GetTime();
        public int GetSpawnBlockId(int x, int z) => _inner.GetSpawnBlockId(x, z);
        public bool SpawnEntity(Entity entity) => _inner.SpawnEntity(entity);

        public bool SpawnItemDrop(double x, double y, double z, ItemStack itemStack)
        {
            DroppedItems.Add(itemStack);
            return true;
        }

        public bool CanInteract(EntityPlayer player, int x, int y, int z) => _inner.CanInteract(player, x, y, z);
        public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power, bool fire) => _inner.CreateExplosion(source, x, y, z, power, fire);
        public Explosion CreateExplosion(Entity? source, double x, double y, double z, float power) => _inner.CreateExplosion(source, x, y, z, power);
    }
}
