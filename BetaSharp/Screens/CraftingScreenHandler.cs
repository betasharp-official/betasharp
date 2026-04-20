using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Recipes;
using BetaSharp.Screens.Slots;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Screens;

public class CraftingScreenHandler : ScreenHandler
{

    public InventoryCrafting input;
    public IInventory result = new InventoryCraftResult();
    private IWorldContext world;
    private int x;
    private int y;
    private int z;

    public CraftingScreenHandler(InventoryPlayer playerInventory, IWorldContext world, int x, int y, int z)
    {
        input = new InventoryCrafting(this, 3, 3);
        this.world = world;
        this.x = x;
        this.y = y;
        this.z = z;
        AddSlot(new CraftingResultSlot(playerInventory.Player, input, result, 0, 124, 35));

        int row;
        int column;
        for (row = 0; row < 3; ++row)
        {
            for (column = 0; column < 3; ++column)
            {
                AddSlot(new Slot(input, column + row * 3, 30 + column * 18, 17 + row * 18));
            }
        }

        for (row = 0; row < 3; ++row)
        {
            for (column = 0; column < 9; ++column)
            {
                AddSlot(new Slot(playerInventory, column + row * 9 + 9, 8 + column * 18, 84 + row * 18));
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
        result.SetStack(0, CraftingManager.getInstance().FindMatchingRecipe(input));
    }

    public override void onClosed(EntityPlayer player)
    {
        base.onClosed(player);
        if (!world.IsRemote)
        {
            for (int i = 0; i < 9; ++i)
            {
                ItemStack itemStack = input.GetStack(i);
                if (itemStack != null)
                {
                    player.inventory.AddItemStackToInventoryOrDrop(itemStack);
                }
            }

        }
    }

    public override bool canUse(EntityPlayer player)
    {
        return world.Reader.GetBlockId(x, y, z) != Block.CraftingTable.ID ? false : player.GetSquaredDistance(x + 0.5D, y + 0.5D, z + 0.5D) <= 64.0D;
    }

    public override ItemStack quickMove(int slotNumber)
    {
        ItemStack movedStack = null;
        Slot slot = Slots[slotNumber];
        if (slot != null && slot.hasStack())
        {
            ItemStack slotStack = slot.getStack();
            movedStack = slotStack.copy();
            if (slotNumber == 0)
            {
                insertItem(slotStack, 10, 46, true);
            }
            else if (slotNumber >= 10 && slotNumber < 37)
            {
                insertItem(slotStack, 37, 46, false);
            }
            else if (slotNumber >= 37 && slotNumber < 46)
            {
                insertItem(slotStack, 10, 37, false);
            }
            else
            {
                insertItem(slotStack, 10, 46, false);
            }

            if (slotStack.Count == 0)
            {
                slot.setStack(null);
            }
            else
            {
                slot.markDirty();
            }

            if (slotStack.Count == movedStack.Count)
            {
                return null;
            }

            slot.onTakeItem(slotStack);
        }

        return movedStack;
    }
}
