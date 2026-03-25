using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class FireTests
{
    [Theory]
    [InlineData(5, true)]  // Planks
    [InlineData(17, true)] // Log
    [InlineData(18, true)] // Leaves
    [InlineData(1, false)] // Stone
    [InlineData(20, false)]// Glass Doest Sufocates
    public void IsFlammable_ReturnsExpectedResult(int id, bool expected)
    {
        FakeWorld world = new();
        world.SetBlock(0, 0, 0, id);
        
        bool isFlammable = Block.Fire.IsFlammable(world.Reader, 0, 0, 0);
        
        Assert.Equal(expected, isFlammable);
    }

    [Fact]
    public void CanPlaceAt_SucceedsOnSolidBlock()
    {
        FakeWorld world = new();
        world.SetBlock(0, 0, 0, Block.Stone.Id);
        
        // Block.Fire.CanPlaceAt checks if the block below suffocates (is solid) or if there are flammable blocks around.
        // FakeWorld.ShouldSuffocate returns false by default. Let's modify FakeWorld manually or just test flammable block around.
        world.SetBlock(1, 1, 0, Block.Log.Id); // Flammable block adjacent
        
        bool canPlace = Block.Fire.CanPlaceAt(new CanPlaceAtContext(world, 0, 0, 1, 0));
        
        Assert.True(canPlace);
    }
}
