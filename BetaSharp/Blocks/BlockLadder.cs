using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockLadder : Block
{

    public BlockLadder(int id, int textureId) : base(id, textureId, Material.PistonBreakable)
    {
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        int meta = world.getBlockMeta(x, y, z);
        float thickness = 2.0F / 16.0F;
        if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
        }

        if (meta == 4)
        {
            setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 5)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
        }

        return base.getCollisionShape(world, x, y, z);
    }

    public override Box getBoundingBox(World world, int x, int y, int z)
    {
        int meta = world.getBlockMeta(x, y, z);
        float thickness = 2.0F / 16.0F;
        if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
        }

        if (meta == 4)
        {
            setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 5)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
        }

        return base.getBoundingBox(world, x, y, z);
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
        return BlockRendererType.Ladder;
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return world.shouldSuffocate(x - 1, y, z) ? true : (world.shouldSuffocate(x + 1, y, z) ? true : (world.shouldSuffocate(x, y, z - 1) ? true : world.shouldSuffocate(x, y, z + 1)));
    }

    public override void onPlaced(World world, int x, int y, int z, int direction)
    {
        int meta = world.getBlockMeta(x, y, z);
        if ((meta == 0 || direction == 2) && world.shouldSuffocate(x, y, z + 1))
        {
            meta = 2;
        }

        if ((meta == 0 || direction == 3) && world.shouldSuffocate(x, y, z - 1))
        {
            meta = 3;
        }

        if ((meta == 0 || direction == 4) && world.shouldSuffocate(x + 1, y, z))
        {
            meta = 4;
        }

        if ((meta == 0 || direction == 5) && world.shouldSuffocate(x - 1, y, z))
        {
            meta = 5;
        }

        world.setBlockMeta(x, y, z, meta);
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        int meta = world.getBlockMeta(x, y, z);
        bool hasSupport = false;
        if (meta == 2 && world.shouldSuffocate(x, y, z + 1))
        {
            hasSupport = true;
        }

        if (meta == 3 && world.shouldSuffocate(x, y, z - 1))
        {
            hasSupport = true;
        }

        if (meta == 4 && world.shouldSuffocate(x + 1, y, z))
        {
            hasSupport = true;
        }

        if (meta == 5 && world.shouldSuffocate(x - 1, y, z))
        {
            hasSupport = true;
        }

        if (!hasSupport)
        {
            dropStacks(world, x, y, z, meta);
            world.setBlock(x, y, z, 0);
        }

        base.neighborUpdate(world, x, y, z, id);
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 1;
    }
}
