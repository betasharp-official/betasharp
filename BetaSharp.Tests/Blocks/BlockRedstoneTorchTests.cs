using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockRedstoneTorchTests
{
    private static OnTickEvent Tick(FakeWorldContext world, int x = 0, int y = 64, int z = 0) => new(world, x, y, z, world.Reader.GetBlockMeta(x, y, z), world.Reader.GetBlockId(x, y, z));

    [Fact]
    public void NeighborUpdate_AlwaysSchedulesTwoTickDelay()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.id);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.RedstoneTorch.id, 5);

        Block.RedstoneTorch.neighborUpdate(Tick(world));

        Assert.Contains(world.TickSchedulerSpy.ScheduledTicks, t =>
            t is { X: 0, Y: 64, Z: 0 } && t.BlockId == Block.RedstoneTorch.id && t.TickRate == 2);
    }

    [Fact]
    public void OnTick_LitTorch_WhenReceivingPower_TurnsIntoUnlitTorch()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.LitRedstoneTorch.id, 5);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.LitRedstoneTorch.id); // powers from below

        Block.LitRedstoneTorch.onTick(Tick(world));

        Assert.Equal(Block.RedstoneTorch.id, world.Reader.GetBlockId(0, 64, 0));
        Assert.Equal(5, world.Reader.GetBlockMeta(0, 64, 0));
    }
}
