namespace BetaSharp.Blocks;

internal class BlockObsidian(int id, int textureId) : BlockStone(id, textureId)
{
    public override int getDroppedItemCount() => 1;

    public override int getDroppedItemId(int blockMeta) => Obsidian.id;
}
