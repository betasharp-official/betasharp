using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Screens.Slots;

namespace BetaSharp.Screens;

public class FurnaceScreenHandler : ScreenHandler
{

    private BlockEntityFurnace furnaceBlockEntity;
    private int cookTime;
    private int burnTime;
    private int fuelTime;

    public FurnaceScreenHandler(InventoryPlayer playerInventory, BlockEntityFurnace furnace)
    {
        furnaceBlockEntity = furnace;
        addSlot(new Slot(furnace, 0, 56, 17));
        addSlot(new Slot(furnace, 1, 56, 53));
        addSlot(new FurnaceOutputSlot(playerInventory.player, furnace, 2, 116, 35));

        int var3;
        for (var3 = 0; var3 < 3; ++var3)
        {
            for (int var4 = 0; var4 < 9; ++var4)
            {
                addSlot(new Slot(playerInventory, var4 + var3 * 9 + 9, 8 + var4 * 18, 84 + var3 * 18));
            }
        }

        for (var3 = 0; var3 < 9; ++var3)
        {
            addSlot(new Slot(playerInventory, var3, 8 + var3 * 18, 142));
        }

    }

    public override void addListener(ScreenHandlerListener listener)
    {
        base.addListener(listener);
        listener.onPropertyUpdate(this, 0, furnaceBlockEntity.cookTime);
        listener.onPropertyUpdate(this, 1, furnaceBlockEntity.burnTime);
        listener.onPropertyUpdate(this, 2, furnaceBlockEntity.fuelTime);
    }

    public override void sendContentUpdates()
    {
        base.sendContentUpdates();

        for (int var1 = 0; var1 < listeners.size(); ++var1)
        {
            ScreenHandlerListener var2 = (ScreenHandlerListener)listeners.get(var1);
            if (cookTime != furnaceBlockEntity.cookTime)
            {
                var2.onPropertyUpdate(this, 0, furnaceBlockEntity.cookTime);
            }

            if (burnTime != furnaceBlockEntity.burnTime)
            {
                var2.onPropertyUpdate(this, 1, furnaceBlockEntity.burnTime);
            }

            if (fuelTime != furnaceBlockEntity.fuelTime)
            {
                var2.onPropertyUpdate(this, 2, furnaceBlockEntity.fuelTime);
            }
        }

        cookTime = furnaceBlockEntity.cookTime;
        burnTime = furnaceBlockEntity.burnTime;
        fuelTime = furnaceBlockEntity.fuelTime;
    }

    public override void setProperty(int id, int value)
    {
        if (id == 0)
        {
            furnaceBlockEntity.cookTime = value;
        }

        if (id == 1)
        {
            furnaceBlockEntity.burnTime = value;
        }

        if (id == 2)
        {
            furnaceBlockEntity.fuelTime = value;
        }

    }

    public override bool canUse(EntityPlayer player)
    {
        return furnaceBlockEntity.canPlayerUse(player);
    }

    public override ItemStack quickMove(int slot)
    {
        ItemStack var2 = null;
        Slot var3 = (Slot)slots.get(slot);
        if (var3 != null && var3.hasStack())
        {
            ItemStack var4 = var3.getStack();
            var2 = var4.copy();
            if (slot == 2)
            {
                insertItem(var4, 3, 39, true);
            }
            else if (slot >= 3 && slot < 30)
            {
                insertItem(var4, 30, 39, false);
            }
            else if (slot >= 30 && slot < 39)
            {
                insertItem(var4, 3, 30, false);
            }
            else
            {
                insertItem(var4, 3, 39, false);
            }

            if (var4.count == 0)
            {
                var3.setStack(null);
            }
            else
            {
                var3.MarkDirty();
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
