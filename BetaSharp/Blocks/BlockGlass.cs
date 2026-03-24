using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockGlass : BlockBreakable
{
    public BlockGlass(int id, int texture, Material material, bool bl) : base(id, texture, material, bl)
    {
    }

    public override int getDroppedItemCount() => 0;

    public override int getRenderLayer() => 0;
}
