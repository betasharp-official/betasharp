using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockGlass(int id, int texture, Material material, bool bl) : BlockBreakable(id, texture, material, bl)
{
    public override int DroppedItemCount => 0;

    public override int GetRenderLayer() => 0;
}
