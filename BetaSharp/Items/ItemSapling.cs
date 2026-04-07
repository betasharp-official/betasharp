using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemSapling : ItemBlock
{

    public ItemSapling(int id) : base(id)
    {
        setMaxDamage(0);
        setHasSubtypes(true);
    }

    public override int getPlacementMetadata(int meta)
    {
        return meta;
    }

    public override int getTextureId(int meta)
    {
        return Block.Sapling.GetTexture(0, meta);
    }
}
