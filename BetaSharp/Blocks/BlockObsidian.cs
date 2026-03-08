namespace BetaSharp.Blocks;

internal class BlockObsidian : BlockStone
{
    public BlockObsidian(int id, int textureId) : base(id, textureId)
    {
    }

    public override int getDroppedItemCount() => 1;

    public override int getDroppedItemId(int blockMeta) => Obsidian.id;
}
