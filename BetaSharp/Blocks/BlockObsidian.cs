namespace BetaSharp.Blocks;

internal class BlockObsidian : BlockStone
{
    public BlockObsidian(int id, int textureId) : base(id, textureId)
    {
    }

    public override int getDroppedItemCount()
    {
        return 1;
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return Block.Obsidian.id;
    }
}
