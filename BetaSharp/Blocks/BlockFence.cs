using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockFence : Block
{
    public BlockFence(int id, int texture) : base(id, texture, Material.Wood)
    {
    }

    public override bool canPlaceAt(CanPlaceAtContext context)
    {
        return context.World.Reader.GetBlockId(context.X, context.Y - 1, context.Z) == id ? true : !context.World.Reader.GetMaterial(context.X, context.Y - 1, context.Z).IsSolid ? false : base.canPlaceAt(context);
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        return new Box(x, y, z, x + 1, y + 1.5F, z + 1);
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Fence;
    }
}
