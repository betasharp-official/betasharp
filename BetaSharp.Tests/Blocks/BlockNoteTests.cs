using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockNoteTests
{
    [Fact]
    public void OnUse_CyclesNoteAndReturnsTrue()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Noteblock.id);
        world.ReaderWriter.SetInitial(0, 65, 0, 0); // air above
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.id); // valid instrument base
        BlockEntityNote noteEntity = new();
        world.Entities.SetBlockEntity(0, 64, 0, noteEntity);

        bool handled = Block.Noteblock.onUse(new OnUseEvent(world, null!, 0, 64, 0));

        Assert.True(handled);
        Assert.Equal(1, noteEntity.note);
    }

    [Fact]
    public void NeighborUpdate_RedstoneRisingEdge_PlaysOnceAndLatchesPowered()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Noteblock.id);
        world.ReaderWriter.SetInitial(0, 65, 0, 0); // air above
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.id);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.LitRedstoneTorch.id, 5); // strong power from below
        BlockEntityNote noteEntity = new();
        world.Entities.SetBlockEntity(0, 64, 0, noteEntity);

        Block.Noteblock.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.LitRedstoneTorch.id));
        bool poweredAfterRisingEdge = noteEntity.powered;

        Block.Noteblock.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.LitRedstoneTorch.id));
        bool poweredAfterSteadySignal = noteEntity.powered;

        Assert.True(poweredAfterRisingEdge);
        Assert.True(poweredAfterSteadySignal);
    }
}
