using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Items;

internal class ItemTool : Item
{

    private Block[] blocksEffectiveAgainst;
    private float efficiencyOnProperMaterial = 4.0F;
    private int damageVsEntity;
    protected ToolMaterial toolMaterial;

    protected ItemTool(int id, int baseDamage, ToolMaterial toolMaterial, Block[] blocksEffectiveAgainst) : base(id)
    {
        this.toolMaterial = toolMaterial;
        this.blocksEffectiveAgainst = blocksEffectiveAgainst;
        maxCount = 1;
        setMaxDamage(toolMaterial.getMaxUses());
        efficiencyOnProperMaterial = toolMaterial.getEfficiencyOnProperMaterial();
        damageVsEntity = baseDamage + toolMaterial.getDamageVsEntity();
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
        itemStack.DamageItem(2, b);
        return true;
    }

    public override bool postMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving)
    {
        itemStack.DamageItem(1, entityLiving);
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
