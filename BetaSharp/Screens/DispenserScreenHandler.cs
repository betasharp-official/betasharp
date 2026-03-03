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

        int var3;
        int var4;
        for (var3 = 0; var3 < 3; ++var3)
        {
            for (var4 = 0; var4 < 3; ++var4)
            {
                AddSlot(new Slot(dispenser, var4 + var3 * 3, 62 + var4 * 18, 17 + var3 * 18));
            }
        }

        for (var3 = 0; var3 < 3; ++var3)
        {
            for (var4 = 0; var4 < 9; ++var4)
            {
                AddSlot(new Slot(playerInventory, var4 + var3 * 9 + 9, 8 + var4 * 18, 84 + var3 * 18));
            }
        }

        for (var3 = 0; var3 < 9; ++var3)
        {
            AddSlot(new Slot(playerInventory, var3, 8 + var3 * 18, 142));
        }

    }

    public override bool canUse(EntityPlayer player)
    {
        return dispenserBlockEntity.canPlayerUse(player);
    }
}
