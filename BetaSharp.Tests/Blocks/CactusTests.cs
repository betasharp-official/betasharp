using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class CactusTests
{
    [Fact]
    public void CanGrow_ValidatesGroundBlock()
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, Block.Sand.Id);
        
        BlockCactus cactus = (BlockCactus)Block.Cactus;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, cactus.Id);
        
        Assert.True(cactus.CanGrow(tickEvent));
    }

    [Fact]
    public void CanGrow_FailsWhenAdjacentToSolidBlock()
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, Block.Sand.Id);
        world.SetBlock(1, 0, 0, Block.Stone.Id); // Adjacent solid block
        
        BlockCactus cactus = (BlockCactus)Block.Cactus;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, cactus.Id);
        
        Assert.False(cactus.CanGrow(tickEvent));
    }

    [Fact]
    public void CanGrow_AllowsCactusBelow()
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, Block.Cactus.Id);
        
        BlockCactus cactus = (BlockCactus)Block.Cactus;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, cactus.Id);
        
        Assert.True(cactus.CanGrow(tickEvent));
    }
}
