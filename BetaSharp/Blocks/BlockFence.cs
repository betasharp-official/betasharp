using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockFence : Block
{

    public BlockFence(int id, int texture) : base(id, texture, Material.Wood)
    {
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return world.getBlockId(x, y - 1, z) == id ? true : (!world.getMaterial(x, y - 1, z).IsSolid ? false : base.canPlaceAt(world, x, y, z));
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return new Box((double)x, (double)y, (double)z, (double)(x + 1), (double)((float)y + 1.5F), (double)(z + 1));
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
