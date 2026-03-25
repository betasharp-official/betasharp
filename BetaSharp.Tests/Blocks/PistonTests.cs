using System.Reflection;
using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class PistonTests
{
    [Fact]
    public void CanMoveBlock_ReturnsFalseForObsidian()
    {
        FakeWorld world = new();
        MethodInfo? method = typeof(BlockPistonBase).GetMethod("canMoveBlock", BindingFlags.NonPublic | BindingFlags.Static);
        
        bool result = (bool)method!.Invoke(null, new object[] { Block.Obsidian.Id, world, 0, 64, 0, true })!;
        
        Assert.False(result);
    }

    [Fact]
    public void CanMoveBlock_ReturnsFalseForBedrock()
    {
        FakeWorld world = new();
        MethodInfo? method = typeof(BlockPistonBase).GetMethod("canMoveBlock", BindingFlags.NonPublic | BindingFlags.Static);
        
        bool result = (bool)method!.Invoke(null, new object[] { Block.Bedrock.Id, world, 0, 64, 0, true })!;
        
        Assert.False(result);
    }
    
    [Fact]
    public void CanMoveBlock_ReturnsTrueForStone()
    {
        FakeWorld world = new();
        MethodInfo? method = typeof(BlockPistonBase).GetMethod("canMoveBlock", BindingFlags.NonPublic | BindingFlags.Static);
        
        bool result = (bool)method!.Invoke(null, new object[] { Block.Stone.Id, world, 0, 64, 0, true })!;
        
        Assert.True(result);
    }

    [Fact]
    public void CanExtend_ReturnsTrueWhenWithinLimit()
    {
        FakeWorld world = new();
        
        // Piston at 0,64,0 facing North (z--). Blocks at 0,64,-1 to 0,64,-10 (10 blocks). Air after. Limit is 12.
        for (int z = -1; z >= -10; z--)
        {
            world.SetBlock(0, 64, z, Block.Stone.Id);
        }
        
        MethodInfo? method = typeof(BlockPistonBase).GetMethod("canExtend", BindingFlags.NonPublic | BindingFlags.Static);
        bool result = (bool)method!.Invoke(null, new object[] { world, 0, 64, 0, 2 })!; // 2 = North
        
        Assert.True(result);
    }

    [Fact]
    public void CanExtend_ReturnsFalseWhenExceedingLimit()
    {
        FakeWorld world = new();
        
        // 13 blocks. Limit is 12.
        for (int z = -1; z >= -13; z--)
        {
            world.SetBlock(0, 64, z, Block.Stone.Id);
        }
        
        MethodInfo? method = typeof(BlockPistonBase).GetMethod("canExtend", BindingFlags.NonPublic | BindingFlags.Static);
        bool result = (bool)method!.Invoke(null, new object[] { world, 0, 64, 0, 2 })!; // 2 = North
        
        Assert.False(result);
    }
}
