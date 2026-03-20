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
        int blockMetadata = world.getBlockMeta(x, y, z);
        int toggleState = blockMetadata & 8;
        blockMetadata &= 7;
        blockMetadata = -1;
        if (direction == 1 && world.shouldSuffocate(x, y - 1, z))
        {
            blockMetadata = 5 + world.random.NextInt(2);
        }

        if (direction == 2 && world.shouldSuffocate(x, y, z + 1))
        {
            blockMetadata = 4;
        }

        if (direction == 3 && world.shouldSuffocate(x, y, z - 1))
        {
            blockMetadata = 3;
        }

        if (direction == 4 && world.shouldSuffocate(x + 1, y, z))
        {
            blockMetadata = 2;
        }

        if (direction == 5 && world.shouldSuffocate(x - 1, y, z))
        {
            blockMetadata = 1;
        }

        if (blockMetadata == -1)
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }
        else
        {
            world.setBlockMeta(x, y, z, blockMetadata + toggleState);
        }
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (breakIfCannotPlaceAt(world, x, y, z))
        {
            int leverOrientation = world.getBlockMeta(x, y, z) & 7;
            bool shouldBreak = false;
            if (!world.shouldSuffocate(x - 1, y, z) && leverOrientation == 1)
            {
                shouldBreak = true;
            }

            if (!world.shouldSuffocate(x + 1, y, z) && leverOrientation == 2)
            {
                shouldBreak = true;
            }

            if (!world.shouldSuffocate(x, y, z - 1) && leverOrientation == 3)
            {
                shouldBreak = true;
            }

            if (!world.shouldSuffocate(x, y, z + 1) && leverOrientation == 4)
            {
                shouldBreak = true;
            }

            if (!world.shouldSuffocate(x, y - 1, z) && leverOrientation == 5)
            {
                shouldBreak = true;
            }

            if (!world.shouldSuffocate(x, y - 1, z) && leverOrientation == 6)
            {
                shouldBreak = true;
            }

            if (shouldBreak)
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
        int leverOrientation = iBlockAccess.getBlockMeta(x, y, z) & 7;
        float leverSize = 3.0F / 16.0F;
        if (leverOrientation == 1)
        {
            setBoundingBox(0.0F, 0.2F, 0.5F - leverSize, leverSize * 2.0F, 0.8F, 0.5F + leverSize);
        }
        else if (leverOrientation == 2)
        {
            setBoundingBox(1.0F - leverSize * 2.0F, 0.2F, 0.5F - leverSize, 1.0F, 0.8F, 0.5F + leverSize);
        }
        else if (leverOrientation == 3)
        {
            setBoundingBox(0.5F - leverSize, 0.2F, 0.0F, 0.5F + leverSize, 0.8F, leverSize * 2.0F);
        }
        else if (leverOrientation == 4)
        {
            setBoundingBox(0.5F - leverSize, 0.2F, 1.0F - leverSize * 2.0F, 0.5F + leverSize, 0.8F, 1.0F);
        }
        else
        {
            leverSize = 0.25F;
            setBoundingBox(0.5F - leverSize, 0.0F, 0.5F - leverSize, 0.5F + leverSize, 0.6F, 0.5F + leverSize);
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
            int blockMetadata = world.getBlockMeta(x, y, z);
            int leverOrientation = blockMetadata & 7;
            int toggleState = 8 - (blockMetadata & 8);
            world.setBlockMeta(x, y, z, leverOrientation + toggleState);
            world.setBlocksDirty(x, y, z, x, y, z);
            world.playSound((double)x + 0.5D, (double)y + 0.5D, (double)z + 0.5D, "random.click", 0.3F, toggleState > 0 ? 0.6F : 0.5F);
            world.notifyNeighbors(x, y, z, id);
            if (leverOrientation == 1)
            {
                world.notifyNeighbors(x - 1, y, z, id);
            }
            else if (leverOrientation == 2)
            {
                world.notifyNeighbors(x + 1, y, z, id);
            }
            else if (leverOrientation == 3)
            {
                world.notifyNeighbors(x, y, z - 1, id);
            }
            else if (leverOrientation == 4)
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
        int blockMetadata = world.getBlockMeta(x, y, z);
        if ((blockMetadata & 8) > 0)
        {
            world.notifyNeighbors(x, y, z, id);
            int leverOrientation = blockMetadata & 7;
            if (leverOrientation == 1)
            {
                world.notifyNeighbors(x - 1, y, z, id);
            }
            else if (leverOrientation == 2)
            {
                world.notifyNeighbors(x + 1, y, z, id);
            }
            else if (leverOrientation == 3)
            {
                world.notifyNeighbors(x, y, z - 1, id);
            }
            else if (leverOrientation == 4)
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
        int blockMetadata = world.getBlockMeta(x, y, z);
        if ((blockMetadata & 8) == 0)
        {
            return false;
        }
        else
        {
            int leverOrientation = blockMetadata & 7;
            return leverOrientation == 6 && side == 1 ? true : (leverOrientation == 5 && side == 1 ? true : (leverOrientation == 4 && side == 2 ? true : (leverOrientation == 3 && side == 3 ? true : (leverOrientation == 2 && side == 4 ? true : leverOrientation == 1 && side == 5))));
        }
    }

    public override bool canEmitRedstonePower()
    {
        return true;
    }
}
