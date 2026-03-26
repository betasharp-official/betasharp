using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockGravel : BlockSand
{
    public BlockGravel(int i, int j) : base(i, j)
    {
    }

    public override int getDroppedItemId(int blockMeta) => Random.Shared.Next(10) == 0 ? Item.Flint.id : id;
}
