using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Screens.Slots;

internal class CraftingResultSlot : Slot
{

    private readonly IInventory craftMatrix;
    private EntityPlayer thePlayer;

    public CraftingResultSlot(EntityPlayer var1, IInventory var2, IInventory var3, int var4, int var5, int var6) : base(var3, var4, var5, var6)
    {
        thePlayer = var1;
        craftMatrix = var2;
    }

    public override bool canInsert(ItemStack var1)
    {
        return false;
    }

    public override void onTakeItem(ItemStack var1)
    {
        var1.onCraft(thePlayer._ctx, thePlayer);
        if (var1.itemId == Block.CraftingTable.id)
        {
            thePlayer.increaseStat(Achievements.BuildWorkbench, 1);
        }
        else if (var1.itemId == Item.WoodenPickaxe.id)
        {
            thePlayer.increaseStat(Achievements.BuildPickaxe, 1);
        }
        else if (var1.itemId == Block.Furnace.id)
        {
            thePlayer.increaseStat(Achievements.BuildFurnace, 1);
        }
        else if (var1.itemId == Item.WoodenHoe.id)
        {
            thePlayer.increaseStat(Achievements.BuildHoe, 1);
        }
        else if (var1.itemId == Item.Bread.id)
        {
            thePlayer.increaseStat(Achievements.MakeBread, 1);
        }
        else if (var1.itemId == Item.Cake.id)
        {
            thePlayer.increaseStat(Achievements.MakeCake, 1);
        }
        else if (var1.itemId == Item.StonePickaxe.id)
        {
            thePlayer.increaseStat(Achievements.CraftStonePickaxe, 1);
        }
        else if (var1.itemId == Item.WoodenSword.id)
        {
            thePlayer.increaseStat(Achievements.CraftSword, 1);
        }

        for (int var2 = 0; var2 < craftMatrix.size(); ++var2)
        {
            ItemStack var3 = craftMatrix.getStack(var2);
            if (var3 != null)
            {
                craftMatrix.removeStack(var2, 1);
                if (var3.getItem().hasContainerItem())
                {
                    craftMatrix.setStack(var2, new ItemStack(var3.getItem().getContainerItem()));
                }
            }
        }

    }
}
