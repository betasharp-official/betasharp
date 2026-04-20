using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Screens.Slots;

namespace BetaSharp.Screens;

public class DispenserScreenHandler : ScreenHandler
{
    private BlockEntityDispenser dispenserBlockEntity;

    public DispenserScreenHandler(IInventory playerInventory, BlockEntityDispenser dispenser)
    {
        dispenserBlockEntity = dispenser;

        int row;
        int column;
        for (row = 0; row < 3; ++row)
        {
            for (column = 0; column < 3; ++column)
            {
                AddSlot(new Slot(dispenser, column + row * 3, 62 + column * 18, 17 + row * 18));
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

    }

    public override bool canUse(EntityPlayer player)
    {
        return dispenserBlockEntity.CanPlayerUse(player);
    }
}
