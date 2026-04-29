using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Items;

public class ItemShears : Item
{
    public ItemShears(int id) : base(id)
    {
        SetMaxCount(1);
        SetMaxDamage(238);
    }

    public override bool PostMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving)
    {
        if (blockId == Block.Leaves.id || blockId == Block.Cobweb.id)
        {
            itemStack.DamageItem(1, entityLiving);
        }

        return base.PostMine(itemStack, blockId, x, y, z, entityLiving);
    }

    public override bool IsSuitableFor(Block block) => block.id == Block.Cobweb.id;

    public override float GetMiningSpeedMultiplier(ItemStack itemStack, Block block) => block.id != Block.Cobweb.id && block.id != Block.Leaves.id ? block.id == Block.Wool.id ? 5.0F : base.GetMiningSpeedMultiplier(itemStack, block) : 15.0F;
}
