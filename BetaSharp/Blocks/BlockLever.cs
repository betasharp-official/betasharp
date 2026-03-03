using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockLever : Block
{

    public BlockLever(int id, int level) : base(id, level, Material.PistonBreakable)
    {
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return null;
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
        return BlockRendererType.Lever;
    }

    public override bool canPlaceAt(World world, int x, int y, int z, int side)
    {
        return side == 1 && world.shouldSuffocate(x, y - 1, z) ? true : (side == 2 && world.shouldSuffocate(x, y, z + 1) ? true : (side == 3 && world.shouldSuffocate(x, y, z - 1) ? true : (side == 4 && world.shouldSuffocate(x + 1, y, z) ? true : side == 5 && world.shouldSuffocate(x - 1, y, z))));
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return world.shouldSuffocate(x - 1, y, z) ? true : (world.shouldSuffocate(x + 1, y, z) ? true : (world.shouldSuffocate(x, y, z - 1) ? true : (world.shouldSuffocate(x, y, z + 1) ? true : world.shouldSuffocate(x, y - 1, z))));
    }

    public override void onPlaced(World world, int x, int y, int z, int direction)
    {
        int var6 = world.getBlockMeta(x, y, z);
        int var7 = var6 & 8;
        var6 &= 7;
        var6 = -1;
        if (direction == 1 && world.shouldSuffocate(x, y - 1, z))
        {
            var6 = 5 + world.random.NextInt(2);
        }

        if (direction == 2 && world.shouldSuffocate(x, y, z + 1))
        {
            var6 = 4;
        }

        if (direction == 3 && world.shouldSuffocate(x, y, z - 1))
        {
            var6 = 3;
        }

        if (direction == 4 && world.shouldSuffocate(x + 1, y, z))
        {
            var6 = 2;
        }

        if (direction == 5 && world.shouldSuffocate(x - 1, y, z))
        {
            var6 = 1;
        }

        if (var6 == -1)
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }
        else
        {
            world.setBlockMeta(x, y, z, var6 + var7);
        }
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (breakIfCannotPlaceAt(world, x, y, z))
        {
            int var6 = world.getBlockMeta(x, y, z) & 7;
            bool var7 = false;
            if (!world.shouldSuffocate(x - 1, y, z) && var6 == 1)
            {
                var7 = true;
            }

            if (!world.shouldSuffocate(x + 1, y, z) && var6 == 2)
            {
                var7 = true;
            }

            if (!world.shouldSuffocate(x, y, z - 1) && var6 == 3)
            {
                var7 = true;
            }

            if (!world.shouldSuffocate(x, y, z + 1) && var6 == 4)
            {
                var7 = true;
            }

            if (!world.shouldSuffocate(x, y - 1, z) && var6 == 5)
            {
                var7 = true;
            }

            if (!world.shouldSuffocate(x, y - 1, z) && var6 == 6)
            {
                var7 = true;
            }

            if (var7)
            {
                dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
                world.setBlock(x, y, z, 0);
            }
        }

    }

    private bool breakIfCannotPlaceAt(World world, int x, int y, int z)
    {
        if (!canPlaceAt(world, x, y, z))
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
            return false;
        }
        else
        {
            return true;
        }
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        int var5 = iBlockAccess.getBlockMeta(x, y, z) & 7;
        float var6 = 3.0F / 16.0F;
        if (var5 == 1)
        {
            setBoundingBox(0.0F, 0.2F, 0.5F - var6, var6 * 2.0F, 0.8F, 0.5F + var6);
        }
        else if (var5 == 2)
        {
            setBoundingBox(1.0F - var6 * 2.0F, 0.2F, 0.5F - var6, 1.0F, 0.8F, 0.5F + var6);
        }
        else if (var5 == 3)
        {
            setBoundingBox(0.5F - var6, 0.2F, 0.0F, 0.5F + var6, 0.8F, var6 * 2.0F);
        }
        else if (var5 == 4)
        {
            setBoundingBox(0.5F - var6, 0.2F, 1.0F - var6 * 2.0F, 0.5F + var6, 0.8F, 1.0F);
        }
        else
        {
            var6 = 0.25F;
            setBoundingBox(0.5F - var6, 0.0F, 0.5F - var6, 0.5F + var6, 0.6F, 0.5F + var6);
        }

    }

    public override void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
        onUse(world, x, y, z, player);
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (world.isRemote)
        {
            return true;
        }
        else
        {
            int var6 = world.getBlockMeta(x, y, z);
            int var7 = var6 & 7;
            int var8 = 8 - (var6 & 8);
            world.setBlockMeta(x, y, z, var7 + var8);
            world.setBlocksDirty(x, y, z, x, y, z);
            world.playSound((double)x + 0.5D, (double)y + 0.5D, (double)z + 0.5D, "random.click", 0.3F, var8 > 0 ? 0.6F : 0.5F);
            world.notifyNeighbors(x, y, z, id);
            if (var7 == 1)
            {
                world.notifyNeighbors(x - 1, y, z, id);
            }
            else if (var7 == 2)
            {
                world.notifyNeighbors(x + 1, y, z, id);
            }
            else if (var7 == 3)
            {
                world.notifyNeighbors(x, y, z - 1, id);
            }
            else if (var7 == 4)
            {
                world.notifyNeighbors(x, y, z + 1, id);
            }
            else
            {
                world.notifyNeighbors(x, y - 1, z, id);
            }

            return true;
        }
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        int var5 = world.getBlockMeta(x, y, z);
        if ((var5 & 8) > 0)
        {
            world.notifyNeighbors(x, y, z, id);
            int var6 = var5 & 7;
            if (var6 == 1)
            {
                world.notifyNeighbors(x - 1, y, z, id);
            }
            else if (var6 == 2)
            {
                world.notifyNeighbors(x + 1, y, z, id);
            }
            else if (var6 == 3)
            {
                world.notifyNeighbors(x, y, z - 1, id);
            }
            else if (var6 == 4)
            {
                world.notifyNeighbors(x, y, z + 1, id);
            }
            else
            {
                world.notifyNeighbors(x, y - 1, z, id);
            }
        }

        base.onBreak(world, x, y, z);
    }

    public override bool isPoweringSide(IBlockAccess iBlockAccess, int x, int y, int a, int side)
    {
        return (iBlockAccess.getBlockMeta(x, y, a) & 8) > 0;
    }

    public override bool isStrongPoweringSide(World world, int x, int y, int z, int side)
    {
        int var6 = world.getBlockMeta(x, y, z);
        if ((var6 & 8) == 0)
        {
            return false;
        }
        else
        {
            int var7 = var6 & 7;
            return var7 == 6 && side == 1 ? true : (var7 == 5 && side == 1 ? true : (var7 == 4 && side == 2 ? true : (var7 == 3 && side == 3 ? true : (var7 == 2 && side == 4 ? true : var7 == 1 && side == 5))));
        }
    }

    public override bool canEmitRedstonePower()
    {
        return true;
    }
}
