using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Recipes;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks.Entities;

/// <summary>
/// Block entity for the furnace block, allowing it to store inventory and smelt items.
/// </summary>
public class BlockEntityFurnace : BlockEntityWithInventory<BlockEntityFurnace>
{
    public override BlockEntityType Type => Furnace;

    public override int Size => 3;
    public override string Name => "Furnace";


    public int BurnTime { get; set; }

    public int CookTime { get; set; }

    public int FuelTime { get; set; }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);

        BurnTime = nbt.GetShort("BurnTime");
        CookTime = nbt.GetShort("CookTime");
        FuelTime = GetFuelTime(_inventory[1]);
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetShort("BurnTime", (short)BurnTime);
        nbt.SetShort("CookTime", (short)CookTime);
    }

    public int GetCookTimeDelta(int multiplier)
    {
        return CookTime * multiplier / 200;
    }

    public int GetFuelTimeDelta(int multiplier)
    {
        if (FuelTime == 0)
        {
            FuelTime = 200;
        }

        return BurnTime * multiplier / FuelTime;
    }

    public bool IsBurning => BurnTime > 0;

    public override void Tick(EntityManager entities)
    {
        bool wasBurning = BurnTime > 0;
        bool stateChanged = false;
        if (BurnTime > 0)
        {
            --BurnTime;
        }

        if (!World.IsRemote)
        {
            if (BurnTime == 0 && CanAcceptRecipeOutput())
            {
                FuelTime = BurnTime = GetFuelTime(_inventory[1]);
                if (BurnTime > 0)
                {
                    stateChanged = true;
                    ItemStack? inv1 = _inventory[1];
                    if (inv1 != null)
                    {
                        --inv1.Count;
                        if (inv1.Count == 0)
                        {
                            _inventory[1] = null;
                        }
                    }
                }
            }

            if (IsBurning && CanAcceptRecipeOutput())
            {
                ++CookTime;
                if (CookTime == 200)
                {
                    CookTime = 0;
                    CraftRecipe();
                    stateChanged = true;
                }
            }
            else
            {
                CookTime = 0;
            }

            if (wasBurning != BurnTime > 0)
            {
                stateChanged = true;
                BlockFurnace.UpdateLitState(BurnTime > 0, World, X, Y, Z);
            }
        }

        if (stateChanged)
        {
            MarkDirty();
        }
    }

    private bool CanAcceptRecipeOutput()
    {
        ItemStack? input = _inventory[0];
        if (input is null)
        {
            return false;
        }

        ItemStack? output = SmeltingRecipeManager.getInstance().Craft(input.getItem().id);
        if (output is null)
        {
            return false;
        }

        ItemStack? slot2 = _inventory[2];

        if (slot2 is null)
        {
            return true;
        }

        if (!slot2.isItemEqual(output))
        {
            return false;
        }

        return slot2.Count < MaxCountPerStack &&
               slot2.Count < slot2.getMaxCount() &&
               slot2.Count < output.getMaxCount();
    }

    private void CraftRecipe()
    {
        if (CanAcceptRecipeOutput())
        {
            ItemStack? inv0 = _inventory[0];

            if (inv0 is null) return;

            ItemStack? outputStack = SmeltingRecipeManager.getInstance().Craft(inv0.getItem().id);

            if (outputStack == null) return;

            if (_inventory[2] == null)
            {
                _inventory[2] = outputStack.copy();
            }
            else
            {
                ItemStack? inv2 = _inventory[2];
                if (inv2 != null && inv2.ItemId == outputStack.ItemId)
                {
                    inv2.Count++;
                }
            }

            inv0 = _inventory[0];

            if (inv0 is null) return;

            inv0.Count--;
            if (inv0.Count <= 0)
            {
                _inventory[0] = null;
            }
        }
    }

    private static int GetFuelTime(ItemStack? itemStack)
    {
        if (itemStack == null)
        {
            return 0;
        }

        int itemId = itemStack.getItem().id;
        return itemId < 256 && Block.Blocks[itemId].Material == Material.Wood ? 300 : itemId == Item.Stick.id ? 100 : itemId == Item.Coal.id ? 1600 : itemId == Item.LavaBucket.id ? 20000 : itemId == Block.Sapling.ID ? 100 : 0;
    }
}