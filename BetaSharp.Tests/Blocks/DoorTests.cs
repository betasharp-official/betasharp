using BetaSharp.Blocks;
using BetaSharp.Items;

namespace BetaSharp.Tests.Blocks;

public class DoorTests
{
    [Fact]
    public void GetDroppedItemId_Returns0ForTopHalf()
    {
        BlockDoor door = (BlockDoor)Block.Door;
        int droppedItem = door.GetDroppedItemId(8); // Meta with bit 8 set indicates top half
        
        Assert.Equal(0, droppedItem);
    }

    [Fact]
    public void GetDroppedItemId_ReturnsDoorItemForBottomHalf()
    {
        BlockDoor woodenDoor = (BlockDoor)Block.Door;
        int droppedWood = woodenDoor.GetDroppedItemId(0);
        
        Assert.Equal(Item.WoodenDoor.id, droppedWood);
        
        BlockDoor ironDoor = (BlockDoor)Block.IronDoor;
        int droppedIron = ironDoor.GetDroppedItemId(0);
        
        Assert.Equal(Item.IronDoor.id, droppedIron);
    }

    [Fact]
    public void IsOpen_ReturnsCorrectState()
    {
        Assert.True(BlockDoor.isOpen(4));
        Assert.False(BlockDoor.isOpen(0));
    }
}
