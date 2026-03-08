using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public class BlockPlant : Block
{
    public BlockPlant(int id, int textureId) : base(id, Material.Plant)
    {
        this.textureId = textureId;
        setTickRandomly(true);
        float halfSize = 0.2F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 3.0F, 0.5F + halfSize);
    }

    public override bool canPlaceAt(WorldBlockView world, int x, int y, int z) => base.canPlaceAt(world, x, y, z) && canPlantOnTop(world.GetBlockId(x, y - 1, z));

    protected virtual bool canPlantOnTop(int id) => id == GrassBlock.id || id == Dirt.id || id == Farmland.id;

    public override void neighborUpdate(WorldBlockView world, int x, int y, int z, int id)
    {
        base.neighborUpdate(world, x, y, z, id);
        breakIfCannotGrow(world, x, y, z);
    }

    public override void onTick(WorldBlockView worldView, int x, int y, int z, JavaRandom random, WorldEventBroadcaster broadcaster, bool isRemote) => breakIfCannotGrow(worldView, x, y, z);

    protected void breakIfCannotGrow(WorldBlockView world, int x, int y, int z)
    {
        if (!canGrow(world, x, y, z))
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, 0);
        }
    }

    public override bool canGrow(World world, int x, int y, int z) => (world.getBrightness(x, y, z) >= 8 || world.hasSkyLight(x, y, z)) && canPlantOnTop(world.getBlockId(x, y - 1, z));

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Reed;
}
