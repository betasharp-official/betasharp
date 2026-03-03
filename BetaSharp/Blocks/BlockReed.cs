using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockReed : Block
{

    public BlockReed(int id, int textureId) : base(id, Material.Plant)
    {
        base.textureId = textureId;
        float halfWidth = 6.0F / 16.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, 1.0F, 0.5F + halfWidth);
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
                int meta = world.getBlockMeta(x, y, z);
                if (meta == 15)
                {
                    world.setBlock(x, y + 1, z, id);
                    world.setBlockMeta(x, y, z, 0);
                }
                else
                {
                    world.setBlockMeta(x, y, z, meta + 1);
                }
            }
        }

    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        int blockBelowId = world.getBlockId(x, y - 1, z);
        return blockBelowId == id ? true : (blockBelowId != Block.GrassBlock.id && blockBelowId != Block.Dirt.id ? false : (world.getMaterial(x - 1, y - 1, z) == Material.Water ? true : (world.getMaterial(x + 1, y - 1, z) == Material.Water ? true : (world.getMaterial(x, y - 1, z - 1) == Material.Water ? true : world.getMaterial(x, y - 1, z + 1) == Material.Water))));
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        breakIfCannotGrow(world, x, y, z);
    }

    protected void breakIfCannotGrow(World world, int x, int y, int z)
    {
        if (!canGrow(world, x, y, z))
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }

    }

    public override bool canGrow(World world, int x, int y, int z)
    {
        return canPlaceAt(world, x, y, z);
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return null;
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Item.SugarCane.id;
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
        return BlockRendererType.Reed;
    }
}
