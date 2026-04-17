using BetaSharp.Blocks;
using BetaSharp.Items;
using BetaSharp.Server.Command;

namespace BetaSharp.Tests.Server.Command;

public sealed class ItemLookupTests
{
    [Fact]
    public void TryResolveItemId_Bed_UsesBedItemId()
    {
        bool resolved = ItemLookup.TryResolveItemId("bed", out int itemId);

        Assert.True(resolved);
        Assert.Equal(Item.Bed.id, itemId);
    }

    [Fact]
    public void TryResolveItemId_Stone_UsesBlockItemId()
    {
        bool resolved = ItemLookup.TryResolveItemId("stone", out int itemId);

        Assert.True(resolved);
        Assert.Equal(Block.Stone.id, itemId);
    }

    [Fact]
    public void TryResolveItemId_Door_UsesWoodDoorItemId()
    {
        bool resolved = ItemLookup.TryResolveItemId("door", out int itemId);

        Assert.True(resolved);
        Assert.Equal(Item.WoodenDoor.id, itemId);
    }

    [Fact]
    public void TryResolveItemId_IronDoorAlias_UsesIronDoorItemId()
    {
        bool resolved = ItemLookup.TryResolveItemId("iron_door", out int itemId);

        Assert.True(resolved);
        Assert.Equal(Item.IronDoor.id, itemId);
    }

    [Fact]
    public void TryResolveItemId_Sign_UsesSignItemId()
    {
        bool resolved = ItemLookup.TryResolveItemId("sign", out int itemId);

        Assert.True(resolved);
        Assert.Equal(Item.Sign.id, itemId);
    }

    [Fact]
    public void TryResolveItemId_SugarCaneAlias_UsesSugarCaneItemId()
    {
        bool resolved = ItemLookup.TryResolveItemId("sugar_cane", out int itemId);

        Assert.True(resolved);
        Assert.Equal(Item.SugarCane.id, itemId);
    }
}
