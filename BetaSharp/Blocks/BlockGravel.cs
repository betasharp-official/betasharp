using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockGravel(int id, int textureIndex) : BlockSand(id, textureIndex)
{
    public override int GetDroppedItemId(int blockMeta) => Random.Shared.Next(10) == 0 ? Item.Flint.id : ID;
}
