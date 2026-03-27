using BetaSharp.Blocks;
using BetaSharp.Entities;

namespace BetaSharp.Items;

public class ItemShears : Item
{

    public ItemShears(int id) : base(id)
    {
        setMaxCount(1);
        setMaxDamage(238);
    }

    public override bool postMine(ItemStack itemStack, int blockId, int x, int y, int z, EntityLiving entityLiving)
    {
        if (blockId == Block.Leaves.id || blockId == Block.Cobweb.id)
        {
            itemStack.DamageItem(1, entityLiving);
        }

        return base.postMine(itemStack, blockId, x, y, z, entityLiving);
    }

    public override bool isSuitableFor(Block block)
    {
        return block.id == Block.Cobweb.id;
    }

    public override float getMiningSpeedMultiplier(ItemStack itemStack, Block block)
    {
        return block.id != Block.Cobweb.id && block.id != Block.Leaves.id ? (block.id == Block.Wool.id ? 5.0F : base.getMiningSpeedMultiplier(itemStack, block)) : 15.0F;
    }
}
