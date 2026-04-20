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
        if (blockId == Block.Leaves.ID || blockId == Block.Cobweb.ID)
        {
            itemStack.DamageItem(1, entityLiving);
        }

        return base.postMine(itemStack, blockId, x, y, z, entityLiving);
    }

    public override bool isSuitableFor(Block block)
    {
        return block.ID == Block.Cobweb.ID;
    }

    public override float getMiningSpeedMultiplier(ItemStack itemStack, Block block)
    {
        return block.ID != Block.Cobweb.ID && block.ID != Block.Leaves.ID ? (block.ID == Block.Wool.ID ? 5.0F : base.getMiningSpeedMultiplier(itemStack, block)) : 15.0F;
    }
}
