using BetaSharp.Blocks.Entities;
using BetaSharp.NBT;
using Xunit;

namespace BetaSharp.Tests;

public class TestWorldSave
{
    [Fact]
    public void TestBlockEntityChestId()
    {
        var chest = new BlockEntityChest();
        var nbt = new NBTTagCompound();
        chest.WriteNbt(nbt);
        Assert.Equal("Chest", nbt.GetString("id"));
    }
}
