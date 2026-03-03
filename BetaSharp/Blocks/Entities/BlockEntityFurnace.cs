using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Recipes;

namespace BetaSharp.Blocks.Entities;

public class BlockEntityFurnace : BlockEntity, IInventory
{
    private ItemStack[] inventory = new ItemStack[3];
    public int burnTime;
    public int fuelTime;
    public int cookTime;

    public int size()
    {
        return inventory.Length;
    }

    public ItemStack getStack(int slot)
    {
        return inventory[slot];
    }

    public ItemStack removeStack(int slot, int stack)
    {
        if (inventory[slot] != null)
        {
            ItemStack removedStack;
            if (inventory[slot].count <= stack)
            {
                removedStack = inventory[slot];
                inventory[slot] = null;
                return removedStack;
            }
            else
            {
                removedStack = inventory[slot].split(stack);
                if (inventory[slot].count == 0)
                {
                    inventory[slot] = null;
                }

                return removedStack;
            }
        }
        else
        {
            return null;
        }
    }

    public void setStack(int slot, ItemStack? stack)
    {
        inventory[slot] = stack;
        if (stack != null && stack.count > getMaxCountPerStack())
        {
            stack.count = getMaxCountPerStack();
        }

    }

    public string getName()
    {
        return "Furnace";
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        NBTTagList itemList = nbt.GetTagList("Items");
        inventory = new ItemStack[size()];

        for (int itemIndex = 0; itemIndex < itemList.TagCount(); ++itemIndex)
        {
            NBTTagCompound itemTag = (NBTTagCompound)itemList.TagAt(itemIndex);
            sbyte slot = itemTag.GetByte("Slot");
            if (slot >= 0 && slot < inventory.Length)
            {
                inventory[slot] = new ItemStack(itemTag);
            }
        }

        burnTime = nbt.GetShort("BurnTime");
        cookTime = nbt.GetShort("CookTime");
        fuelTime = getFuelTime(inventory[1]);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetShort("BurnTime", (short)burnTime);
        nbt.SetShort("CookTime", (short)cookTime);
        NBTTagList itemList = new NBTTagList();

        for (int slotIndex = 0; slotIndex < inventory.Length; ++slotIndex)
        {
            if (inventory[slotIndex] != null)
            {
                NBTTagCompound slotTag = new NBTTagCompound();
                slotTag.SetByte("Slot", (sbyte)slotIndex);
                inventory[slotIndex].writeToNBT(slotTag);
                itemList.SetTag(slotTag);
            }
        }

        nbt.SetTag("Items", itemList);
    }

    public int getMaxCountPerStack()
    {
        return 64;
    }

    public int getCookTimeDelta(int multiplier)
    {
        return cookTime * multiplier / 200;
    }

    public int getFuelTimeDelta(int multiplier)
    {
        if (fuelTime == 0)
        {
            fuelTime = 200;
        }

        return burnTime * multiplier / fuelTime;
    }

    public bool isBurning()
    {
        return burnTime > 0;
    }

    public override void tick()
    {
        bool wasBurning = burnTime > 0;
        bool stateChanged = false;
        if (burnTime > 0)
        {
            --burnTime;
        }

        if (!World.isRemote)
        {
            if (burnTime == 0 && canAcceptRecipeOutput())
            {
                fuelTime = burnTime = getFuelTime(inventory[1]);
                if (burnTime > 0)
                {
                    stateChanged = true;
                    if (inventory[1] != null)
                    {
                        --inventory[1].count;
                        if (inventory[1].count == 0)
                        {
                            inventory[1] = null;
                        }
                    }
                }
            }

            if (isBurning() && canAcceptRecipeOutput())
            {
                ++cookTime;
                if (cookTime == 200)
                {
                    cookTime = 0;
                    craftRecipe();
                    stateChanged = true;
                }
            }
            else
            {
                cookTime = 0;
            }

            if (wasBurning != burnTime > 0)
            {
                stateChanged = true;
                BlockFurnace.updateLitState(burnTime > 0, World, X, Y, Z);
            }
        }

        if (stateChanged)
        {
            markDirty();
        }

    }

    private bool canAcceptRecipeOutput()
    {
        if (inventory[0] == null)
        {
            return false;
        }
        else
        {
            ItemStack outputStack = SmeltingRecipeManager.getInstance().Craft(inventory[0].getItem().id);
            return outputStack == null ? false : inventory[2] == null ? true : !inventory[2].isItemEqual(outputStack) ? false : inventory[2].count < getMaxCountPerStack() && inventory[2].count < inventory[2].getMaxCount() ? true : inventory[2].count < outputStack.getMaxCount();
        }
    }

    public void craftRecipe()
    {
        if (canAcceptRecipeOutput())
        {
            ItemStack outputStack = SmeltingRecipeManager.getInstance().Craft(inventory[0].getItem().id);
            if (inventory[2] == null)
            {
                inventory[2] = outputStack.copy();
            }
            else if (inventory[2].itemId == outputStack.itemId)
            {
                ++inventory[2].count;
            }

            --inventory[0].count;
            if (inventory[0].count <= 0)
            {
                inventory[0] = null;
            }

        }
    }

    private int getFuelTime(ItemStack itemStack)
    {
        if (itemStack == null)
        {
            return 0;
        }
        else
        {
            int itemId = itemStack.getItem().id;
            return itemId < 256 && Block.Blocks[itemId].material == Material.Wood ? 300 : itemId == Item.Stick.id ? 100 : itemId == Item.Coal.id ? 1600 : itemId == Item.LavaBucket.id ? 20000 : itemId == Block.Sapling.id ? 100 : 0;
        }
    }

    public bool canPlayerUse(EntityPlayer player)
    {
        return World.getBlockEntity(X, Y, Z) != this ? false : player.getSquaredDistance(X + 0.5D, Y + 0.5D, Z + 0.5D) <= 64.0D;
    }
}
