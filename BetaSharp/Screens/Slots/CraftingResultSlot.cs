using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Screens.Slots;

internal class CraftingResultSlot : Slot
{

    private readonly IInventory craftMatrix;
    private EntityPlayer thePlayer;

    public CraftingResultSlot(EntityPlayer player, IInventory craftMatrix, IInventory resultInventory, int slotIndex, int x, int y) : base(resultInventory, slotIndex, x, y)
    {
        thePlayer = player;
        this.craftMatrix = craftMatrix;
    }

    public override bool canInsert(ItemStack stack)
    {
        return false;
    }

    public override void onTakeItem(ItemStack stack)
    {
        stack.OnCraft(thePlayer.World, thePlayer);
        if (stack.ItemId == Block.CraftingTable.id)
        {
            thePlayer.increaseStat(Achievements.BuildWorkbench, 1);
        }
        else if (stack.ItemId == Item.WoodenPickaxe.Id)
        {
            thePlayer.increaseStat(Achievements.BuildPickaxe, 1);
        }
        else if (stack.ItemId == Block.Furnace.id)
        {
            thePlayer.increaseStat(Achievements.BuildFurnace, 1);
        }
        else if (stack.ItemId == Item.WoodenHoe.Id)
        {
            thePlayer.increaseStat(Achievements.BuildHoe, 1);
        }
        else if (stack.ItemId == Item.Bread.Id)
        {
            thePlayer.increaseStat(Achievements.MakeBread, 1);
        }
        else if (stack.ItemId == Item.Cake.Id)
        {
            thePlayer.increaseStat(Achievements.MakeCake, 1);
        }
        else if (stack.ItemId == Item.StonePickaxe.Id)
        {
            thePlayer.increaseStat(Achievements.CraftStonePickaxe, 1);
        }
        else if (stack.ItemId == Item.WoodenSword.Id)
        {
            thePlayer.increaseStat(Achievements.CraftSword, 1);
        }

        for (int slotIndex = 0; slotIndex < craftMatrix.Size; ++slotIndex)
        {
            ItemStack? ingredientStack = craftMatrix.GetStack(slotIndex);
            if (ingredientStack != null)
            {
                craftMatrix.RemoveStack(slotIndex, 1);
                if (ingredientStack.GetItem().HasContainerItem())
                {
                    craftMatrix.SetStack(slotIndex, new ItemStack(ingredientStack.GetItem().GetContainerItem()));
                }
            }
        }

    }
}
