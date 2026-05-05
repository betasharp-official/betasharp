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
        stack.onCraft(thePlayer.World, thePlayer);
        if (stack.ItemId == Block.CraftingTable.id)
        {
            thePlayer.IncreaseStat(Achievements.BuildWorkbench, 1);
        }
        else if (stack.ItemId == Item.WoodenPickaxe.id)
        {
            thePlayer.IncreaseStat(Achievements.BuildPickaxe, 1);
        }
        else if (stack.ItemId == Block.Furnace.id)
        {
            thePlayer.IncreaseStat(Achievements.BuildFurnace, 1);
        }
        else if (stack.ItemId == Item.WoodenHoe.id)
        {
            thePlayer.IncreaseStat(Achievements.BuildHoe, 1);
        }
        else if (stack.ItemId == Item.Bread.id)
        {
            thePlayer.IncreaseStat(Achievements.MakeBread, 1);
        }
        else if (stack.ItemId == Item.Cake.id)
        {
            thePlayer.IncreaseStat(Achievements.MakeCake, 1);
        }
        else if (stack.ItemId == Item.StonePickaxe.id)
        {
            thePlayer.IncreaseStat(Achievements.CraftStonePickaxe, 1);
        }
        else if (stack.ItemId == Item.WoodenSword.id)
        {
            thePlayer.IncreaseStat(Achievements.CraftSword, 1);
        }

        for (int slotIndex = 0; slotIndex < craftMatrix.Size; ++slotIndex)
        {
            ItemStack? ingredientStack = craftMatrix.GetStack(slotIndex);
            if (ingredientStack != null)
            {
                craftMatrix.RemoveStack(slotIndex, 1);
                if (ingredientStack.getItem().hasContainerItem())
                {
                    craftMatrix.SetStack(slotIndex, new ItemStack(ingredientStack.getItem().getContainerItem()));
                }
            }
        }

    }
}
