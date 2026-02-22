using BetaSharp.Blocks;
using BetaSharp.Items;

namespace BetaSharp.Recipes;

public class SmeltingRecipeManager
{
    private static readonly SmeltingRecipeManager smeltingBase = new();
    private Dictionary<int, ItemStack> smeltingList = new();

    public static SmeltingRecipeManager getInstance()
    {
        return smeltingBase;
    }

    private SmeltingRecipeManager()
    {
        AddSmelting(Block.IronOre.id, new ItemStack(Item.IronIngot));
        AddSmelting(Block.GoldOre.id, new ItemStack(Item.GoldIngot));
        AddSmelting(Block.DiamondOre.id, new ItemStack(Item.Diamond));
        AddSmelting(Block.Sand.id, new ItemStack(Block.Glass));
        AddSmelting(Item.RawPorkchop.id, new ItemStack(Item.CookedPorkchop));
        AddSmelting(Item.RawFish.id, new ItemStack(Item.CookedFish));
        AddSmelting(Item.RawChicken.id, new ItemStack(Item.CookedChicken));
        AddSmelting(Item.Beef.id, new ItemStack(Item.CookedPorkchop));
        AddSmelting(Item.RawMutton.id, new ItemStack(Item.CookenMutton));
        AddSmelting(Block.Cobblestone.id, new ItemStack(Block.Stone));
        AddSmelting(Item.Clay.id, new ItemStack(Item.Brick));
        AddSmelting(Block.Cactus.id, new ItemStack(Item.Dye, 1, 2));
        AddSmelting(Block.Log.id, new ItemStack(Item.Coal, 1, 1));
    }

    public void AddSmelting(int inputId, ItemStack output)
    {
        smeltingList[inputId] = output;
    }

    public ItemStack? Craft(int inputId)
    {
        if (smeltingList.TryGetValue(inputId, out ItemStack? result))
        {
            return result;
        }
        return null;
    }

    public Dictionary<int, ItemStack> GetSmeltingList()
    {
        return smeltingList;
    }
}
