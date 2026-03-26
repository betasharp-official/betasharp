using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Rules;
using BetaSharp.Worlds.Core.Systems;

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
        return id == Farmland.id;
    }

    public override void onTick(OnTickEvent @event)
    {
        base.onTick(@event);
        if (@event.World.Lighting.GetBrightness(LightType.Block, @event.X, @event.Y + 1, @event.Z) >= 9)
        {
            int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            if (meta < 7)
            {
                float var7 = getAvailableMoisture(@event.World.Reader, @event.X, @event.Y, @event.Z);
                if (Random.Shared.Next(100) / var7 == 0)
                {
                    ++meta;
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta);
                }
            }
        }
    }

    public void applyFullGrowth(IWorldContext world, int x, int y, int z)
    {
        world.Writer.SetBlockMeta(x, y, z, 7);
    }

    private float getAvailableMoisture(IBlockReader read, int x, int y, int z)
    {
        float totalMoisture = 1.0F;
        int blockNorth = read.GetBlockId(x, y, z - 1);
        int blockSouth = read.GetBlockId(x, y, z + 1);
        int blockWest = read.GetBlockId(x - 1, y, z);
        int blockEast = read.GetBlockId(x + 1, y, z);
        int blockNorthWest = read.GetBlockId(x - 1, y, z - 1);
        int blockNorthEast = read.GetBlockId(x + 1, y, z - 1);
        int blockSouthEast = read.GetBlockId(x + 1, y, z + 1);
        int blockSouthWest = read.GetBlockId(x - 1, y, z + 1);
        bool cropsEastWest = blockWest == id || blockEast == id;
        bool cropsNorthSouth = blockNorth == id || blockSouth == id;
        bool cropsDiagonals = blockNorthWest == id || blockNorthEast == id || blockSouthEast == id || blockSouthWest == id;

        for (int dx = x - 1; dx <= x + 1; ++dx)
        {
            for (int dz = z - 1; dz <= z + 1; ++dz)
            {
                int blockBelow = read.GetBlockId(dx, y - 1, dz);
                float cellMoisture = 0.0F;
                if (blockBelow == Farmland.id)
                {
                    cellMoisture = 1.0F;
                    if (read.GetBlockMeta(dx, y - 1, dz) > 0)
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

        if (cropsDiagonals || (cropsEastWest && cropsNorthSouth))
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

    public override void dropStacks(OnDropEvent @event)
    {
        base.dropStacks(@event);
        if (!@event.World.IsRemote && @event.World.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            for (int attempt = 0; attempt < 3; ++attempt)
            {
                if (Random.Shared.Next(15) <= @event.Meta)
                {
                    float spreadFactor = 0.7F;
                    float offsetX = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    float offsetY = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    float offsetZ = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    EntityItem entityItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(Item.Seeds));
                    entityItem.delayBeforeCanPickup = 10;
                    @event.World.Entities.SpawnEntity(entityItem);
                }
            }
        }
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return blockMeta == 7 ? Item.Wheat.id : -1;
    }

    public override int getDroppedItemCount()
    {
        return 1;
    }
}
