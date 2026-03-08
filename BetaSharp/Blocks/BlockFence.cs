using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockFence : Block
{
    public BlockFence(int id, int texture) : base(id, texture, Material.Wood)
    {
    }

    public override bool canPlaceAt(OnPlacedEvt ctx) => ctx.WorldRead.GetBlockId(ctx.X, ctx.Y - 1, ctx.Z) == id ? true : !ctx.WorldRead.GetMaterial(ctx.X, ctx.Y - 1, ctx.Z).IsSolid ? false : base.canPlaceAt(ctx);

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => new Box(x, y, z, x + 1, y + 1.5F, z + 1);

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Fence;
}
