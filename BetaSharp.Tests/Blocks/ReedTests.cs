using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class ReedTests
{
    [Fact]
    public void CanGrow_FailsOnDryDirt()
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, Block.Dirt.Id);
        
        BlockReed reed = (BlockReed)Block.SugarCane;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, reed.Id);
        
        Assert.False(reed.CanGrow(tickEvent));
    }

    [Fact]
    public void CanGrow_SucceedsOnDirtAdjacentToWater()
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, Block.Dirt.Id);
        world.SetBlock(1, -1, 0, Block.Water.Id); // Adjacent water
        
        BlockReed reed = (BlockReed)Block.SugarCane;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, reed.Id);
        
        Assert.True(reed.CanGrow(tickEvent));
    }

    [Fact]
    public void CanGrow_AllowsReedBelow()
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, Block.SugarCane.Id);
        
        BlockReed reed = (BlockReed)Block.SugarCane;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, reed.Id);
        
        Assert.True(reed.CanGrow(tickEvent));
    }
}
