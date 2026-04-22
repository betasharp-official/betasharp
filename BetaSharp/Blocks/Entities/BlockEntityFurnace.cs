using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Recipes;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks.Entities;

public class BlockEntityFurnace : BlockEntity, IInventory
{
    public override BlockEntityType Type => Furnace;
    private ItemStack?[] _inventory = new ItemStack[3];
    public int BurnTime { get; set; }
    public int CookTime { get; set; }
    public int FuelTime { get; set; }

    public int Size => _inventory.Length;

    public ItemStack? GetStack(int slot)
    {
        return _inventory[slot];
    }

    public ItemStack? RemoveStack(int slot, int stack)
    {
        if (_inventory[slot] != null)
        {
            ItemStack removedStack;
            ItemStack? iStack = _inventory[slot];

            if (iStack is null) return null;

            if (iStack.Count <= stack)
            {
                removedStack = iStack;
                _inventory[slot] = null;
                return removedStack;
            }

            removedStack = iStack.Split(stack);
            if (iStack.Count == 0)
            {
                _inventory[slot] = null;
            }

            return removedStack;
        }

        return null;
    }

    public void SetStack(int slot, ItemStack? stack)
    {
        _inventory[slot] = stack;
        if (stack != null && stack.Count > MaxCountPerStack)
        {
            stack.Count = MaxCountPerStack;
        }
    }

    public string Name => "Furnace";

    public int MaxCountPerStack => 64;

    public bool CanPlayerUse(EntityPlayer player)
    {
        return World.Entities.GetBlockEntity<BlockEntityFurnace>(X, Y, Z) == this && player.GetSquaredDistance(X + 0.5D, Y + 0.5D, Z + 0.5D) <= 64.0D;
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        NBTTagList itemList = nbt.GetTagList("Items");
        _inventory = new ItemStack[Size];

        for (int itemIndex = 0; itemIndex < itemList.TagCount(); ++itemIndex)
        {
            NBTTagCompound itemTag = (NBTTagCompound)itemList.TagAt(itemIndex);
            sbyte slot = itemTag.GetByte("Slot");
            if (slot >= 0 && slot < _inventory.Length)
            {
                _inventory[slot] = new ItemStack(itemTag);
            }
        }

        BurnTime = nbt.GetShort("BurnTime");
        CookTime = nbt.GetShort("CookTime");
        FuelTime = GetFuelTime(_inventory[1]);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetShort("BurnTime", (short)BurnTime);
        nbt.SetShort("CookTime", (short)CookTime);
        NBTTagList itemList = new();

        for (int slotIndex = 0; slotIndex < _inventory.Length; ++slotIndex)
        {
            ItemStack? stack = _inventory[slotIndex];
            if (stack != null)
            {
                NBTTagCompound slotTag = new();
                slotTag.SetByte("Slot", (sbyte)slotIndex);
                stack.writeToNBT(slotTag);
                itemList.SetTag(slotTag);
            }
        }

        nbt.SetTag("Items", itemList);
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

    public override void tick(EntityManager entities)
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
                BlockFurnace.updateLitState(BurnTime > 0, World, X, Y, Z);
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
        return itemId < 256 && Block.Blocks[itemId].material == Material.Wood ? 300 : itemId == Item.Stick.id ? 100 : itemId == Item.Coal.id ? 1600 : itemId == Item.LavaBucket.id ? 20000 : itemId == Block.Sapling.id ? 100 : 0;
    }
}
