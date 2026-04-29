using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Items;

internal class ItemTool : Item
{
    private readonly Block[] _blocksEffectiveAgainst;
    private readonly int _damageVsEntity;
    private readonly float _efficiencyOnProperMaterial;
    protected readonly ToolMaterial ToolMaterial;

    protected ItemTool(int id, int baseDamage, ToolMaterial toolMaterial, Block[] blocksEffectiveAgainst) : base(id)
    {
        ToolMaterial = toolMaterial;
        _blocksEffectiveAgainst = blocksEffectiveAgainst;
        MaxCount = 1;
        SetMaxDamage(toolMaterial.getMaxUses());
        _efficiencyOnProperMaterial = toolMaterial.getEfficiencyOnProperMaterial();
        _damageVsEntity = baseDamage + toolMaterial.getDamageVsEntity();
    }

    public override float GetMiningSpeedMultiplier(ItemStack itemStack, Block block)
    {
        foreach (var t in _blocksEffectiveAgainst)
        {
            if (t == block)
            {
                return _efficiencyOnProperMaterial;
            }
        }

        return 1.0F;
    }

    public override bool PostHit(ItemStack itemStack, EntityLiving a, EntityPlayer b)
    {
        itemStack.DamageItem(2, b);
        return true;
    }

    public override bool PostMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving)
    {
        itemStack.DamageItem(1, entityLiving);
        return true;
    }

    public override int GetAttackDamage(Entity entity) => _damageVsEntity;

    public override bool IsHandheld() => true;
}
