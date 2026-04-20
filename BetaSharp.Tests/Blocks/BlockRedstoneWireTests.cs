using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockRedstoneWireTests
{
    private static OnTickEvent Tick(FakeWorldContext world, int x = 0, int y = 64, int z = 0) => new(world, x, y, z, world.Reader.GetBlockMeta(x, y, z), world.Reader.GetBlockId(x, y, z));

    [Fact]
    public void NeighborUpdate_WhenSupportMissing_BreaksWireIntoAir()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.RedstoneWire.ID, 7);

        Block.RedstoneWire.NeighborUpdate(Tick(world));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
        Assert.Contains(world.ReaderWriter.SetBlockCalls, c => c is { X: 0, Y: 64, Z: 0, BlockId: 0 });
    }

    [Fact]
    public void NeighborUpdate_WhenRemote_DoesNothing()
    {
        FakeWorldContext world = new()
        {
            IsRemote = true
        };
        world.ReaderWriter.SetInitial(0, 64, 0, Block.RedstoneWire.ID, 4);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);

        Block.RedstoneWire.NeighborUpdate(Tick(world));

        Assert.Equal(Block.RedstoneWire.ID, world.Reader.GetBlockId(0, 64, 0));
        Assert.Equal(4, world.Reader.GetBlockMeta(0, 64, 0));
        Assert.Empty(world.ReaderWriter.SetBlockCalls);
        Assert.Empty(world.ReaderWriter.SetMetaCalls);
    }
}
