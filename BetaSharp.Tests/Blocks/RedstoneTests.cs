using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class RedstoneTests
{
    [Fact]
    public void RedstoneWire_SignalStrengthDecaysOverDistance()
    {
        FakeWorld world = new();
        
        // Floor Support
        world.SetBlockInternal(0, -1, 0, Block.Stone.Id);
        world.SetBlockInternal(1, -1, 0, Block.Stone.Id);
        world.SetBlockInternal(2, -1, 0, Block.Stone.Id);
        
        // Redstone Torch at 0, 0, 0
        world.SetBlockInternal(0, 0, 0, Block.LitRedstoneTorch.Id);
        world.SetBlockMetaInternal(0, 0, 0, 5); // 5 = floor mounted

        
        // Wire at 1, 0, 0
        world.SetBlockInternal(1, 0, 0, Block.RedstoneWire.Id);
        Block.RedstoneWire.NeighborUpdate(new OnTickEvent(world, 1, 0, 0, 0, Block.RedstoneWire.Id));
        
        // Adjacent wire should have power 15
        Assert.Equal(15, world.Reader.GetBlockMeta(1, 0, 0));
        
        // Wire at 2, 0, 0
        world.SetBlockInternal(2, 0, 0, Block.RedstoneWire.Id);
        Block.RedstoneWire.NeighborUpdate(new OnTickEvent(world, 2, 0, 0, 0, Block.RedstoneWire.Id));
        
        // Wire 1 block away should have power 14
        Assert.Equal(14, world.Reader.GetBlockMeta(2, 0, 0));
    }

    [Fact]
    public void RedstoneTorch_TurnsOffWhenPowered()
    {
        FakeWorld world = new();
        
        // Place stone at 0, 0, 0
        world.SetBlockInternal(0, 0, 0, Block.Stone.Id);
        
        // Place Redstone Wire ON TOP of the stone
        world.SetBlockInternal(0, 1, 0, Block.RedstoneWire.Id);
        world.SetBlockMetaInternal(0, 1, 0, 15); // forceful power
        
        // Torch attached to stone at -1, 0, 0 (metadata 2 assumes attached to east face 'Stone' at 0,0,0)
        world.SetBlockInternal(-1, 0, 0, Block.LitRedstoneTorch.Id);
        world.SetBlockMetaInternal(-1, 0, 0, 2);
        
        // Trigger neighbor update on torch
        Block.LitRedstoneTorch.NeighborUpdate(new OnTickEvent(world, -1, 0, 0, 2, Block.LitRedstoneTorch.Id));
        
        // The TickScheduler would normally schedule the torch to turn off.
        // For FakeWorld, our fake scheduler doesn't do anything, so we manually call OnTick for the torch.
        Block.LitRedstoneTorch.OnTick(new OnTickEvent(world, -1, 0, 0, 2, Block.LitRedstoneTorch.Id));

        
        // Torch should now be off
        Assert.Equal(Block.RedstoneTorch.Id, world.Reader.GetBlockId(-1, 0, 0));
    }

    [Fact]
    public void Lever_PowersWireWhenToggled()
    {
        FakeWorld world = new();
        
        // Floor support
        world.SetBlockInternal(0, -1, 0, Block.Stone.Id);
        
        // Place lever at 0, 0, 0 (floor mounted)
        world.SetBlockInternal(0, 0, 0, Block.Lever.Id);
        world.SetBlockMetaInternal(0, 0, 0, 5); // 5 = floor mounted
        
        // Place wire at 1, 0, 0
        world.SetBlockInternal(1, -1, 0, Block.Stone.Id);
        world.SetBlockInternal(1, 0, 0, Block.RedstoneWire.Id);
        
        // Wire should initially have power 0
        Assert.Equal(0, world.Reader.GetBlockMeta(1, 0, 0));
        
        // Use Lever
        Block.Lever.OnUse(new OnUseEvent(world, null!, 0, 0, 0));
        
        // Trigger neighbor update on wire
        Block.RedstoneWire.NeighborUpdate(new OnTickEvent(world, 1, 0, 0, 0, Block.RedstoneWire.Id));
        
        // Wire should receive power
        Assert.Equal(15, world.Reader.GetBlockMeta(1, 0, 0));
    }

    [Fact]
    public void Button_PowersTemporarily()
    {
        FakeWorld world = new();
        
        world.SetBlockInternal(0, 0, 0, Block.Stone.Id);
        world.SetBlockInternal(-1, 0, 0, Block.Button.Id);
        world.SetBlockMetaInternal(-1, 0, 0, 1); // Attached to East (X+)
        
        // Use Button
        Block.Button.OnUse(new OnUseEvent(world, null!, -1, 0, 0));
        
        Assert.True((world.Reader.GetBlockMeta(-1, 0, 0) & 8) > 0);
        
        // Trigger OnTick artificially to turn it off
        Block.Button.OnTick(new OnTickEvent(world, -1, 0, 0, 1 | 8, Block.Button.Id));
        
        Assert.False((world.Reader.GetBlockMeta(-1, 0, 0) & 8) > 0);
    }
}

