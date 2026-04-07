using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockGravel(int i, int j) : BlockSand(i, j)
{
    public override int getDroppedItemId(int blockMeta) => Random.Shared.Next(10) == 0 ? Item.Flint.id : id;
}
