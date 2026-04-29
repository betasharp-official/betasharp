using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemLog : ItemBlock
{
    public ItemLog(int id) : base(id)
    {
        SetMaxDamage(0);
        SetHasSubtypes(true);
    }

    public override int GetTextureId(int meta) => Block.Log.GetTexture(2.ToSide(), meta);

    protected override int GetPlacementMetadata(int meta) => meta;
}
