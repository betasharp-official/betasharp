using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemSlab : ItemBlock
{

    public ItemSlab(int id) : base(id)
    {
        setMaxDamage(0);
        setHasSubtypes(true);
    }

    public override int getTextureId(int meta)
    {
        return Block.Slab.GetTexture(2.ToSide(), meta);
    }

    public override int getPlacementMetadata(int meta)
    {
        return meta;
    }

    public override String getItemNameIS(ItemStack itemStack)
    {
        if (BlockSlab.Names.Length > itemStack.getDamage())
            return base.getItemName() + "." + BlockSlab.Names[itemStack.getDamage()];

        return "";
    }
}
