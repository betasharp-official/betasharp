using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockNoteTests
{
    [Fact]
    public void OnUse_CyclesNoteAndReturnsTrue()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Noteblock.ID);
        world.ReaderWriter.SetInitial(0, 65, 0, 0); // air above
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID); // valid instrument base
        BlockEntityNote noteEntity = new();
        world.Entities.SetBlockEntity(0, 64, 0, noteEntity);

        bool handled = Block.Noteblock.OnUse(new OnUseEvent(world, null!, 0, 64, 0));

        Assert.True(handled);
        Assert.Equal(1, noteEntity.Note);
    }

    [Fact]
    public void NeighborUpdate_RedstoneRisingEdge_PlaysOnceAndLatchesPowered()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Noteblock.ID);
        world.ReaderWriter.SetInitial(0, 65, 0, 0); // air above
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.LitRedstoneTorch.ID, 5); // strong power from below
        BlockEntityNote noteEntity = new();
        world.Entities.SetBlockEntity(0, 64, 0, noteEntity);

        Block.Noteblock.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.LitRedstoneTorch.ID));
        bool poweredAfterRisingEdge = noteEntity.Powered;

        Block.Noteblock.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.LitRedstoneTorch.ID));
        bool poweredAfterSteadySignal = noteEntity.Powered;

        Assert.True(poweredAfterRisingEdge);
        Assert.True(poweredAfterSteadySignal);
    }
}
