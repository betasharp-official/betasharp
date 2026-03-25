using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class MushroomTests
{
    [Fact]
    public void CanGrow_SucceedsInDarknessOnGrass()
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, Block.GrassBlock.Id);
        world.SetLightLevel(0, 0, 0, 10);
        
        BlockMushroom mushroom = (BlockMushroom)Block.BrownMushroom;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, mushroom.Id);
        
        Assert.True(mushroom.CanGrow(tickEvent));
    }

    [Fact]
    public void CanGrow_FailsInSunlight()
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, Block.GrassBlock.Id);
        world.SetLightLevel(0, 0, 0, 15);
        
        BlockMushroom mushroom = (BlockMushroom)Block.RedMushroom;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, mushroom.Id);
        
        Assert.False(mushroom.CanGrow(tickEvent));
    }
}
