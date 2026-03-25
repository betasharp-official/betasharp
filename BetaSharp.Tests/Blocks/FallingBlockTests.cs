using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class FallingBlockTests
{
    [Theory]
    [InlineData(0, true)] // Air
    [InlineData(51, true)] // Fire
    [InlineData(8, true)] // Flowing Water
    [InlineData(10, true)] // Flowing Lava
    [InlineData(1, false)] // Stone
    public void CanFallThrough_ReturnsExpectedResult(int blockBelowId, bool expected)
    {
        FakeWorld world = new();
        world.SetBlock(0, 5, 0, blockBelowId); // Block below is at y=5
        
        // Request canFallThrough for the block above (y=6). Wait, canFallThrough checks the block at ctx.Y, which is the block below it.
        // Wait, looking at BlockSand.canFallThrough, it checks ctx.Y. But the OnTick calls canFallThrough with y-1.
        // So I'll just pass y=5.
        bool result = BlockSand.canFallThrough(new OnTickEvent(world, 0, 5, 0, 0, Block.Sand.Id));
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void OnTick_WithFallInstantly_TeleportsToGround()
    {
        FakeWorld world = new();
        
        // Ground at y=1
        world.SetBlock(0, 1, 0, Block.Stone.Id);
        
        // Empty space from y=2 to y=4
        
        // Sand at y=5
        world.SetBlock(0, 5, 0, Block.Sand.Id);
        
        BlockSand.FallInstantly = true;
        BlockSand sand = (BlockSand)Block.Sand;
        sand.OnTick(new OnTickEvent(world, 0, 5, 0, 0, sand.Id));
        BlockSand.FallInstantly = false; // Restore
        
        // Old position is air
        Assert.Equal(0, world.Reader.GetBlockId(0, 5, 0));
        
        // New position is sand (y=2 because ground is y=1)
        Assert.Equal(Block.Sand.Id, world.Reader.GetBlockId(0, 2, 0));
    }
}
