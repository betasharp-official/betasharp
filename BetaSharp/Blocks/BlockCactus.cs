using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockCactus : Block
{

    public BlockCactus(int id, int textureId) : base(id, textureId, Material.Cactus)
    {
        setTickRandomly(true);
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (world.isAir(x, y + 1, z))
        {
            int heightBelow;
            for (heightBelow = 1; world.getBlockId(x, y - heightBelow, z) == id; ++heightBelow)
            {
            }

            if (heightBelow < 3)
            {
                int growthStage = world.getBlockMeta(x, y, z);
                if (growthStage == 15)
                {
                    world.setBlock(x, y + 1, z, id);
                    world.setBlockMeta(x, y, z, 0);
                }
                else
                {
                    world.setBlockMeta(x, y, z, growthStage + 1);
                }
            }
        }

    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        float edgeInset = 1.0F / 16.0F;
        return new Box((double)((float)x + edgeInset), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)(y + 1) - edgeInset), (double)((float)(z + 1) - edgeInset));
    }

    public override Box getBoundingBox(World world, int x, int y, int z)
    {
        float edgeInset = 1.0F / 16.0F;
        return new Box((double)((float)x + edgeInset), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)(y + 1), (double)((float)(z + 1) - edgeInset));
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId - 1 : (side == 0 ? textureId + 1 : textureId);
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Cactus;
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return !base.canPlaceAt(world, x, y, z) ? false : canGrow(world, x, y, z);
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (!canGrow(world, x, y, z))
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }

    }

    public override bool canGrow(World world, int x, int y, int z)
    {
        if (world.getMaterial(x - 1, y, z).IsSolid)
        {
            return false;
        }
        else if (world.getMaterial(x + 1, y, z).IsSolid)
        {
            return false;
        }
        else if (world.getMaterial(x, y, z - 1).IsSolid)
        {
            return false;
        }
        else if (world.getMaterial(x, y, z + 1).IsSolid)
        {
            return false;
        }
        else
        {
            int blockBelowId = world.getBlockId(x, y - 1, z);
            return blockBelowId == Block.Cactus.id || blockBelowId == Block.Sand.id;
        }
    }

    public override void onEntityCollision(World world, int x, int y, int z, Entity entity)
    {
        entity.damage((Entity)null, 1);
    }
}
