namespace BetaSharp.Blocks;

internal class BlockObsidian(int id, int textureId) : BlockStone(id, textureId)
{
    public override int GetDroppedItemCount() => 1;

    public override int GetDroppedItemId(int blockMeta) => Obsidian.ID;
}
