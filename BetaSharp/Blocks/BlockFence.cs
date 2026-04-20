using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockFence(int id, int texture) : Block(id, texture, Material.Wood)
{
    public override bool CanPlaceAt(CanPlaceAtContext context) => context.World.Reader.GetBlockId(context.X, context.Y - 1, context.Z) == ID ||
                                                                  (context.World.Reader.GetMaterial(context.X, context.Y - 1, context.Z).IsSolid &&
                                                                   base.CanPlaceAt(context));

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => new Box(x, y, z, x + 1, y + 1.5F, z + 1);

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Fence;
}
