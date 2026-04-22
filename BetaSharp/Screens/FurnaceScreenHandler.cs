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
        AddSlot(new FurnaceOutputSlot(playerInventory.Player, furnace, 2, 116, 35));

        int row;
        for (row = 0; row < 3; ++row)
        {
            for (int column = 0; column < 9; ++column)
            {
                AddSlot(new Slot(playerInventory, column + row * 9 + 9, 8 + column * 18, 84 + row * 18));
            }
        }

        for (row = 0; row < 9; ++row)
        {
            AddSlot(new Slot(playerInventory, row, 8 + row * 18, 142));
        }

    }

    public override void AddListener(ScreenHandlerListener listener)
    {
        base.AddListener(listener);
        listener.onPropertyUpdate(this, 0, furnaceBlockEntity.CookTime);
        listener.onPropertyUpdate(this, 1, furnaceBlockEntity.BurnTime);
        listener.onPropertyUpdate(this, 2, furnaceBlockEntity.FuelTime);
    }

    public override void SendContentUpdates()
    {
        base.SendContentUpdates();

        for (int listenerIndex = 0; listenerIndex < Listeners.Count; ++listenerIndex)
        {
            ScreenHandlerListener listener = Listeners[listenerIndex];
            if (cookTime != furnaceBlockEntity.CookTime)
            {
                listener.onPropertyUpdate(this, 0, furnaceBlockEntity.CookTime);
            }

            if (burnTime != furnaceBlockEntity.BurnTime)
            {
                listener.onPropertyUpdate(this, 1, furnaceBlockEntity.BurnTime);
            }

            if (fuelTime != furnaceBlockEntity.FuelTime)
            {
                listener.onPropertyUpdate(this, 2, furnaceBlockEntity.FuelTime);
            }
        }

        cookTime = furnaceBlockEntity.CookTime;
        burnTime = furnaceBlockEntity.BurnTime;
        fuelTime = furnaceBlockEntity.FuelTime;
    }

    public override void setProperty(int id, int value)
    {
        if (id == 0)
        {
            furnaceBlockEntity.CookTime = value;
        }

        if (id == 1)
        {
            furnaceBlockEntity.BurnTime = value;
        }

        if (id == 2)
        {
            furnaceBlockEntity.FuelTime = value;
        }

    }

    public override bool canUse(EntityPlayer player)
    {
        return furnaceBlockEntity.CanPlayerUse(player);
    }

    public override ItemStack quickMove(int slotNumber)
    {
        ItemStack movedStack = null;
        Slot slot = Slots[slotNumber];
        if (slot != null && slot.hasStack())
        {
            ItemStack slotStack = slot.getStack();
            movedStack = slotStack.copy();
            if (slotNumber == 2)
            {
                insertItem(slotStack, 3, 39, true);
            }
            else if (slotNumber >= 3 && slotNumber < 30)
            {
                insertItem(slotStack, 30, 39, false);
            }
            else if (slotNumber >= 30 && slotNumber < 39)
            {
                insertItem(slotStack, 3, 30, false);
            }
            else
            {
                insertItem(slotStack, 3, 39, false);
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
