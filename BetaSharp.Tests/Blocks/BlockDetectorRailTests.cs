using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockDetectorRailTests
{
    private static OnTickEvent Tick(FakeWorldContext world, int x = 0, int y = 64, int z = 0) => new(world, x, y, z, world.Reader.GetBlockMeta(x, y, z), world.Reader.GetBlockId(x, y, z));

    [Fact]
    public void OnTick_UnpoweredDetectorRail_DoesNothing()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Stone.ID);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.DetectorRail.ID);

        Block.DetectorRail.OnTick(Tick(world));

        Assert.Equal(Block.DetectorRail.ID, world.Reader.GetBlockId(0, 64, 0));
        Assert.Equal(0, world.Reader.GetBlockMeta(0, 64, 0));
        Assert.Empty(world.TickSchedulerSpy.ScheduledTicks);
    }

    [Fact]
    public void OnEntityCollision_WithMinecartInDetectionVolume_SetsPoweredMeta()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(8, 63, 8, Block.Stone.ID);
        world.ReaderWriter.SetInitial(8, 64, 8, Block.DetectorRail.ID, 0);

        EntityMinecart cart = new(world, 8.5D, 64.0D, 8.5D, 0);
        world.Entities.SpawnEntity(cart);

        Block.DetectorRail.OnEntityCollision(new OnEntityCollisionEvent(world, cart, 8, 64, 8));

        Assert.NotEqual(0, world.Reader.GetBlockMeta(8, 64, 8) & 8);
        Assert.Contains(world.TickSchedulerSpy.ScheduledTicks, t =>
            t is { X: 8, Y: 64, Z: 8 } && t.BlockId == Block.DetectorRail.ID);
    }

    [Fact]
    public void OnTick_PoweredWithoutMinecart_ClearsPoweredMeta()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(9, 63, 9, Block.Stone.ID);
        world.ReaderWriter.SetInitial(9, 64, 9, Block.DetectorRail.ID, 8);

        Block.DetectorRail.OnTick(Tick(world, 9, 64, 9));

        Assert.Equal(0, world.Reader.GetBlockMeta(9, 64, 9) & 8);
    }
}
