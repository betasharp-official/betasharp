using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class FluidInteractionTests
{
    [Fact]
    public void CheckBlockCollisions_LavaSourceCreatesObsidianWhenTouchingWater()
    {
        FakeWorld world = new();
        
        // Lava at 0, 0, 0 (meta 0 = Source)
        world.SetBlock(0, 0, 0, Block.Lava.Id, 0);
        
        // Water at 0, 0, 1
        world.SetBlockInternal(0, 0, 1, Block.Water.Id);
        
        // Trigger NeighborUpdate on Lava
        BlockFluid lava = (BlockFluid)Block.Lava;
        lava.NeighborUpdate(new OnTickEvent(world, 0, 0, 0, 0, lava.Id));
        
        // Check if Lava became Obsidian
        Assert.Equal(Block.Obsidian.Id, world.Reader.GetBlockId(0, 0, 0));
    }

    [Fact]
    public void CheckBlockCollisions_FlowingLavaCreatesCobblestoneWhenTouchingWater()
    {
        FakeWorld world = new();
        
        // Flowing Lava at 0, 0, 0 (meta 1 = Flowing)
        world.SetBlock(0, 0, 0, Block.Lava.Id, 1);
        
        // Water at 0, 0, 1
        world.SetBlockInternal(0, 0, 1, Block.Water.Id);
        
        // Trigger NeighborUpdate on Lava
        BlockFluid lava = (BlockFluid)Block.Lava;
        lava.NeighborUpdate(new OnTickEvent(world, 0, 0, 0, 1, lava.Id));
        
        // Check if Lava became Cobblestone
        Assert.Equal(Block.Cobblestone.Id, world.Reader.GetBlockId(0, 0, 0));
    }
}
