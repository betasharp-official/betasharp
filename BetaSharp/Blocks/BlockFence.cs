using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockFence(int id, int texture) : Block(id, texture, Material.Wood)
{
    public override bool canPlaceAt(CanPlaceAtContext context) => context.World.Reader.GetBlockId(context.X, context.Y - 1, context.Z) == id ||
                                                                  context.World.Reader.GetMaterial(context.X, context.Y - 1, context.Z).IsSolid &&
                                                                  base.canPlaceAt(context);

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => new Box(x, y, z, x + 1, y + 1.5F, z + 1);

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Fence;
}
