using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockFarmland : Block
{

    public BlockFarmland(int id) : base(id, Material.Soil)
    {
        textureId = 87;
        setTickRandomly(true);
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 15.0F / 16.0F, 1.0F);
        setOpacity(255);
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return new Box((double)(x + 0), (double)(y + 0), (double)(z + 0), (double)(x + 1), (double)(y + 1), (double)(z + 1));
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override int getTexture(int side, int meta)
    {
        return side == 1 && meta > 0 ? textureId - 1 : (side == 1 ? textureId : 2);
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (random.NextInt(5) == 0)
        {
            if (!isWaterNearby(world, x, y, z) && !world.isRaining(x, y + 1, z))
            {
                int meta = world.getBlockMeta(x, y, z);
                if (meta > 0)
                {
                    world.setBlockMeta(x, y, z, meta - 1);
                }
                else if (!hasCrop(world, x, y, z))
                {
                    world.setBlock(x, y, z, Block.Dirt.id);
                }
            }
            else
            {
                world.setBlockMeta(x, y, z, 7);
            }
        }

    }

    public override void onSteppedOn(World world, int x, int y, int z, Entity entity)
    {
        if (world.random.NextInt(4) == 0)
        {
            world.setBlock(x, y, z, Block.Dirt.id);
        }

    }

    private static bool hasCrop(World world, int x, int y, int z)
    {
        sbyte cropRadius = 0;

        for (int cx = x - cropRadius; cx <= x + cropRadius; ++cx)
        {
            for (int cz = z - cropRadius; cz <= z + cropRadius; ++cz)
            {
                if (world.getBlockId(cx, y + 1, cz) == Block.Wheat.id)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool isWaterNearby(World world, int x, int y, int z)
    {
        for (int checkX = x - 4; checkX <= x + 4; ++checkX)
        {
            for (int checkY = y; checkY <= y + 1; ++checkY)
            {
                for (int checkZ = z - 4; checkZ <= z + 4; ++checkZ)
                {
                    if (world.getMaterial(checkX, checkY, checkZ) == Material.Water)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        base.neighborUpdate(world, x, y, z, id);
        Material material = world.getMaterial(x, y + 1, z);
        if (material.IsSolid)
        {
            world.setBlock(x, y, z, Block.Dirt.id);
        }

    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Block.Dirt.getDroppedItemId(0, random);
    }
}
