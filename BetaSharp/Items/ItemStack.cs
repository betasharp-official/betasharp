using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

public class ItemStack
{
    public int Count;
    public int AnimationTime;
    public int ItemId;
    private int _damage;

    public ItemStack(Block block) : this((Block)block, 1)
    {
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
        Count = 0;
        ItemId = itemId;
        Count = count;
        _damage = damage;
    }

    public ItemStack(NBTTagCompound nbt)
    {
        Count = 0;
        readFromNBT(nbt);
    }

    public ItemStack Split(int splitAmount)
    {
        Count -= splitAmount;
        return new ItemStack(ItemId, splitAmount, _damage);
    }

    public Item getItem()
    {
        return Item.ITEMS[ItemId];
    }

    public int getTextureId()
    {
        return getItem().getTextureId(this);
    }

    public bool useOnBlock(EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        bool item = getItem().useOnBlock(this, entityPlayer, world, x, y, z, meta);
        if (item)
        {
            entityPlayer.IncreaseStat(Stats.Stats.Used[ItemId], 1);
        }

        return item;
    }

    public float getMiningSpeedMultiplier(Block block)
    {
        return getItem().getMiningSpeedMultiplier(this, block);
    }

    public ItemStack use(IWorldContext world, EntityPlayer entityPlayer)
    {
        return getItem().use(this, world, entityPlayer);
    }

    public NBTTagCompound writeToNBT(NBTTagCompound nbt)
    {
        nbt.SetShort("id", (short)ItemId);
        nbt.SetByte("Count", (sbyte)Count);
        nbt.SetShort("Damage", (short)_damage);
        return nbt;
    }

    public void readFromNBT(NBTTagCompound nbt)
    {
        ItemId = nbt.GetShort("id");
        Count = nbt.GetByte("Count");
        _damage = nbt.GetShort("Damage");
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
        return Item.ITEMS[ItemId].getMaxDamage() > 0;
    }

    public bool getHasSubtypes()
    {
        return Item.ITEMS[ItemId].getHasSubtypes();
    }

    public bool isDamaged()
    {
        return isDamageable() && _damage > 0;
    }

    public int getDamage2()
    {
        return _damage;
    }

    public int getDamage()
    {
        return _damage;
    }

    public void setDamage(int damage)
    {
        _damage = damage;
    }

    public int getMaxDamage()
    {
        return Item.ITEMS[ItemId].getMaxDamage();
    }

    public void ConsumeItem(EntityPlayer player)
    {
        if (!player.GameMode.FiniteResources) return;
        Count--;
    }

    public void DamageItem(int damageAmount, Entity entity)
    {
        if (!isDamageable()) return;

        if (entity is EntityPlayer player)
        {
            DamageItemForced(damageAmount, player);
        }
        else
        {
            _damage += damageAmount;
            UpdateBroken();
        }
    }

    public void DamageItem(int damageAmount, EntityPlayer player)
    {
        if (!isDamageable()) return;
        DamageItemForced(damageAmount, player);
    }

    private void DamageItemForced(int damageAmount, EntityPlayer player)
    {
        if (!player.GameMode.FiniteResources) return;

        _damage += damageAmount;
        if (UpdateBroken())
        {
            player.IncreaseStat(Stats.Stats.Broken[ItemId], 1);
        }
    }

    private bool UpdateBroken()
    {
        if (_damage > getMaxDamage())
        {
            --Count;
            if (Count < 0) Count = 0;
            _damage = 0;
            return true;
        }

        return false;
    }

    public void postHit(EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
        bool hit = Item.ITEMS[ItemId].postHit(this, entityLiving, entityPlayer);
        if (hit)
        {
            entityPlayer.IncreaseStat(Stats.Stats.Used[ItemId], 1);
        }

    }

    public void postMine(int blockId, int x, int y, int z, EntityPlayer entityPlayer)
    {
        bool mined = Item.ITEMS[ItemId].postMine(this, blockId, x, y, z, entityPlayer);
        if (mined)
        {
            entityPlayer.IncreaseStat(Stats.Stats.Used[ItemId], 1);
        }

    }

    public int getAttackDamage(Entity entity)
    {
        return Item.ITEMS[ItemId].getAttackDamage(entity);
    }

    public bool isSuitableFor(Block block)
    {
        return Item.ITEMS[ItemId].isSuitableFor(block);
    }

    public static void onRemoved(EntityPlayer entityPlayer)
    {
    }

    public void useOnEntity(EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
        Item.ITEMS[ItemId].useOnEntity(this, entityLiving, entityPlayer);
    }

    public ItemStack copy()
    {
        return new ItemStack(ItemId, Count, _damage);
    }

    public static bool areEqual(ItemStack? a, ItemStack? b)
    {
        return a == null && b == null ? true : (a != null && b != null ? a.equals2(b) : false);
    }

    private bool equals2(ItemStack itemStack)
    {
        return Count != itemStack.Count ? false : (ItemId != itemStack.ItemId ? false : _damage == itemStack._damage);
    }

    public bool isItemEqual(ItemStack itemStack)
    {
        return ItemId == itemStack.ItemId && _damage == itemStack._damage;
    }

    public string getItemName()
    {
        return Item.ITEMS[ItemId].getItemNameIS(this);
    }

    public static ItemStack clone(ItemStack itemStack)
    {
        return itemStack == null ? null : itemStack.copy();
    }

    public override string ToString()
    {
        return Count + "x" + Item.ITEMS[ItemId].getItemName() + "@" + _damage;
    }

    public void inventoryTick(IWorldContext world, Entity entity, int slotIndex, bool shouldUpdate)
    {
        if (AnimationTime > 0)
        {
            --AnimationTime;
        }

        Item.ITEMS[ItemId].inventoryTick(this, world, entity, slotIndex, shouldUpdate);
    }

    public void onCraft(IWorldContext world, EntityPlayer entityPlayer)
    {
        entityPlayer.IncreaseStat(Stats.Stats.Crafted[ItemId], Count);
        Item.ITEMS[ItemId].onCraft(this, world, entityPlayer);
    }

    public bool Equals(ItemStack itemStack)
    {
        return ItemId == itemStack.ItemId && Count == itemStack.Count && _damage == itemStack._damage;
    }
}
