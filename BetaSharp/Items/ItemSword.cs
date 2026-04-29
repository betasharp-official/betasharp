using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Items;

internal class ItemSword : Item
{
    private readonly int _weaponDamage;

    public ItemSword(int id, ToolMaterial toolMaterial) : base(id)
    {
        MaxCount = 1;
        SetMaxDamage(toolMaterial.getMaxUses());
        _weaponDamage = 4 + toolMaterial.getDamageVsEntity() * 2;
    }

    public override float GetMiningSpeedMultiplier(ItemStack itemStack, Block block) => block.id == Block.Cobweb.id ? 15.0F : 1.5F;

    public override bool PostHit(ItemStack itemStack, EntityLiving a, EntityPlayer entityPlayer)
    {
        itemStack.DamageItem(1, entityPlayer);
        return true;
    }

    public override bool PostMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving)
    {
        itemStack.DamageItem(2, entityLiving);
        return true;
    }

    public override int GetAttackDamage(Entity entity) => _weaponDamage;

    public override bool IsHandheld() => true;

    public override bool IsSuitableFor(Block block) => block.id == Block.Cobweb.id;
}
