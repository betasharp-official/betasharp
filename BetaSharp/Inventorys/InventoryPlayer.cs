using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.NBT;

namespace BetaSharp.Inventorys;

public class InventoryPlayer : IInventory
{

    public ItemStack[] main = new ItemStack[36];
    public ItemStack[] armor = new ItemStack[4];
    public int selectedSlot;
    public EntityPlayer player;
    private ItemStack? cursorStack;
    public bool dirty;

    public InventoryPlayer(EntityPlayer player)
    {
        this.player = player;
    }

    public static int getHotbarSize()
    {
        return 9;
    }

    public ItemStack getSelectedItem()
    {
        return selectedSlot < 9 && selectedSlot >= 0 ? main[selectedSlot] : null;
    }

    private int getInventorySlotContainItem(int itemId)
    {
        for (int slotIndex = 0; slotIndex < main.Length; ++slotIndex)
        {
            if (main[slotIndex] != null && main[slotIndex].itemId == itemId)
            {
                return slotIndex;
            }
        }

        return -1;
    }

    private int storeItemStack(ItemStack itemStack)
    {
        for (int slotIndex = 0; slotIndex < main.Length; ++slotIndex)
        {
            if (main[slotIndex] != null && main[slotIndex].itemId == itemStack.itemId && main[slotIndex].isStackable() && main[slotIndex].count < main[slotIndex].getMaxCount() && main[slotIndex].count < getMaxCountPerStack() && (!main[slotIndex].getHasSubtypes() || main[slotIndex].getDamage() == itemStack.getDamage()))
            {
                return slotIndex;
            }
        }

        return -1;
    }

    private int getFirstEmptyStack()
    {
        for (int slotIndex = 0; slotIndex < main.Length; ++slotIndex)
        {
            if (main[slotIndex] == null)
            {
                return slotIndex;
            }
        }

        return -1;
    }

    public void setCurrentItem(int itemId, bool var2)
    {
        int slotIndex = getInventorySlotContainItem(itemId);
        if (slotIndex >= 0 && slotIndex < 9)
        {
            selectedSlot = slotIndex;
        }
    }

    public void changeCurrentItem(int scrollDirection)
    {
        if (scrollDirection > 0)
        {
            scrollDirection = 1;
        }

        if (scrollDirection < 0)
        {
            scrollDirection = -1;
        }

        for (selectedSlot -= scrollDirection; selectedSlot < 0; selectedSlot += 9)
        {
        }

        while (selectedSlot >= 9)
        {
            selectedSlot -= 9;
        }

    }

    private int storePartialItemStack(ItemStack itemStack)
    {
        int itemId = itemStack.itemId;
        int remainingCount = itemStack.count;
        int slotIndex = storeItemStack(itemStack);
        if (slotIndex < 0)
        {
            slotIndex = getFirstEmptyStack();
        }

        if (slotIndex < 0)
        {
            return remainingCount;
        }
        else
        {
            if (main[slotIndex] == null)
            {
                main[slotIndex] = new ItemStack(itemId, 0, itemStack.getDamage());
            }

            int spaceAvailable = remainingCount;
            if (remainingCount > main[slotIndex].getMaxCount() - main[slotIndex].count)
            {
                spaceAvailable = main[slotIndex].getMaxCount() - main[slotIndex].count;
            }

            if (spaceAvailable > getMaxCountPerStack() - main[slotIndex].count)
            {
                spaceAvailable = getMaxCountPerStack() - main[slotIndex].count;
            }

            if (spaceAvailable == 0)
            {
                return remainingCount;
            }
            else
            {
                remainingCount -= spaceAvailable;
                main[slotIndex].count += spaceAvailable;
                main[slotIndex].bobbingAnimationTime = 5;
                return remainingCount;
            }
        }
    }

    public void inventoryTick()
    {
        for (int slotIndex = 0; slotIndex < main.Length; ++slotIndex)
        {
            if (main[slotIndex] != null)
            {
                main[slotIndex].inventoryTick(player.world, player, slotIndex, selectedSlot == slotIndex);
            }
        }

    }

    public bool consumeInventoryItem(int itemId)
    {
        int slotIndex = getInventorySlotContainItem(itemId);
        if (slotIndex < 0)
        {
            return false;
        }
        else
        {
            if (--main[slotIndex].count <= 0)
            {
                main[slotIndex] = null;
            }

            return true;
        }
    }

    public bool addItemStackToInventory(ItemStack itemStack)
    {
        int slotIndex;
        if (itemStack.isDamaged())
        {
            slotIndex = getFirstEmptyStack();
            if (slotIndex >= 0)
            {
                main[slotIndex] = ItemStack.clone(itemStack);
                main[slotIndex].bobbingAnimationTime = 5;
                itemStack.count = 0;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            do
            {
                slotIndex = itemStack.count;
                itemStack.count = storePartialItemStack(itemStack);
            } while (itemStack.count > 0 && itemStack.count < slotIndex);

            return itemStack.count < slotIndex;
        }
    }

    public ItemStack removeStack(int slotIndex, int amount)
    {
        ItemStack[] targetArray = main;
        if (slotIndex >= main.Length)
        {
            targetArray = armor;
            slotIndex -= main.Length;
        }

        if (targetArray[slotIndex] != null)
        {
            ItemStack removeStack;
            if (targetArray[slotIndex].count <= amount)
            {
                removeStack = targetArray[slotIndex];
                targetArray[slotIndex] = null;
                return removeStack;
            }
            else
            {
                removeStack = targetArray[slotIndex].split(amount);
                if (targetArray[slotIndex].count == 0)
                {
                    targetArray[slotIndex] = null;
                }

                return removeStack;
            }
        }
        else
        {
            return null;
        }
    }

    public void setStack(int slotIndex, ItemStack? itemStack)
    {
        ItemStack[] targetArray = main;
        if (slotIndex >= targetArray.Length)
        {
            slotIndex -= targetArray.Length;
            targetArray = armor;
        }

        targetArray[slotIndex] = itemStack;
    }

    public float getStrVsBlock(Block block)
    {
        float miningSpeed = 1.0F;
        if (main[selectedSlot] != null)
        {
            miningSpeed *= main[selectedSlot].getMiningSpeedMultiplier(block);
        }

        return miningSpeed;
    }

    public NBTTagList writeToNBT(NBTTagList nbt)
    {
        int slotIndex;
        NBTTagCompound itemTag;
        for (slotIndex = 0; slotIndex < main.Length; ++slotIndex)
        {
            if (main[slotIndex] != null)
            {
                itemTag = new NBTTagCompound();
                itemTag.SetByte("Slot", (sbyte)slotIndex);
                main[slotIndex].writeToNBT(itemTag);
                nbt.SetTag(itemTag);
            }
        }

        for (slotIndex = 0; slotIndex < armor.Length; ++slotIndex)
        {
            if (armor[slotIndex] != null)
            {
                itemTag = new NBTTagCompound();
                itemTag.SetByte("Slot", (sbyte)(slotIndex + 100));
                armor[slotIndex].writeToNBT(itemTag);
                nbt.SetTag(itemTag);
            }
        }

        return nbt;
    }

    public void readFromNBT(NBTTagList nbt)
    {
        main = new ItemStack[36];
        armor = new ItemStack[4];

        for (int i = 0; i < nbt.TagCount(); ++i)
        {
            NBTTagCompound itemTag = (NBTTagCompound)nbt.TagAt(i);
            int slotIndex = itemTag.GetByte("Slot") & 255;
            ItemStack itemStack = new ItemStack(itemTag);
            if (itemStack.getItem() != null)
            {
                if (slotIndex >= 0 && slotIndex < main.Length)
                {
                    main[slotIndex] = itemStack;
                }

                if (slotIndex >= 100 && slotIndex < armor.Length + 100)
                {
                    armor[slotIndex - 100] = itemStack;
                }
            }
        }

    }

    public int size()
    {
        return main.Length + 4;
    }

    public ItemStack getStack(int slotIndex)
    {
        ItemStack[] targetArray = main;
        if (slotIndex >= targetArray.Length)
        {
            slotIndex -= targetArray.Length;
            targetArray = armor;
        }

        return targetArray[slotIndex];
    }

    public string getName()
    {
        return "Inventory";
    }

    public int getMaxCountPerStack()
    {
        return 64;
    }

    public int getDamageVsEntity(Entity entity)
    {
        ItemStack itemStack = getStack(selectedSlot);
        return itemStack != null ? itemStack.getAttackDamage(entity) : 1;
    }

    public bool canHarvestBlock(Block block)
    {
        if (block.material.IsHandHarvestable)
        {
            return true;
        }
        else
        {
            ItemStack itemStack = getStack(selectedSlot);
            return itemStack != null ? itemStack.isSuitableFor(block) : false;
        }
    }

    public ItemStack armorItemInSlot(int slotIndex)
    {
        return armor[slotIndex];
    }

    public int getTotalArmorValue()
    {
        int totalArmor = 0;
        int durabilitySum = 0;
        int totalMaxDurability = 0;

        for (int slotIndex = 0; slotIndex < armor.Length; ++slotIndex)
        {
            if (armor[slotIndex] != null && armor[slotIndex].getItem() is ItemArmor)
            {
                int maxDurability = armor[slotIndex].getMaxDamage();
                int pieceDamage = armor[slotIndex].getDamage2();
                int remainingDurability = maxDurability - pieceDamage;
                durabilitySum += remainingDurability;
                totalMaxDurability += maxDurability;
                int armorValue = ((ItemArmor)armor[slotIndex].getItem()).damageReduceAmount;
                totalArmor += armorValue;
            }
        }

        if (totalMaxDurability == 0)
        {
            return 0;
        }
        else
        {
            return (totalArmor - 1) * durabilitySum / totalMaxDurability + 1;
        }
    }

    public void damageArmor(int durabilityLoss)
    {
        for (int slotIndex = 0; slotIndex < armor.Length; ++slotIndex)
        {
            if (armor[slotIndex] != null && armor[slotIndex].getItem() is ItemArmor)
            {
                armor[slotIndex].damageItem(durabilityLoss, player);
                if (armor[slotIndex].count == 0)
                {
                    armor[slotIndex].onRemoved(player);
                    armor[slotIndex] = null;
                }
            }
        }

    }

    public void dropInventory()
    {
        int slotIndex;
        for (slotIndex = 0; slotIndex < main.Length; ++slotIndex)
        {
            if (main[slotIndex] != null)
            {
                player.dropItem(main[slotIndex], true);
                main[slotIndex] = null;
            }
        }

        for (slotIndex = 0; slotIndex < armor.Length; ++slotIndex)
        {
            if (armor[slotIndex] != null)
            {
                player.dropItem(armor[slotIndex], true);
                armor[slotIndex] = null;
            }
        }

    }

    public void markDirty()
    {
        dirty = true;
    }

    public void setItemStack(ItemStack? itemStack)
    {
        cursorStack = itemStack;
        player.onCursorStackChanged(itemStack);
    }

    public ItemStack getCursorStack()
    {
        return cursorStack;
    }

    public bool canPlayerUse(EntityPlayer entityPlayer)
    {
        return player.dead ? false : entityPlayer.getSquaredDistance(player) <= 64.0D;
    }

    public bool contains(ItemStack itemStack)
    {
        int slotIndex;
        for (slotIndex = 0; slotIndex < armor.Length; ++slotIndex)
        {
            if (armor[slotIndex] != null && armor[slotIndex].Equals(itemStack))
            {
                return true;
            }
        }

        for (slotIndex = 0; slotIndex < main.Length; ++slotIndex)
        {
            if (main[slotIndex] != null && main[slotIndex].Equals(itemStack))
            {
                return true;
            }
        }

        return false;
    }
}
