using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;

namespace BetaSharp.Tests.Blocks;

public sealed class BlockFluidTests
{
    [Fact]
    public void LavaNeighborUpdate_WithMetaZeroAndAdjacentWater_HardensToObsidian()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Lava.id, 0);
        world.ReaderWriter.SetInitial(1, 64, 0, Block.Water.id, 0);

        Block.Lava.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.Water.id));

        Assert.Equal(Block.Obsidian.id, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void FlowingLavaNeighborUpdate_WithMetaBetweenOneAndFourAndAdjacentWater_HardensToCobblestone()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.FlowingLava.id, 3);
        world.ReaderWriter.SetInitial(1, 64, 0, Block.Water.id, 0);

        Block.FlowingLava.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 3, Block.Water.id));

        Assert.Equal(Block.Cobblestone.id, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void FlowingLavaNeighborUpdate_WithMetaFourAndAdjacentWater_HardensToCobblestone()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.FlowingLava.id, 4);
        world.ReaderWriter.SetInitial(1, 64, 0, Block.Water.id, 0);

        Block.FlowingLava.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 4, Block.Water.id));

        Assert.Equal(Block.Cobblestone.id, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void FlowingLavaNeighborUpdate_WithMetaAboveFourAndAdjacentWater_DoesNotHarden()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.FlowingLava.id, 5);
        world.ReaderWriter.SetInitial(1, 64, 0, Block.Water.id, 0);

        Block.FlowingLava.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 5, Block.Water.id));

        Assert.Equal(Block.FlowingLava.id, world.Reader.GetBlockId(0, 64, 0));
        Assert.Equal(5, world.Reader.GetBlockMeta(0, 64, 0));
    }

    [Fact]
    public void LavaNeighborUpdate_WithOnlyWaterBelow_DoesNotHarden()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Lava.id, 0);
        world.ReaderWriter.SetInitial(0, 63, 0, Block.Water.id, 0);

        Block.Lava.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.Water.id));

        // Water below is not adjacent for lava/water hardening; still lava may convert to flowing on neighbor tick.
        Assert.NotEqual(Block.Obsidian.id, world.Reader.GetBlockId(0, 64, 0));
        Assert.NotEqual(Block.Cobblestone.id, world.Reader.GetBlockId(0, 64, 0));
        Assert.Equal(0, world.Reader.GetBlockMeta(0, 64, 0));
    }

    [Fact]
    public void LavaNeighborUpdate_WithWaterAbove_HardensToObsidian()
    {
        FakeWorldContext world = new();
        world.ReaderWriter.SetInitial(0, 64, 0, Block.Lava.id, 0);
        world.ReaderWriter.SetInitial(0, 65, 0, Block.Water.id, 0);

        Block.Lava.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 0, Block.Water.id));

        Assert.Equal(Block.Obsidian.id, world.Reader.GetBlockId(0, 64, 0));
    }

    [Fact]
    public void FlowingLavaNeighborUpdate_WhenHardening_EmitsFizzWorldEventAndSmokeParticles()
    {
        FakeWorldContext world = new();
        RecordingWorldEventListener listener = new();
        world.Broadcaster.AddWorldAccess(listener);
        world.ReaderWriter.SetInitial(0, 64, 0, Block.FlowingLava.id, 3);
        world.ReaderWriter.SetInitial(1, 64, 0, Block.Water.id, 0);

        Block.FlowingLava.neighborUpdate(new OnTickEvent(world, 0, 64, 0, 3, Block.Water.id));

        Assert.Contains(listener.WorldEvents, evt => evt.EventId == 1004 && evt.X == 0 && evt.Y == 64 && evt.Z == 0);
        Assert.Equal(8, listener.LargeSmokeParticles);
    }

    private sealed class RecordingWorldEventListener : IWorldEventListener
    {
        public List<(int EventId, int X, int Y, int Z, int Data)> WorldEvents { get; } = [];
        public int LargeSmokeParticles { get; private set; }

        public void BlockUpdate(int x, int y, int z)
        {
        }

        public void SetBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
        }

        public void PlaySound(string var1, double var2, double var4, double var6, float var8, float var9)
        {
        }

        public void SpawnParticle(string var1, double var2, double var4, double var6, double var8, double var10, double var12)
        {
            if (var1 == "largesmoke")
            {
                LargeSmokeParticles++;
            }
        }

        public void NotifyEntityAdded(Entity var1)
        {
        }

        public void NotifyEntityRemoved(Entity var1)
        {
        }

        public void NotifyAmbientDarknessChanged()
        {
        }

        public void PlayNote(int x, int y, int z, int soundType, int pitch)
        {
        }

        public void PlayStreaming(string var1, int var2, int var3, int var4)
        {
        }

        public void UpdateBlockEntity(int var1, int var2, int var3, BlockEntity var4)
        {
        }

        public void WorldEvent(EntityPlayer? player, int @event, int x, int y, int z, int data)
        {
            WorldEvents.Add((@event, x, y, z, data));
        }

        public void BroadcastEntityEvent(Entity entity, byte @event)
        {
        }
    }
}
