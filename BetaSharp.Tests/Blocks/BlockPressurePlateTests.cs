using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockPressurePlateTests
{
    private static OnTickEvent Tick(FakeWorldContext world, int x = 0, int y = 64, int z = 0) => new(world, x, y, z, world.Reader.GetBlockMeta(x, y, z), world.Reader.GetBlockId(x, y, z));

    [Fact]
    public void NeighborUpdate_WithoutSupport_BreaksPressurePlate()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.StonePressurePlate.id);

        Block.StonePressurePlate.neighborUpdate(Tick(world));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void OnTick_UnpressedPlate_DoesNotScheduleTicks()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.id);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.StonePressurePlate.id);

        Block.StonePressurePlate.onTick(Tick(world));

        Assert.Empty(world.TickSchedulerSpy.ScheduledTicks);
    }
}
