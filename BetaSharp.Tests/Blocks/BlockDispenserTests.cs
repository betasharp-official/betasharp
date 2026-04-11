using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockDispenserTests
{
    private sealed class CapturingDispenserPlayer : EntityPlayer
    {
        public BlockEntityDispenser? LastOpened;

        public CapturingDispenserPlayer(IWorldContext world) : base(world)
        {
        }

        public override EntityType Type => EntityRegistry.Player;

        public override void spawn()
        {
        }

        public override void openDispenserScreen(BlockEntityDispenser dispenser) => LastOpened = dispenser;
    }

    [Fact]
    public void NeighborUpdate_PoweredByEmitter_SchedulesDispenseTick()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Dispenser.id, 3);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.LitRedstoneTorch.id); // powers dispenser position

        Block.Dispenser.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 3, Block.LitRedstoneTorch.id));

        Assert.Contains(world.TickSchedulerSpy.ScheduledTicks, t =>
            t is { X: 0, Y: 64, Z: 0 } && t.BlockId == Block.Dispenser.id && t.TickRate == 4);
    }

    [Fact]
    public void NeighborUpdate_NonEmitterTrigger_DoesNotScheduleTick()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Dispenser.id, 3);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.id);

        Block.Dispenser.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 3, Block.Stone.id));

        Assert.Empty(world.TickSchedulerSpy.ScheduledTicks);
    }

    [Fact]
    public void NeighborUpdate_BlockIdZero_DoesNotScheduleTick()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(20, 64, 20, Block.Dispenser.id, 3);
        world.ReaderWriter.SetInitial(20, 63, 20, Block.LitRedstoneTorch.id);

        Block.Dispenser.neighborUpdate(new OnTickEvent(world, 20, 64, 20, 3, 0));

        Assert.Empty(world.TickSchedulerSpy.ScheduledTicks);
    }

    [Fact]
    public void NeighborUpdate_EmitterButDispenserUnpowered_DoesNotScheduleTick()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(21, 64, 21, Block.Dispenser.id, 3);

        Block.Dispenser.neighborUpdate(new OnTickEvent(world, 21, 64, 21, 0, Block.LitRedstoneTorch.id));

        Assert.Empty(world.TickSchedulerSpy.ScheduledTicks);
    }

    [Fact]
    public void NeighborUpdate_PoweredFromBlockAbove_SchedulesTick()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(22, 64, 22, Block.Dispenser.id, 3);
        world.ReaderWriter.SetInitial(22, 65, 22, Block.LitRedstoneTorch.id);

        Block.Dispenser.neighborUpdate(new OnTickEvent(world, 22, 64, 22, 5, Block.LitRedstoneTorch.id));

        Assert.Contains(world.TickSchedulerSpy.ScheduledTicks, t =>
            t is { X: 22, Y: 64, Z: 22 } && t.BlockId == Block.Dispenser.id);
    }

    private static void AttachDispenser(FakeWorldContext world, int x, int y, int z)
    {
        BlockEntityDispenser dispenser = new()
        {
            World = world,
            X = x,
            Y = y,
            Z = z
        };
        world.Entities.SetBlockEntity(x, y, z, dispenser);
    }

    private static OnTickEvent DispenserTick(FakeWorldContext world, int x, int y, int z) =>
        new(world, x, y, z, world.Reader.GetBlockMeta(x, y, z), Block.Dispenser.id);

    [Fact]
    public void OnTick_PoweredEmpty_EmitsClickWorldEventAndSpawnsNothing()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(2, 63, 2, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(2, 64, 2, Block.Dispenser.id, 3);
        AttachDispenser(world, 2, 64, 2);

        int entitiesBefore = world.Entities.Entities.Count;
        Block.Dispenser.onTick(DispenserTick(world, 2, 64, 2));

        Assert.Equal(entitiesBefore, world.Entities.Entities.Count);
    }

    [Fact]
    public void OnTick_Unpowered_DoesNotDispense()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(23, 64, 23, Block.Dispenser.id, 3);
        AttachDispenser(world, 23, 64, 23);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(23, 64, 23)!.setStack(0, new ItemStack(Item.ARROW.id, 1));

        int before = world.Entities.Entities.Count;
        Block.Dispenser.onTick(DispenserTick(world, 23, 64, 23));

        Assert.Equal(before, world.Entities.Entities.Count);
    }

    [Fact]
    public void OnTick_PoweredNoBlockEntity_DoesNotThrow()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(24, 63, 24, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(24, 64, 24, Block.Dispenser.id, 3);

        Block.Dispenser.onTick(DispenserTick(world, 24, 64, 24));
    }

    [Fact]
    public void OnTick_PoweredWithArrow_SpawnsArrowEntity()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(3, 63, 3, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(3, 64, 3, Block.Dispenser.id, 3);
        AttachDispenser(world, 3, 64, 3);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(3, 64, 3)!.setStack(0, new ItemStack(Item.ARROW.id, 1));

        Block.Dispenser.onTick(DispenserTick(world, 3, 64, 3));

        Assert.Contains(world.Entities.Entities, e => e is EntityArrow);
    }

    [Fact]
    public void OnTick_Meta2North_FiresAlongNegativeZ()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(30, 63, 30, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(30, 64, 30, Block.Dispenser.id, 2);
        AttachDispenser(world, 30, 64, 30);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(30, 64, 30)!.setStack(0, new ItemStack(Item.ARROW.id, 1));

        Block.Dispenser.onTick(DispenserTick(world, 30, 64, 30));

        Assert.Contains(world.Entities.Entities, e => e is EntityArrow);
    }

    [Fact]
    public void OnTick_Meta5East_FiresAlongPositiveX()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(31, 63, 31, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(31, 64, 31, Block.Dispenser.id, 5);
        AttachDispenser(world, 31, 64, 31);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(31, 64, 31)!.setStack(0, new ItemStack(Item.ARROW.id, 1));

        Block.Dispenser.onTick(DispenserTick(world, 31, 64, 31));

        Assert.Contains(world.Entities.Entities, e => e is EntityArrow);
    }

    [Fact]
    public void OnTick_Meta4West_FiresAlongNegativeX()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(32, 63, 32, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(32, 64, 32, Block.Dispenser.id, 4);
        AttachDispenser(world, 32, 64, 32);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(32, 64, 32)!.setStack(0, new ItemStack(Item.ARROW.id, 1));

        Block.Dispenser.onTick(DispenserTick(world, 32, 64, 32));

        Assert.Contains(world.Entities.Entities, e => e is EntityArrow);
    }

    [Fact]
    public void OnTick_PoweredWithEgg_SpawnsEggEntity()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(4, 63, 4, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(4, 64, 4, Block.Dispenser.id, 3);
        AttachDispenser(world, 4, 64, 4);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(4, 64, 4)!.setStack(0, new ItemStack(Item.Egg.id, 1));

        Block.Dispenser.onTick(DispenserTick(world, 4, 64, 4));

        Assert.Contains(world.Entities.Entities, e => e is EntityEgg);
    }

    [Fact]
    public void OnTick_PoweredWithSnowball_SpawnsSnowballEntity()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(5, 63, 5, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(5, 64, 5, Block.Dispenser.id, 3);
        AttachDispenser(world, 5, 64, 5);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(5, 64, 5)!.setStack(0, new ItemStack(Item.Snowball.id, 1));

        Block.Dispenser.onTick(DispenserTick(world, 5, 64, 5));

        Assert.Contains(world.Entities.Entities, e => e is EntitySnowball);
    }

    [Fact]
    public void OnTick_PoweredWithGenericItem_SpawnsItemEntity()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(6, 63, 6, Block.LitRedstoneTorch.id);
        world.ReaderWriter.SetInitial(6, 64, 6, Block.Dispenser.id, 3);
        AttachDispenser(world, 6, 64, 6);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(6, 64, 6)!.setStack(0, new ItemStack(Item.Stick.id, 1));

        Block.Dispenser.onTick(DispenserTick(world, 6, 64, 6));

        Assert.Contains(world.Entities.Entities, e => e is EntityItem ei && ei.stack.itemId == Item.Stick.id);
    }

    [Fact]
    public void GetDroppedItemId_ReturnsDispenserBlock()
    {
        Assert.Equal(Block.Dispenser.id, Block.Dispenser.getDroppedItemId(0));
    }

    [Fact]
    public void GetTickRate_IsFour()
    {
        Assert.Equal(4, Block.Dispenser.getTickRate());
    }

    [Fact]
    public void GetTexture_TopBottom_AndSouthFace_Vary()
    {
        int top = Block.Dispenser.GetTexture(Side.Up);
        int south = Block.Dispenser.GetTexture(Side.South);
        int north = Block.Dispenser.GetTexture(Side.North);
        Assert.Equal(Block.Dispenser.TextureId + 17, top);
        Assert.Equal(Block.Dispenser.TextureId + 1, south);
        Assert.Equal(Block.Dispenser.TextureId, north);
    }

    [Fact]
    public void GetTextureId_TopAndFacingMeta_MatchBlockTextures()
    {
        FakeWorldContext world = new();
        int x = 40, y = 64, z = 40;
        world.ReaderWriter.SetInitial(x, y, z, Block.Dispenser.id, 3);
        int meta = world.Reader.GetBlockMeta(x, y, z);
        Side facing = meta.ToSide();

        Assert.Equal(Block.Dispenser.TextureId + 17, Block.Dispenser.GetTextureId(world.Reader, x, y, z, Side.Up));
        Assert.Equal(Block.Dispenser.TextureId + 17, Block.Dispenser.GetTextureId(world.Reader, x, y, z, Side.Down));
        Assert.Equal(Block.Dispenser.TextureId + 1, Block.Dispenser.GetTextureId(world.Reader, x, y, z, facing));
        Assert.Equal(Block.Dispenser.TextureId, Block.Dispenser.GetTextureId(world.Reader, x, y, z, facing.OppositeFace()));
    }

    [Fact]
    public void OnPlaced_NullPlacer_OpensSouthWhenNorthFaceIsOpaque()
    {
        FakeWorldContext world = new();
        int x = 50, y = 64, z = 50;
        world.ReaderWriter.SetInitial(x, y, z - 1, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z + 1, 0);
        world.ReaderWriter.SetInitial(x - 1, y, z, 0);
        world.ReaderWriter.SetInitial(x + 1, y, z, 0);
        world.ReaderWriter.SetInitial(x, y, z, Block.Dispenser.id, 0);

        Block.Dispenser.onPlaced(new OnPlacedEvent(world, null, Side.Up, Side.Up, x, y, z));

        Assert.Equal(3, world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_NullPlacer_OpensNorthWhenSouthFaceIsOpaque()
    {
        FakeWorldContext world = new();
        int x = 51, y = 64, z = 51;
        world.ReaderWriter.SetInitial(x, y, z - 1, 0);
        world.ReaderWriter.SetInitial(x, y, z + 1, Block.Stone.id);
        world.ReaderWriter.SetInitial(x - 1, y, z, 0);
        world.ReaderWriter.SetInitial(x + 1, y, z, 0);
        world.ReaderWriter.SetInitial(x, y, z, Block.Dispenser.id, 0);

        Block.Dispenser.onPlaced(new OnPlacedEvent(world, null, Side.Up, Side.Up, x, y, z));

        Assert.Equal(Side.North.ToInt(), world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_NullPlacer_OpensEastWhenWestFaceIsOpaque()
    {
        FakeWorldContext world = new();
        int x = 52, y = 64, z = 52;
        world.ReaderWriter.SetInitial(x, y, z - 1, 0);
        world.ReaderWriter.SetInitial(x, y, z + 1, 0);
        world.ReaderWriter.SetInitial(x - 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x + 1, y, z, 0);
        world.ReaderWriter.SetInitial(x, y, z, Block.Dispenser.id, 0);

        Block.Dispenser.onPlaced(new OnPlacedEvent(world, null, Side.Up, Side.Up, x, y, z));

        Assert.Equal(Side.East.ToInt(), world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_NullPlacer_OpensWestWhenEastFaceIsOpaque()
    {
        FakeWorldContext world = new();
        int x = 53, y = 64, z = 53;
        world.ReaderWriter.SetInitial(x, y, z - 1, 0);
        world.ReaderWriter.SetInitial(x, y, z + 1, 0);
        world.ReaderWriter.SetInitial(x - 1, y, z, 0);
        world.ReaderWriter.SetInitial(x + 1, y, z, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Dispenser.id, 0);

        Block.Dispenser.onPlaced(new OnPlacedEvent(world, null, Side.Up, Side.Up, x, y, z));

        Assert.Equal(Side.West.ToInt(), world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_NullPlacer_IsRemote_DoesNotChangeMetaFromUpdateDirection()
    {
        FakeWorldContext world = new();
        world.IsRemote = true;
        int x = 54, y = 64, z = 54;
        world.ReaderWriter.SetInitial(x, y, z - 1, Block.Stone.id);
        world.ReaderWriter.SetInitial(x, y, z, Block.Dispenser.id, 7);

        Block.Dispenser.onPlaced(new OnPlacedEvent(world, null, Side.Up, Side.Up, x, y, z));

        Assert.Equal(7, world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnPlaced_WithPlacer_YawMapsToFacingMeta()
    {
        FakeWorldContext world = new();
        int x = 60, y = 64, z = 60;
        world.ReaderWriter.SetInitial(x, y, z, Block.Dispenser.id, 0);

        CapturingDispenserPlayer placer = new(world) { yaw = 90f };
        Block.Dispenser.onPlaced(new OnPlacedEvent(world, placer, Side.Up, Side.Up, x, y, z));

        Assert.Equal(Side.East.ToInt(), world.Reader.GetBlockMeta(x, y, z));
    }

    [Fact]
    public void OnUse_IsRemote_ReturnsTrueWithoutOpening()
    {
        FakeWorldContext world = new();
        world.IsRemote = true;
        world.ReaderWriter.SetInitial(70, 64, 70, Block.Dispenser.id, 3);
        AttachDispenser(world, 70, 64, 70);
        CapturingDispenserPlayer player = new(world);

        bool result = Block.Dispenser.onUse(new OnUseEvent(world, player, 70, 64, 70));

        Assert.True(result);
        Assert.Null(player.LastOpened);
    }

    [Fact]
    public void OnUse_Server_WithBlockEntity_OpensScreen()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(71, 64, 71, Block.Dispenser.id, 3);
        AttachDispenser(world, 71, 64, 71);
        BlockEntityDispenser be = world.Entities.GetBlockEntity<BlockEntityDispenser>(71, 64, 71)!;
        CapturingDispenserPlayer player = new(world);

        bool result = Block.Dispenser.onUse(new OnUseEvent(world, player, 71, 64, 71));

        Assert.True(result);
        Assert.Same(be, player.LastOpened);
    }

    [Fact]
    public void OnUse_Server_ChunkGetBlockEntityAutoCreates_OpensScreen()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(72, 64, 72, Block.Dispenser.id, 3);
        CapturingDispenserPlayer player = new(world);

        bool result = Block.Dispenser.onUse(new OnUseEvent(world, player, 72, 64, 72));

        Assert.True(result);
        BlockEntityDispenser? be = world.Entities.GetBlockEntity<BlockEntityDispenser>(72, 64, 72);
        Assert.NotNull(be);
        Assert.Same(be, player.LastOpened);
    }

    [Fact]
    public void OnBreak_WithStacks_SpawnsItemEntities()
    {
        FakeWorldContext world = new();
        int x = 80, y = 64, z = 80;
        world.ReaderWriter.SetInitial(x, y, z, Block.Dispenser.id, 3);
        AttachDispenser(world, x, y, z);
        world.Entities.GetBlockEntity<BlockEntityDispenser>(x, y, z)!.setStack(0, new ItemStack(Item.IronIngot.id, 24));

        Block.Dispenser.onBreak(new OnBreakEvent(world, null, x, y, z));

        Assert.Contains(world.Entities.Entities, e => e is EntityItem);
    }
}
