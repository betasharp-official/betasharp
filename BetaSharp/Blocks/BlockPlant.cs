using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockPlant : Block
{
    public BlockPlant(int id, int textureId) : base(id, Material.Plant)
    {
        base.textureId = textureId;
        setTickRandomly(true);
        float halfSize = 0.2F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 3.0F, 0.5F + halfSize);
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return base.canPlaceAt(world, x, y, z) && canPlantOnTop(world.getBlockId(x, y - 1, z));
    }

    protected virtual bool canPlantOnTop(int id)
    {
        return id == Block.GrassBlock.id || id == Block.Dirt.id || id == Block.Farmland.id;
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        base.neighborUpdate(world, x, y, z, id);
        breakIfCannotGrow(world, x, y, z);
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
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
        return (world.getBrightness(x, y, z) >= 8 || world.hasSkyLight(x, y, z)) && canPlantOnTop(world.getBlockId(x, y - 1, z));
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
        return BlockRendererType.Reed;
    }
}
