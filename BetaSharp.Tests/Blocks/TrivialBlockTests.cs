using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public class BlockRegistryTests
{
    [Theory]
    [ClassData(typeof(StandardBlockData))]
    public void StandardBlocks_HaveValidConfiguration(Block block, float expectedHardness, string expectedName)
    {
        Assert.Equal(expectedHardness, block.Hardness, 5);
        Assert.Equal($"tile.{expectedName}", block.GetBlockName());
        Assert.NotNull(block.SoundGroup);
    }
}

public class StandardBlockData : TheoryData<Block, float, string>
{
    public StandardBlockData()
    {
        Add(Block.Stone, 1.5f, "stone");
        Add(Block.Cobblestone, 2.0f, "stonebrick");
        Add(Block.GrassBlock, 0.6f, "grass");
        Add(Block.Dirt, 0.5f, "dirt");
        Add(Block.Sand, 0.5f, "sand");
        Add(Block.Gravel, 0.6f, "gravel");
        Add(Block.Planks, 2.0f, "wood");
        Add(Block.Bedrock, -1.0f, "bedrock");
        Add(Block.Glass, 0.3f, "glass");
        Add(Block.Bricks, 2.0f, "brick");
        Add(Block.Obsidian, 10.0f, "obsidian");
        Add(Block.Netherrack, 0.4f, "hellrock");
        Add(Block.SoulSand, 0.5f, "hellsand");
        Add(Block.Clay, 0.6f, "clay");
        Add(Block.Snow, 0.1f, "snow");
        Add(Block.Ice, 0.5f, "ice");
        Add(Block.Sapling, 0.0f, "sapling");
        Add(Block.Log, 2.0f, "log");
        Add(Block.Leaves, 0.2f, "leaves");
        Add(Block.Wool, 0.8f, "cloth");
    }
}
