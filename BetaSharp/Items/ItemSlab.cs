using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemSlab : ItemBlock
{
    public ItemSlab(int id) : base(id)
    {
        SetMaxDamage(0);
        SetHasSubtypes(true);
    }

    public override int GetTextureId(int meta) => Block.Slab.GetTexture(2.ToSide(), meta);

    protected override int GetPlacementMetadata(int meta) => meta;

    public override string GetItemNameIS(ItemStack itemStack)
    {
        return BlockSlab.Names.Length > itemStack.GetDamage() ? $"{base.GetItemName()}.{BlockSlab.Names[itemStack.GetDamage()]}" : "";
    }
}
