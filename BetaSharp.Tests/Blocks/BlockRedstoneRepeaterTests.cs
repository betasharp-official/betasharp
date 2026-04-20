using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockRedstoneRepeaterTests
{
    private static OnTickEvent RepeaterEvent(FakeWorldContext world, int x = 0, int y = 64, int z = 0)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        int blockId = world.Reader.GetBlockId(x, y, z);
        return new OnTickEvent(world, x, y, z, meta, blockId);
    }

    [Fact]
    public void NeighborUpdate_UnlitRepeater_WhenPowered_SchedulesTickUsingDelaySetting()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Repeater.ID, 4); // facing=0, delay index=1 => 4 ticks
        world.ReaderWriter.SetInitial(0, 64, 1, Block.RedstoneWire.ID, 15); // power input for facing=0

        Block.Repeater.NeighborUpdate(RepeaterEvent(world));

        Assert.Contains(world.TickSchedulerSpy.ScheduledTicks, t =>
            t is { X: 0, Y: 64, Z: 0 } && t.BlockId == Block.Repeater.ID && t.TickRate == 4);
    }

    [Fact]
    public void NeighborUpdate_LitRepeater_WhenUnpowered_SchedulesTickUsingDelaySetting()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.PoweredRepeater.ID, 8); // facing=0, delay index=2 => 6 ticks

        Block.PoweredRepeater.NeighborUpdate(RepeaterEvent(world));

        Assert.Contains(world.TickSchedulerSpy.ScheduledTicks, t =>
            t is { X: 0, Y: 64, Z: 0 } && t.BlockId == Block.PoweredRepeater.ID && t.TickRate == 6);
    }

    [Fact]
    public void NeighborUpdate_WithoutSupportingBlock_BreaksRepeaterIntoAir()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Repeater.ID);
        // No supporting block at (0,63,0) => ShouldSuffocate false.

        Block.Repeater.NeighborUpdate(RepeaterEvent(world));

        Assert.Equal(0, world.Reader.GetBlockId(0, 64, 0));
        Assert.Contains(world.ReaderWriter.SetBlockCalls, c => c.X == 0 && c.Y == 64 && c.Z == 0 && c.BlockId == 0);
    }

    [Fact]
    public void OnTick_UnlitRepeater_WhenPowered_TransitionsToPoweredRepeaterPreservingMeta()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Repeater.ID, 1); // facing=1
        world.ReaderWriter.SetInitial(-1, 64, 0, Block.RedstoneWire.ID, 7); // power input for facing=1

        Block.Repeater.OnTick(RepeaterEvent(world));

        Assert.Equal(Block.PoweredRepeater.ID, world.Reader.GetBlockId(0, 64, 0));
        Assert.Equal(1, world.Reader.GetBlockMeta(0, 64, 0));
    }

    [Fact]
    public void OnTick_LitRepeater_WhenUnpowered_TransitionsToUnlitRepeaterPreservingMeta()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.PoweredRepeater.ID, 3);

        Block.PoweredRepeater.OnTick(RepeaterEvent(world));

        Assert.Equal(Block.Repeater.ID, world.Reader.GetBlockId(0, 64, 0));
        Assert.Equal(3, world.Reader.GetBlockMeta(0, 64, 0));
    }

    [Fact]
    public void OnTick_UnlitRepeater_WhenStillUnpowered_TurnsOnThenSchedulesPoweredTick()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Repeater.ID, 12); // delay index=3 => 8 ticks

        Block.Repeater.OnTick(RepeaterEvent(world));

        Assert.Equal(Block.PoweredRepeater.ID, world.Reader.GetBlockId(0, 64, 0));
        Assert.Contains(world.TickSchedulerSpy.ScheduledTicks, t =>
            t is { X: 0, Y: 64, Z: 0 } && t.BlockId == Block.PoweredRepeater.ID && t.TickRate == 8);
    }
}
