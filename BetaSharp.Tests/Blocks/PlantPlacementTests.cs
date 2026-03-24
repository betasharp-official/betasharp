using BetaSharp.Blocks;
using BetaSharp.Tests.Fakes;

namespace BetaSharp.Tests.Blocks;

public class PlantPlacementTests
{
    [Theory]
    // Saplings (Standard Plant rules)
    [InlineData(6, 2, true)] // Sapling on Grass -> True
    [InlineData(6, 3, true)] // Sapling on Dirt -> True
    [InlineData(6, 20, false)] // Sapling on Glass -> False
    // DeadBush rules
    [InlineData(32, 12, true)] // Deadbush on Sand -> True
    [InlineData(32, 3, false)] // Deadbush on Dirt -> False
    // Crop rules
    [InlineData(59, 60, true)] // Crops on Farmland -> True
    [InlineData(59, 3, false)] // Crops on Dirt -> False
    public void CanGrow_ValidatesGroundBlockProperly(int plantId, int groundBlockId, bool expectedCanGrow)
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, groundBlockId);

        Block? plantBlock = Block.Blocks[plantId];
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, plantId);
        bool canGrow = plantBlock.CanGrow(tickEvent);

        Assert.Equal(expectedCanGrow, canGrow);
    }

    [Fact]
    public void Mushroom_CannotGrowInDirectSunlight()
    {
        FakeWorld world = new();

        world.SetBlock(0, -1, 0, Block.GrassBlock.Id);
        world.SetLightLevel(0, 0, 0, 15);

        BlockMushroom mushroom = (BlockMushroom)Block.BrownMushroom;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, mushroom.Id);
        bool canGrow = mushroom.CanGrow(tickEvent);

        Assert.False(canGrow, "Mushrooms should not survive at light level >= 13");
    }
}

public class PlantBehaviorTests
{
    [Theory]
    [InlineData(6, 2, true)] // Sapling on Grass
    [InlineData(6, 20, false)] // Sapling on Glass
    [InlineData(59, 60, true)] // Crops on Farmland
    public void CanGrow_ValidatesGroundBlockProperly(int plantId, int groundBlockId, bool expectedCanGrow)
    {
        FakeWorld world = new();
        world.SetBlock(0, -1, 0, groundBlockId);

        Block? plantBlock = Block.Blocks[plantId];
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, plantId);
        bool canGrow = plantBlock.CanGrow(tickEvent);

        Assert.Equal(expectedCanGrow, canGrow);
    }

    [Fact]
    public void NeighborUpdate_BreaksPlantIfGroundIsRemoved()
    {
        FakeWorld world = new();
        world.SetBlock(0, 0, 0, 37);
        world.SetBlock(0, -1, 0, 0);

        BlockPlant plant = Block.Dandelion;
        OnTickEvent tickEvent = new(world, 0, 0, 0, 0, plant.Id);
        plant.NeighborUpdate(tickEvent);

        int blockAtPlantPosition = world.Reader.GetBlockId(0, 0, 0);
        Assert.Equal(0, blockAtPlantPosition);
    }
}

public class BlockCropsTests
{
    [Theory]
    [InlineData(0, -1)]
    [InlineData(3, -1)]
    [InlineData(7, 296)]
    public void GetDroppedItemId_ReturnsWheatOnlyWhenFullyGrown(int metadata, int expectedDropId)
    {
        BlockCrops crops = (BlockCrops)Block.Wheat;
        int droppedId = crops.GetDroppedItemId(metadata);
        Assert.Equal(expectedDropId, droppedId);
    }
}
