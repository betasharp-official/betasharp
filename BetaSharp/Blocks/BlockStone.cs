using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockStone(int id, int textureId) : Block(id, textureId, Material.Stone)
{
    public override int GetDroppedItemId(int blockMeta) => Cobblestone.ID;
}
