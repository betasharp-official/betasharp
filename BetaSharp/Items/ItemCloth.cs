using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemCloth : ItemBlock
{
    public ItemCloth(int id) : base(id)
    {
        SetMaxDamage(0);
        SetHasSubtypes(true);
    }

    public override int GetTextureId(int meta) => Block.Wool.GetTexture(2.ToSide(), BlockCloth.getBlockMeta(meta));

    protected override int GetPlacementMetadata(int meta) => meta;

    public override string GetItemNameIS(ItemStack itemStack) => $"{base.GetItemName()}.{ItemDye.DyeColorNames[BlockCloth.getBlockMeta(itemStack.GetDamage())]}";
}
