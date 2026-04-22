using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockDoorTests
{
    [Fact]
    public void NeighborUpdate_TopHalfWithoutBottom_BreaksTopHalf()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Door.id, 8); // top half only

        Block.Door.neighborUpdate(new OnTickEvent(world, 0, 65, 0, 8, Block.Stone.id));

        Assert.Equal(0, world.Reader.GetBlockId(0, 65, 0));
    }

    [Fact]
    public void NeighborUpdate_BottomHalfWithoutTop_BreaksBottomHalf()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.id); // support below
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Door.id); // bottom half only

        Block.Door.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.Stone.id));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void NeighborUpdate_RedstonePower_TogglesBottomAndTopOpenBits()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.id);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Door.id); // bottom closed
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Door.id, 8); // top closed
        world.ReaderWriter.SetInitial(-1, 64, 0, Block.LitRedstoneTorch.id); // powers door block

        Block.Door.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.LitRedstoneTorch.id));

        Assert.Equal(4, world.Reader.GetBlockMeta(0, 64, 0)); // bottom open
        Assert.Equal(12, world.Reader.GetBlockMeta(0, 65, 0)); // top open + top bit
    }
}
