using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockTntTests
{
    [Fact]
    public void NeighborUpdate_WhenPoweredByEmitter_PrimesAndClearsBlock()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.TNT.ID);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.LitRedstoneTorch.ID); // powers TNT

        Block.TNT.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.LitRedstoneTorch.ID));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void NeighborUpdate_NonEmitterTrigger_DoesNotPrimeTnt()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.TNT.ID);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);

        Block.TNT.NeighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.Stone.ID));

        Assert.Equal(Block.TNT.ID, world.Reader.GetBlockId(0, 64, 0));
    }
}
