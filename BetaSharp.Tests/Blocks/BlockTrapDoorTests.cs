using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockTrapDoorTests
{
    private static OnTickEvent Tick(FakeWorldContext world, int x = 0, int y = 64, int z = 0) => new(world, x, y, z, world.Reader.GetBlockMeta(x, y, z), world.Reader.GetBlockId(x, y, z));

    [Fact]
    public void NeighborUpdate_WhenAttachedBlockMissing_BreaksTrapDoor()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Trapdoor.ID); // hinge expects block at z+1

        Block.Trapdoor.NeighborUpdate(Tick(world));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }
}
