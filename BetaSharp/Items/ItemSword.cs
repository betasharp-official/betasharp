using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Items;

internal class ItemSword : Item
{

    private int weaponDamage;

    public ItemSword(int id, EnumToolMaterial enumToolMaterial) : base(id)
    {
        maxCount = 1;
        setMaxDamage(enumToolMaterial.getMaxUses());
        weaponDamage = 4 + enumToolMaterial.getDamageVsEntity() * 2;
    }

    public override float getMiningSpeedMultiplier(ItemStack itemStack, Block block)
    {
        return block.id == Block.Cobweb.id ? 15.0F : 1.5F;
    }

    public override bool postHit(ItemStack itemStack, EntityLiving a, EntityPlayer entityPlayer)
    {
        itemStack.DamageItem(1, entityPlayer);
        return true;
    }

    public override bool postMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving)
    {
        itemStack.DamageItem(2, entityLiving);
        return true;
    }

    public override int getAttackDamage(Entity entity)
    {
        return weaponDamage;
    }

    public override bool isHandheld()
    {
        return true;
    }

    public override bool isSuitableFor(Block block)
    {
        return block.id == Block.Cobweb.id;
    }
}
