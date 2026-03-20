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

        int row;
        int col;
        for (row = 0; row < 3; ++row)
        {
            for (col = 0; col < 3; ++col)
            {
                AddSlot(new Slot(input, col + row * 3, 30 + col * 18, 17 + row * 18));
            }
        }

        for (row = 0; row < 3; ++row)
        {
            for (col = 0; col < 9; ++col)
            {
                AddSlot(new Slot(playerInventory, col + row * 9 + 9, 8 + col * 18, 84 + row * 18));
            }
        }

        for (row = 0; row < 9; ++row)
        {
            AddSlot(new Slot(playerInventory, row, 8 + row * 18, 142));
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
            for (int i = 0; i < 9; ++i)
            {
                ItemStack itemStack = input.getStack(i);
                if (itemStack != null)
                {
                    player.dropItem(itemStack);
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
        ItemStack itemStack = null;
        Slot slot = Slots[slotNumber];
        if (slot != null && slot.hasStack())
        {
            ItemStack itemCopy = slot.getStack();
            itemStack = itemCopy.copy();
            if (slotNumber == 0)
            {
                insertItem(itemCopy, 10, 46, true);
            }
            else if (slotNumber >= 10 && slotNumber < 37)
            {
                insertItem(itemCopy, 37, 46, false);
            }
            else if (slotNumber >= 37 && slotNumber < 46)
            {
                insertItem(itemCopy, 10, 37, false);
            }
            else
            {
                insertItem(itemCopy, 10, 46, false);
            }

            if (itemCopy.count == 0)
            {
                slot.setStack(null);
            }
            else
            {
                slot.markDirty();
            }

            if (itemCopy.count == itemStack.count)
            {
                return null;
            }

            slot.onTakeItem(itemCopy);
        }

        return itemStack;
    }
}
