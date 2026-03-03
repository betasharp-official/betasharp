using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Worlds;

namespace BetaSharp.Items;

public class ItemStack
{
    public int count;
    public int bobbingAnimationTime;
    public int itemId;
    private int damage;

    public ItemStack(Block block) : this((Block)block, 1)
    {
    }

    public ItemStack(int id, int count)
    {
        itemId = id;
        this.count = count;
    }

    public ItemStack(Block block, int count) : this(block.id, count, 0)
    {
    }

    public ItemStack(Block block, int count, int damage) : this(block.id, count, damage)
    {
    }

    public ItemStack(Item item) : this(item.id, 1, 0)
    {
    }

    public ItemStack(Item item, int count) : this(item.id, count, 0)
    {
    }

    public ItemStack(Item item, int count, int damage) : this(item.id, count, damage)
    {
    }

    public ItemStack(int itemId, int count, int damage)
    {
        this.count = 0;
        this.itemId = itemId;
        this.count = count;
        this.damage = damage;
    }

    public ItemStack(NBTTagCompound nbt)
    {
        count = 0;
        readFromNBT(nbt);
    }

    public ItemStack split(int splitAmount)
    {
        count -= splitAmount;
        return new ItemStack(itemId, splitAmount, damage);
    }

    public Item getItem()
    {
        return Item.ITEMS[itemId];
    }

    public int getTextureId()
    {
        return getItem().getTextureId(this);
    }

    public bool useOnBlock(EntityPlayer entityPlayer, World world, int x, int y, int z, int meta)
    {
        bool item = getItem().useOnBlock(this, entityPlayer, world, x, y, z, meta);
        if (item)
        {
            entityPlayer.increaseStat(Stats.Stats.Used[itemId], 1);
        }

        return item;
    }

    public float getMiningSpeedMultiplier(Block block)
    {
        return getItem().getMiningSpeedMultiplier(this, block);
    }

    public ItemStack use(World world, EntityPlayer entityPlayer)
    {
        return getItem().use(this, world, entityPlayer);
    }

    public NBTTagCompound writeToNBT(NBTTagCompound nbt)
    {
        nbt.SetShort("id", (short)itemId);
        nbt.SetByte("Count", (sbyte)count);
        nbt.SetShort("Damage", (short)damage);
        return nbt;
    }

    public void readFromNBT(NBTTagCompound nbt)
    {
        itemId = nbt.GetShort("id");
        count = nbt.GetByte("Count");
        damage = nbt.GetShort("Damage");
    }

    public int getMaxCount()
    {
        return getItem().getMaxCount();
    }

    public bool isStackable()
    {
        return getMaxCount() > 1 && (!isDamageable() || !isDamaged());
    }

    public bool isDamageable()
    {
        return Item.ITEMS[itemId].getMaxDamage() > 0;
    }

    public bool getHasSubtypes()
    {
        return Item.ITEMS[itemId].getHasSubtypes();
    }

    public bool isDamaged()
    {
        return isDamageable() && damage > 0;
    }

    public int getDamage2()
    {
        return damage;
    }

    public int getDamage()
    {
        return damage;
    }

    public void setDamage(int damage)
    {
        this.damage = damage;
    }

    public int getMaxDamage()
    {
        return Item.ITEMS[itemId].getMaxDamage();
    }

    public void damageItem(int damageAmount, Entity entity)
    {
        if (isDamageable())
        {
            damage += damageAmount;
            if (damage > getMaxDamage())
            {
                if (entity is EntityPlayer)
                {
                    ((EntityPlayer)entity).increaseStat(Stats.Stats.Broken[itemId], 1);
                }

                --count;
                if (count < 0)
                {
                    count = 0;
                }

                damage = 0;
            }

        }
    }

    public void postHit(EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
        bool hit = Item.ITEMS[itemId].postHit(this, entityLiving, entityPlayer);
        if (hit)
        {
            entityPlayer.increaseStat(Stats.Stats.Used[itemId], 1);
        }

    }

    public void postMine(int blockId, int x, int y, int z, EntityPlayer entityPlayer)
    {
        bool mined = Item.ITEMS[itemId].postMine(this, blockId, x, y, z, entityPlayer);
        if (mined)
        {
            entityPlayer.increaseStat(Stats.Stats.Used[itemId], 1);
        }

    }

    public int getAttackDamage(Entity entity)
    {
        return Item.ITEMS[itemId].getAttackDamage(entity);
    }

    public bool isSuitableFor(Block block)
    {
        return Item.ITEMS[itemId].isSuitableFor(block);
    }

    public void onRemoved(EntityPlayer entityPlayer)
    {
    }

    public void useOnEntity(EntityLiving entityLiving)
    {
        Item.ITEMS[itemId].useOnEntity(this, entityLiving);
    }

    public ItemStack copy()
    {
        return new ItemStack(itemId, count, damage);
    }

    public static bool areEqual(ItemStack? a, ItemStack? b)
    {
        return a == null && b == null ? true : (a != null && b != null ? a.equals2(b) : false);
    }

    private bool equals2(ItemStack itemStack)
    {
        return count != itemStack.count ? false : (itemId != itemStack.itemId ? false : damage == itemStack.damage);
    }

    public bool isItemEqual(ItemStack itemStack)
    {
        return itemId == itemStack.itemId && damage == itemStack.damage;
    }

    public string getItemName()
    {
        return Item.ITEMS[itemId].getItemNameIS(this);
    }

    public static ItemStack clone(ItemStack itemStack)
    {
        return itemStack == null ? null : itemStack.copy();
    }

    public override string ToString()
    {
        return count + "x" + Item.ITEMS[itemId].getItemName() + "@" + damage;
    }

    public void inventoryTick(World world, Entity entity, int slotIndex, bool shouldUpdate)
    {
        if (bobbingAnimationTime > 0)
        {
            --bobbingAnimationTime;
        }

        Item.ITEMS[itemId].inventoryTick(this, world, entity, slotIndex, shouldUpdate);
    }

    public void onCraft(World world, EntityPlayer entityPlayer)
    {
        entityPlayer.increaseStat(Stats.Stats.Crafted[itemId], count);
        Item.ITEMS[itemId].onCraft(this, world, entityPlayer);
    }

    public bool Equals(ItemStack itemStack)
    {
        return itemId == itemStack.itemId && count == itemStack.count && damage == itemStack.damage;
    }
}
