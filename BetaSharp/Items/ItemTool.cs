using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Items;

internal class ItemTool : Item
{

    private Block[] blocksEffectiveAgainst;
    private float efficiencyOnProperMaterial = 4.0F;
    private int damageVsEntity;
    protected EnumToolMaterial toolMaterial;

    protected ItemTool(int id, int baseDamage, EnumToolMaterial enumToolMaterial, Block[] blocksEffectiveAgainst) : base(id)
    {
        toolMaterial = enumToolMaterial;
        this.blocksEffectiveAgainst = blocksEffectiveAgainst;
        maxCount = 1;
        setMaxDamage(enumToolMaterial.getMaxUses());
        efficiencyOnProperMaterial = enumToolMaterial.getEfficiencyOnProperMaterial();
        damageVsEntity = baseDamage + enumToolMaterial.getDamageVsEntity();
    }

    public override float getMiningSpeedMultiplier(ItemStack itemStack, Block block)
    {
        for (int i = 0; i < blocksEffectiveAgainst.Length; ++i)
        {
            if (blocksEffectiveAgainst[i] == block)
            {
                return efficiencyOnProperMaterial;
            }
        }

        return 1.0F;
    }

    public override bool postHit(ItemStack itemStack, EntityLiving a, EntityPlayer b)
    {
        itemStack.damageItem(2, b);
        return true;
    }

    public override bool postMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving)
    {
        itemStack.damageItem(1, entityLiving);
        return true;
    }

    public override int getAttackDamage(Entity entity)
    {
        return damageVsEntity;
    }

    public override bool isHandheld()
    {
        return true;
    }
}
