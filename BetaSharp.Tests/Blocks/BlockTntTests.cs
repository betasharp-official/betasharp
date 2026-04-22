using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockTntTests
{
    [Fact]
    public void NeighborUpdate_WhenPoweredByEmitter_PrimesAndClearsBlock()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.TNT.id);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.LitRedstoneTorch.id); // powers TNT

        Block.TNT.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.LitRedstoneTorch.id));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void NeighborUpdate_NonEmitterTrigger_DoesNotPrimeTnt()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.TNT.id);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.id);

        Block.TNT.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.Stone.id));

        Assert.Equal(Block.TNT.id, world.Reader.GetBlockId(0, 64, 0));
    }
}
