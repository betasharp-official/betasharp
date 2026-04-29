using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

public class ItemStack
{
    private int _damage;
    public int AnimationTime;
    public int Count;
    public int ItemId;

    public ItemStack(Block block) : this((Block)block, 1)
    {
    }

    public ItemStack(Block block, int count) : this(block.id, count, 0)
    {
    }

    public ItemStack(Item item) : this(item.Id, 1, 0)
    {
    }

    public ItemStack(Item item, int count) : this(item.Id, count, 0)
    {
    }

    public ItemStack(Item item, int count, int damage) : this(item.Id, count, damage)
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
        ReadFromNbt(nbt);
    }

    public ItemStack Split(int splitAmount)
    {
        Count -= splitAmount;
        return new ItemStack(ItemId, splitAmount, _damage);
    }

    public Item GetItem() => Item.ITEMS[ItemId];

    public int GetTextureId() => GetItem().GetTextureId(this);

    public bool UseOnBlock(EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        bool item = GetItem().UseOnBlock(this, entityPlayer, world, x, y, z, meta);
        if (item)
        {
            entityPlayer.increaseStat(Stats.Stats.Used[ItemId], 1);
        }

        return item;
    }

    public float GetMiningSpeedMultiplier(Block block) => GetItem().GetMiningSpeedMultiplier(this, block);

    public ItemStack Use(IWorldContext world, EntityPlayer entityPlayer) => GetItem().Use(this, world, entityPlayer);

    public NBTTagCompound WriteToNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("id", (short)ItemId);
        nbt.SetByte("Count", (sbyte)Count);
        nbt.SetShort("Damage", (short)_damage);
        return nbt;
    }

    public void ReadFromNbt(NBTTagCompound nbt)
    {
        ItemId = nbt.GetShort("id");
        Count = nbt.GetByte("Count");
        _damage = nbt.GetShort("Damage");
    }

    public int GetMaxCount() => GetItem().GetMaxCount();

    public bool IsStackable() => GetMaxCount() > 1 && (!IsDamageable() || !IsDamaged());

    public bool IsDamageable() => Item.ITEMS[ItemId].GetMaxDamage() > 0;

    public bool GetHasSubtypes() => Item.ITEMS[ItemId].GetHasSubtypes();

    public bool IsDamaged() => IsDamageable() && _damage > 0;

    public int GetDamage2() => _damage;

    public int GetDamage() => _damage;

    public void SetDamage(int damage) => _damage = damage;

    public int GetMaxDamage() => Item.ITEMS[ItemId].GetMaxDamage();

    public void ConsumeItem(EntityPlayer player)
    {
        if (!player.GameMode.FiniteResources)
        {
            return;
        }

        Count--;
    }

    public void DamageItem(int damageAmount, Entity entity)
    {
        if (!IsDamageable())
        {
            return;
        }

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
        if (!IsDamageable())
        {
            return;
        }

        DamageItemForced(damageAmount, player);
    }

    private void DamageItemForced(int damageAmount, EntityPlayer player)
    {
        if (!player.GameMode.FiniteResources)
        {
            return;
        }

        _damage += damageAmount;
        if (UpdateBroken())
        {
            player.increaseStat(Stats.Stats.Broken[ItemId], 1);
        }
    }

    private bool UpdateBroken()
    {
        if (_damage <= GetMaxDamage())
        {
            return false;
        }

        --Count;
        if (Count < 0)
        {
            Count = 0;
        }

        _damage = 0;
        return true;

    }

    public void PostHit(EntityLiving entityLiving, EntityPlayer entityPlayer)
    {
        bool hit = Item.ITEMS[ItemId].PostHit(this, entityLiving, entityPlayer);
        if (hit)
        {
            entityPlayer.increaseStat(Stats.Stats.Used[ItemId], 1);
        }
    }

    public void PostMine(int blockId, int x, int y, int z, EntityPlayer entityPlayer)
    {
        bool mined = Item.ITEMS[ItemId].PostMine(this, blockId, x, y, z, entityPlayer);
        if (mined)
        {
            entityPlayer.increaseStat(Stats.Stats.Used[ItemId], 1);
        }
    }

    public int GetAttackDamage(Entity entity) => Item.ITEMS[ItemId].GetAttackDamage(entity);

    public bool IsSuitableFor(Block block) => Item.ITEMS[ItemId].IsSuitableFor(block);

    public static void OnRemoved(EntityPlayer entityPlayer)
    {
    }

    public void UseOnEntity(EntityLiving entityLiving, EntityPlayer entityPlayer) => Item.ITEMS[ItemId].UseOnEntity(this, entityLiving, entityPlayer);

    public ItemStack Copy() => new(ItemId, Count, _damage);

    public static bool AreEqual(ItemStack? a, ItemStack? b) => a == null && b == null ? true : a != null && b != null ? a.Equals2(b) : false;

    private bool Equals2(ItemStack itemStack) => Count != itemStack.Count ? false : ItemId != itemStack.ItemId ? false : _damage == itemStack._damage;

    public bool IsItemEqual(ItemStack itemStack) => ItemId == itemStack.ItemId && _damage == itemStack._damage;

    public string GetItemName() => Item.ITEMS[ItemId].GetItemNameIS(this);

    public static ItemStack Clone(ItemStack itemStack) => itemStack == null ? null : itemStack.Copy();

    public override string ToString() => $"{Count}x{Item.ITEMS[ItemId].GetItemName()}@{_damage}";

    public void InventoryTick(IWorldContext world, Entity entity, int slotIndex, bool shouldUpdate)
    {
        if (AnimationTime > 0)
        {
            --AnimationTime;
        }

        Item.ITEMS[ItemId].InventoryTick(this, world, entity, slotIndex, shouldUpdate);
    }

    public void OnCraft(IWorldContext world, EntityPlayer entityPlayer)
    {
        entityPlayer.increaseStat(Stats.Stats.Crafted[ItemId], Count);
        Item.ITEMS[ItemId].OnCraft(this, world, entityPlayer);
    }

    public bool Equals(ItemStack itemStack) => ItemId == itemStack.ItemId && Count == itemStack.Count && _damage == itemStack._damage;
}
