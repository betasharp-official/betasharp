using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Recipes;
using BetaSharp.Screens.Slots;
using BetaSharp.Worlds;

namespace BetaSharp.Screens;

public class CraftingScreenHandler : ScreenHandler
{

    public InventoryCrafting input;
    public IInventory result = new InventoryCraftResult();
    private World world;
    private int x;
    private int y;
    private int z;

    public CraftingScreenHandler(InventoryPlayer playerInventory, World world, int x, int y, int z)
    {
        input = new InventoryCrafting(this, 3, 3);
        this.world = world;
        this.x = x;
        this.y = y;
        this.z = z;
        AddSlot(new CraftingResultSlot(playerInventory.player, input, result, 0, 124, 35));

        int var6;
        int var7;
        for (var6 = 0; var6 < 3; ++var6)
        {
            for (var7 = 0; var7 < 3; ++var7)
            {
                AddSlot(new Slot(input, var7 + var6 * 3, 30 + var7 * 18, 17 + var6 * 18));
            }
        }

        for (var6 = 0; var6 < 3; ++var6)
        {
            for (var7 = 0; var7 < 9; ++var7)
            {
                AddSlot(new Slot(playerInventory, var7 + var6 * 9 + 9, 8 + var7 * 18, 84 + var6 * 18));
            }
        }

        for (var6 = 0; var6 < 9; ++var6)
        {
            AddSlot(new Slot(playerInventory, var6, 8 + var6 * 18, 142));
        }

        onSlotUpdate(input);
    }

    public override void onSlotUpdate(IInventory inv)
    {
        result.setStack(0, CraftingManager.getInstance().FindMatchingRecipe(input));
    }

    public override void onClosed(EntityPlayer player)
    {
        base.onClosed(player);
        if (!world.isRemote)
        {
            for (int var2 = 0; var2 < 9; ++var2)
            {
                ItemStack var3 = input.getStack(var2);
                if (var3 != null)
                {
                    player.dropItem(var3);
                }
            }

        }
    }

    public override bool canUse(EntityPlayer player)
    {
        return world.getBlockId(x, y, z) != Block.CraftingTable.id ? false : player.getSquaredDistance(x + 0.5D, y + 0.5D, z + 0.5D) <= 64.0D;
    }

    public override ItemStack quickMove(int slotNumber)
    {
        ItemStack var2 = null;
        Slot var3 = Slots[slotNumber];
        if (var3 != null && var3.hasStack())
        {
            ItemStack var4 = var3.getStack();
            var2 = var4.copy();
            if (slotNumber == 0)
            {
                insertItem(var4, 10, 46, true);
            }
            else if (slotNumber >= 10 && slotNumber < 37)
            {
                insertItem(var4, 37, 46, false);
            }
            else if (slotNumber >= 37 && slotNumber < 46)
            {
                insertItem(var4, 10, 37, false);
            }
            else
            {
                insertItem(var4, 10, 46, false);
            }

            if (var4.count == 0)
            {
                var3.setStack(null);
            }
            else
            {
                var3.markDirty();
            }

            if (var4.count == var2.count)
            {
                return null;
            }

            var3.onTakeItem(var4);
        }

        return var2;
    }
}
