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
        AddSlot(new Slot(furnace, 0, 56, 17));
        AddSlot(new Slot(furnace, 1, 56, 53));
        AddSlot(new FurnaceOutputSlot(playerInventory.player, furnace, 2, 116, 35));

        int var3;
        for (var3 = 0; var3 < 3; ++var3)
        {
            for (int var4 = 0; var4 < 9; ++var4)
            {
                AddSlot(new Slot(playerInventory, var4 + var3 * 9 + 9, 8 + var4 * 18, 84 + var3 * 18));
            }
        }

        for (var3 = 0; var3 < 9; ++var3)
        {
            AddSlot(new Slot(playerInventory, var3, 8 + var3 * 18, 142));
        }

    }

    public override void AddListener(ScreenHandlerListener listener)
    {
        base.AddListener(listener);
        listener.onPropertyUpdate(this, 0, furnaceBlockEntity.cookTime);
        listener.onPropertyUpdate(this, 1, furnaceBlockEntity.burnTime);
        listener.onPropertyUpdate(this, 2, furnaceBlockEntity.fuelTime);
    }

    public override void SendContentUpdates()
    {
        base.SendContentUpdates();

        for (int var1 = 0; var1 < Listeners.Count; ++var1)
        {
            ScreenHandlerListener var2 = Listeners[var1];
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

    public override ItemStack quickMove(int slotNumber)
    {
        ItemStack var2 = null;
        Slot var3 = Slots[slotNumber];
        if (var3 != null && var3.hasStack())
        {
            ItemStack var4 = var3.getStack();
            var2 = var4.copy();
            if (slotNumber == 2)
            {
                insertItem(var4, 3, 39, true);
            }
            else if (slotNumber >= 3 && slotNumber < 30)
            {
                insertItem(var4, 30, 39, false);
            }
            else if (slotNumber >= 30 && slotNumber < 39)
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
