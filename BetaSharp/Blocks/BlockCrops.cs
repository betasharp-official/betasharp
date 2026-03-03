using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockCrops : BlockPlant
{

    public BlockCrops(int i, int j) : base(i, j)
    {
        textureId = j;
        setTickRandomly(true);
        float halfWidth = 0.5F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, 0.25F, 0.5F + halfWidth);
    }

    protected override bool canPlantOnTop(int id)
    {
        return id == Block.Farmland.id;
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        base.onTick(world, x, y, z, random);
        if (world.getLightLevel(x, y + 1, z) >= 9)
        {
            int meta = world.getBlockMeta(x, y, z);
            if (meta < 7)
            {
                float var7 = getAvailableMoisture(world, x, y, z);
                if (random.NextInt((int)(100.0F / var7)) == 0)
                {
                    ++meta;
                    world.setBlockMeta(x, y, z, meta);
                }
            }
        }

    }

    public void applyFullGrowth(World world, int x, int y, int z)
    {
        world.setBlockMeta(x, y, z, 7);
    }

    private float getAvailableMoisture(World world, int x, int y, int z)
    {
        float totalMoisture = 1.0F;
        int blockNorth = world.getBlockId(x, y, z - 1);
        int blockSouth = world.getBlockId(x, y, z + 1);
        int blockWest = world.getBlockId(x - 1, y, z);
        int blockEast = world.getBlockId(x + 1, y, z);
        int blockNorthWest = world.getBlockId(x - 1, y, z - 1);
        int blockNorthEast = world.getBlockId(x + 1, y, z - 1);
        int blockSouthEast = world.getBlockId(x + 1, y, z + 1);
        int blockSouthWest = world.getBlockId(x - 1, y, z + 1);
        bool cropsEastWest = blockWest == id || blockEast == id;
        bool cropsNorthSouth = blockNorth == id || blockSouth == id;
        bool cropsDiagonals = blockNorthWest == id || blockNorthEast == id || blockSouthEast == id || blockSouthWest == id;

        for (int dx = x - 1; dx <= x + 1; ++dx)
        {
            for (int dz = z - 1; dz <= z + 1; ++dz)
            {
                int blockBelow = world.getBlockId(dx, y - 1, dz);
                float cellMoisture = 0.0F;
                if (blockBelow == Block.Farmland.id)
                {
                    cellMoisture = 1.0F;
                    if (world.getBlockMeta(dx, y - 1, dz) > 0)
                    {
                        cellMoisture = 3.0F;
                    }
                }

                if (dx != x || dz != z)
                {
                    cellMoisture /= 4.0F;
                }

                totalMoisture += cellMoisture;
            }
        }

        if (cropsDiagonals || cropsEastWest && cropsNorthSouth)
        {
            totalMoisture /= 2.0F;
        }

        return totalMoisture;
    }

    public override int getTexture(int side, int meta)
    {
        if (meta < 0)
        {
            meta = 7;
        }

        return textureId + meta;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Crops;
    }

    public override void dropStacks(World world, int x, int y, int z, int meta, float luck)
    {
        base.dropStacks(world, x, y, z, meta, luck);
        if (!world.isRemote && world.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            for (int attempt = 0; attempt < 3; ++attempt)
            {
                if (world.random.NextInt(15) <= meta)
                {
                    float spreadFactor = 0.7F;
                    float offsetX = world.random.NextFloat() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    float offsetY = world.random.NextFloat() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    float offsetZ = world.random.NextFloat() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    EntityItem entityItem = new EntityItem(world, (double)((float)x + offsetX), (double)((float)y + offsetY), (double)((float)z + offsetZ), new ItemStack(Item.Seeds));
                    entityItem.delayBeforeCanPickup = 10;
                    world.SpawnEntity(entityItem);
                }
            }

        }
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return blockMeta == 7 ? Item.Wheat.id : -1;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 1;
    }
}
