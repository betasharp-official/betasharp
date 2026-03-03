using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockTorch : Block
{

    public BlockTorch(int id, int textureId) : base(id, textureId, Material.PistonBreakable)
    {
        setTickRandomly(true);
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
        return BlockRendererType.Torch;
    }

    private bool canPlaceOn(World world, int x, int y, int z)
    {
        return world.shouldSuffocate(x, y, z) || world.getBlockId(x, y, z) == Block.Fence.id;
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return world.shouldSuffocate(x - 1, y, z) ? true : (world.shouldSuffocate(x + 1, y, z) ? true : (world.shouldSuffocate(x, y, z - 1) ? true : (world.shouldSuffocate(x, y, z + 1) ? true : canPlaceOn(world, x, y - 1, z))));
    }

    public override void onPlaced(World world, int x, int y, int z, int direction)
    {
        int meta = world.getBlockMeta(x, y, z);
        if (direction == 1 && canPlaceOn(world, x, y - 1, z))
        {
            meta = 5;
        }

        if (direction == 2 && world.shouldSuffocate(x, y, z + 1))
        {
            meta = 4;
        }

        if (direction == 3 && world.shouldSuffocate(x, y, z - 1))
        {
            meta = 3;
        }

        if (direction == 4 && world.shouldSuffocate(x + 1, y, z))
        {
            meta = 2;
        }

        if (direction == 5 && world.shouldSuffocate(x - 1, y, z))
        {
            meta = 1;
        }

        world.setBlockMeta(x, y, z, meta);
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        base.onTick(world, x, y, z, random);
        if (world.getBlockMeta(x, y, z) == 0)
        {
            onPlaced(world, x, y, z);
        }

    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        if (world.shouldSuffocate(x - 1, y, z))
        {
            world.setBlockMeta(x, y, z, 1);
        }
        else if (world.shouldSuffocate(x + 1, y, z))
        {
            world.setBlockMeta(x, y, z, 2);
        }
        else if (world.shouldSuffocate(x, y, z - 1))
        {
            world.setBlockMeta(x, y, z, 3);
        }
        else if (world.shouldSuffocate(x, y, z + 1))
        {
            world.setBlockMeta(x, y, z, 4);
        }
        else if (canPlaceOn(world, x, y - 1, z))
        {
            world.setBlockMeta(x, y, z, 5);
        }

        breakIfCannotPlaceAt(world, x, y, z);
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (breakIfCannotPlaceAt(world, x, y, z))
        {
            int meta = world.getBlockMeta(x, y, z);
            bool canPlace = false;
            if (!world.shouldSuffocate(x - 1, y, z) && meta == 1)
            {
                canPlace = true;
            }

            if (!world.shouldSuffocate(x + 1, y, z) && meta == 2)
            {
                canPlace = true;
            }

            if (!world.shouldSuffocate(x, y, z - 1) && meta == 3)
            {
                canPlace = true;
            }

            if (!world.shouldSuffocate(x, y, z + 1) && meta == 4)
            {
                canPlace = true;
            }

            if (!canPlaceOn(world, x, y - 1, z) && meta == 5)
            {
                canPlace = true;
            }

            if (canPlace)
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

    public override HitResult raycast(World world, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        int meta = world.getBlockMeta(x, y, z) & 7;
        float torchWidth = 0.15F;
        if (meta == 1)
        {
            setBoundingBox(0.0F, 0.2F, 0.5F - torchWidth, torchWidth * 2.0F, 0.8F, 0.5F + torchWidth);
        }
        else if (meta == 2)
        {
            setBoundingBox(1.0F - torchWidth * 2.0F, 0.2F, 0.5F - torchWidth, 1.0F, 0.8F, 0.5F + torchWidth);
        }
        else if (meta == 3)
        {
            setBoundingBox(0.5F - torchWidth, 0.2F, 0.0F, 0.5F + torchWidth, 0.8F, torchWidth * 2.0F);
        }
        else if (meta == 4)
        {
            setBoundingBox(0.5F - torchWidth, 0.2F, 1.0F - torchWidth * 2.0F, 0.5F + torchWidth, 0.8F, 1.0F);
        }
        else
        {
            torchWidth = 0.1F;
            setBoundingBox(0.5F - torchWidth, 0.0F, 0.5F - torchWidth, 0.5F + torchWidth, 0.6F, 0.5F + torchWidth);
        }

        return base.raycast(world, x, y, z, startPos, endPos);
    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        int meta = world.getBlockMeta(x, y, z);
        double flameX = (double)((float)x + 0.5F);
        double flameY = (double)((float)y + 0.7F);
        double flameZ = (double)((float)z + 0.5F);
        double yOffset = (double)0.22F;
        double xOffset = (double)0.27F;
        if (meta == 1)
        {
            world.addParticle("smoke", flameX - xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
            world.addParticle("flame", flameX - xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 2)
        {
            world.addParticle("smoke", flameX + xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
            world.addParticle("flame", flameX + xOffset, flameY + yOffset, flameZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 3)
        {
            world.addParticle("smoke", flameX, flameY + yOffset, flameZ - xOffset, 0.0D, 0.0D, 0.0D);
            world.addParticle("flame", flameX, flameY + yOffset, flameZ - xOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 4)
        {
            world.addParticle("smoke", flameX, flameY + yOffset, flameZ + xOffset, 0.0D, 0.0D, 0.0D);
            world.addParticle("flame", flameX, flameY + yOffset, flameZ + xOffset, 0.0D, 0.0D, 0.0D);
        }
        else
        {
            world.addParticle("smoke", flameX, flameY, flameZ, 0.0D, 0.0D, 0.0D);
            world.addParticle("flame", flameX, flameY, flameZ, 0.0D, 0.0D, 0.0D);
        }

    }
}
