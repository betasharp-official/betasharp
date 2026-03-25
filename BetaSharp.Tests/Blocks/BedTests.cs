using BetaSharp.Blocks;
using BetaSharp.Items;

namespace BetaSharp.Tests.Blocks;

public class BedTests
{
    [Fact]
    public void GetDroppedItemId_Returns0ForHead()
    {
        BlockBed bed = (BlockBed)Block.Bed;
        int droppedItem = bed.GetDroppedItemId(8); // Meta with bit 8 set indicates head of bed
        
        Assert.Equal(0, droppedItem);
    }

    [Fact]
    public void GetDroppedItemId_ReturnsBedItemForFoot()
    {
        BlockBed bed = (BlockBed)Block.Bed;
        int droppedItem = bed.GetDroppedItemId(0);
        
        Assert.Equal(Item.Bed.id, droppedItem);
    }

    [Fact]
    public void IsHeadOfBed_ReturnsCorrectState()
    {
        Assert.True(BlockBed.IsHeadOfBed(8));
        Assert.False(BlockBed.IsHeadOfBed(0));
    }
}
