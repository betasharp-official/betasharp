using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockRailTests
{
    private static OnTickEvent Tick(FakeWorldContext world, int x = 0, int y = 64, int z = 0) => new(world, x, y, z, world.Reader.GetBlockMeta(x, y, z), world.Reader.GetBlockId(x, y, z));

    [Fact]
    public void NeighborUpdate_RailWithoutBlockBelow_BreaksIntoAir()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Rail.ID);

        Block.Rail.NeighborUpdate(Tick(world));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void NeighborUpdate_PoweredRailAscendingEastWithoutSupport_BreaksIntoAir()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID); // support below
        world.ReaderWriter.SetInitial(0, 64, 0, Block.PoweredRail.ID, 2); // ascending east needs support at +X

        Block.PoweredRail.NeighborUpdate(Tick(world));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }
}
