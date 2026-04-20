using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockPistonTests
{
    [Fact]
    public void NeighborUpdate_PushLimit12_ExtendsAndPushes()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 5); // facing +X
        for (int x = 1; x <= 12; x++)
        {
            world.ReaderWriter.SetInitial(x, 64, 0, Block.Stone.ID);
        }

        world.ReaderWriter.SetInitial(0, 66, 0, Block.LitRedstoneTorch.ID); // quasi power

        Block.Piston.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 5, Block.LitRedstoneTorch.ID));

        Assert.Equal(13, world.Reader.GetBlockMeta(0, 64, 0)); // extended
        Assert.Equal(Block.MovingPiston.ID, world.Reader.GetBlockId(1, 64, 0)); // head extension mover
        Assert.Equal(Block.MovingPiston.ID, world.Reader.GetBlockId(13, 64, 0)); // farthest pushed mover
    }

    [Fact]
    public void NeighborUpdate_QuasiConnectivity_TwoBlocksAbovePower_ExtendsPiston()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 5); // facing +X
        world.ReaderWriter.SetInitial(0, 66, 0, Block.LitRedstoneTorch.ID); // quasi power source at y+2

        Block.Piston.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 5, Block.LitRedstoneTorch.ID));

        Assert.Equal(13, world.Reader.GetBlockMeta(0, 64, 0)); // facing + extended
        Assert.Equal(Block.MovingPiston.ID, world.Reader.GetBlockId(1, 64, 0));
        Assert.NotNull(world.Entities.GetBlockEntity<BlockEntityPiston>(1, 64, 0));
    }

    [Fact]
    public void OnBlockAction_Retract_NonStickyConvertsBaseToMovingAndClearsHead()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 13); // facing +X, extended
        world.ReaderWriter.SetInitial(1, 64, 0, Block.PistonHead.ID, 5);

        Block.Piston.OnBlockAction(new OnBlockActionEvent(world, 1, 5, 0, 64, 0));

        Assert.Equal(Block.MovingPiston.ID, world.Reader.GetBlockId(0, 64, 0));
        Assert.NotNull(world.Entities.GetBlockEntity<BlockEntityPiston>(0, 64, 0));
        Assert.Equal(0, world.Reader.GetBlockId(1, 64, 0));
    }

    [Fact]
    public void PistonExtension_OnBreak_RemovesExtendedBasePistonBehind()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 13); // extended base
        world.ReaderWriter.SetInitial(1, 64, 0, Block.PistonHead.ID, 5); // extension facing +X

        Block.PistonHead.OnBreak(new OnBreakEvent(world, null, 1, 64, 0));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void PistonMoving_OnUse_WithoutBlockEntity_RemovesMovingBlock()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.MovingPiston.ID);

        bool handled = Block.MovingPiston.OnUse(new OnUseEvent(world, null!, 0, 64, 0));

        Assert.True(handled);
        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void StickyRetract_PullsRegularBlockIntoMovingState()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.StickyPiston.ID, 13); // facing +X, extended
        world.ReaderWriter.SetInitial(1, 64, 0, Block.PistonHead.ID, 5);
        world.ReaderWriter.SetInitial(2, 64, 0, Block.Stone.ID);

        Block.StickyPiston.OnBlockAction(new OnBlockActionEvent(world, 1, 5, 0, 64, 0));

        Assert.Equal(Block.MovingPiston.ID, world.Reader.GetBlockId(0, 64, 0)); // retracting base
        Assert.Equal(0, world.Reader.GetBlockId(2, 64, 0)); // source cleared
        Assert.Equal(Block.MovingPiston.ID, world.Reader.GetBlockId(1, 64, 0)); // pulled block now moving at head

        BlockEntityPiston? pulled = world.Entities.GetBlockEntity<BlockEntityPiston>(1, 64, 0);
        Assert.NotNull(pulled);
        Assert.Equal(Block.Stone.ID, pulled.PushedBlockId);
        Assert.False(pulled.IsExtending);
    }

    [Fact]
    public void StickyRetract_ShortPulseAbandonsExtension_LeavesBlockBehind()
    {
        FakeWorldContext world = new();
        world.IsRemote = true; // Avoid broadcaster world-context path during extension abandon finalization.
        world.ReaderWriter.SetInitial(0, 64, 0, Block.StickyPiston.ID, 13); // facing +X, extended
        world.ReaderWriter.SetInitial(1, 64, 0, Block.PistonHead.ID, 5);
        world.ReaderWriter.SetInitial(2, 64, 0, Block.MovingPiston.ID);
        world.Entities.SetBlockEntity(2, 64, 0, BlockPistonMoving.CreatePistonBlockEntity(Block.Stone.ID, 0, 5, true, false));

        Block.StickyPiston.OnBlockAction(new OnBlockActionEvent(world, 1, 5, 0, 64, 0));

        Assert.Equal(Block.Stone.ID, world.Reader.GetBlockId(2, 64, 0)); // extension finalized to static block
        Assert.Equal(0, world.Reader.GetBlockId(1, 64, 0)); // sticky spit clears head space
    }

    [Fact]
    public void NeighborUpdate_PushBlockedByObsidian_DoesNotExtend()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 5); // facing +X
        world.ReaderWriter.SetInitial(1, 64, 0, Block.Obsidian.ID); // immovable in front
        world.ReaderWriter.SetInitial(0, 66, 0, Block.LitRedstoneTorch.ID); // quasi power

        Block.Piston.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 5, Block.LitRedstoneTorch.ID));

        Assert.Equal(5, world.Reader.GetBlockMeta(0, 64, 0)); // not extended
        Assert.NotEqual(Block.MovingPiston.ID, world.Reader.GetBlockId(1, 64, 0));
    }

    [Fact]
    public void NeighborUpdate_PushBlockedByBlockEntity_DoesNotExtend()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 5); // facing +X
        world.ReaderWriter.SetInitial(1, 64, 0, Block.Chest.ID);
        world.Entities.SetBlockEntity(1, 64, 0, new BlockEntityChest()); // immovable because block entity present
        world.ReaderWriter.SetInitial(0, 66, 0, Block.LitRedstoneTorch.ID); // quasi power

        Block.Piston.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 5, Block.LitRedstoneTorch.ID));

        Assert.Equal(5, world.Reader.GetBlockMeta(0, 64, 0)); // stays unextended
        Assert.Equal(Block.Chest.ID, world.Reader.GetBlockId(1, 64, 0));
    }

    [Fact]
    public void NeighborUpdate_PushChainBeyondLimit_DoesNotExtend()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 5); // facing +X
        for (int x = 1; x <= 13; x++)
        {
            world.ReaderWriter.SetInitial(x, 64, 0, Block.Stone.ID);
        }

        world.ReaderWriter.SetInitial(0, 66, 0, Block.LitRedstoneTorch.ID); // quasi power

        Block.Piston.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 5, Block.LitRedstoneTorch.ID));

        Assert.Equal(5, world.Reader.GetBlockMeta(0, 64, 0)); // not extended
        Assert.Equal(Block.Stone.ID, world.Reader.GetBlockId(1, 64, 0)); // front block unchanged
    }

    [Fact]
    public void StickyRetract_UnpullableBlockRetractsHeadButLeavesBlock()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.StickyPiston.ID, 13); // facing +X, extended
        world.ReaderWriter.SetInitial(1, 64, 0, Block.PistonHead.ID, 5);
        world.ReaderWriter.SetInitial(2, 64, 0, Block.Door.ID); // piston behavior 1 => not pullable

        Block.StickyPiston.OnBlockAction(new OnBlockActionEvent(world, 1, 5, 0, 64, 0));

        Assert.Equal(Block.MovingPiston.ID, world.Reader.GetBlockId(0, 64, 0)); // base retract animation
        Assert.Equal(0, world.Reader.GetBlockId(1, 64, 0)); // head removed
        Assert.Equal(Block.Door.ID, world.Reader.GetBlockId(2, 64, 0)); // target left in place
    }

    [Fact]
    public void QuasiConnectivity_BudStyle_NoImmediateUpdateUntilNeighborEvent()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 5); // facing +X
        world.ReaderWriter.SetInitial(1, 64, 0, 0);
        world.ReaderWriter.SetInitial(1, 65, 0, Block.LitRedstoneTorch.ID); // diagonal-above style power source

        // No neighborUpdate yet => no state change
        Assert.Equal(5, world.Reader.GetBlockMeta(0, 64, 0));

        // Adjacent block update event arrives (BUD-like trigger)
        Block.Piston.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 5, Block.Stone.ID));

        Assert.Equal(13, world.Reader.GetBlockMeta(0, 64, 0));
    }

    [Fact]
    public void OpposingPistons_RapidNeighborUpdates_DoesNotThrow()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Piston.ID, 5); // faces +X
        world.ReaderWriter.SetInitial(2, 64, 0, Block.Piston.ID, 4); // faces -X
        world.ReaderWriter.SetInitial(0, 66, 0, Block.LitRedstoneTorch.ID);
        world.ReaderWriter.SetInitial(2, 66, 0, Block.LitRedstoneTorch.ID);

        Exception? ex = Record.Exception(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                Block.Piston.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, world.Reader.GetBlockMeta(0, 64, 0), Block.Stone.ID));
                Block.Piston.NeighborUpdate(new OnTickEvent(world, 2, 64, 0, world.Reader.GetBlockMeta(2, 64, 0), Block.Stone.ID));
            }
        });

        Assert.Null(ex);
    }
}
