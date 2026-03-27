using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockStone : Block
{
    public BlockStone(int id, int textureId) : base(id, textureId, Material.Stone)
    {
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return Block.Cobblestone.id;
    }
}
