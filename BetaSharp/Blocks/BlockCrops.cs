using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Rules;
using BetaSharp.Worlds.Core;

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

    protected override bool canPlantOnTop(int id) => id == Farmland.id;

    public override void onTick(OnTickEvt ctx)
    {
        base.onTick(ctx);
        if (ctx.Lighting.GetBrightness(LightType.Block, ctx.X, ctx.Y + 1, ctx.Z) >= 9)
        {
            int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            if (meta < 7)
            {
                float var7 = getAvailableMoisture(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z);
                if (Random.Shared.Next(100) / var7 == 0)
                {
                    ++meta;
                    ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta);
                }
            }
        }
    }

    public void applyFullGrowth(World world, int x, int y, int z) => world.setBlockMeta(x, y, z, 7);

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

    public override BlockRendererType getRenderType() => BlockRendererType.Crops;

    public override void dropStacks(OnDropEvt ctx)
    {
        base.dropStacks(ctx);
        if (!ctx.IsRemote && ctx.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            for (int attempt = 0; attempt < 3; ++attempt)
            {
                if (Random.Shared.Next(15) <= ctx.Meta)
                {
                    float spreadFactor = 0.7F;
                    float offsetX = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    float offsetY = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    float offsetZ = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
                    EntityItem entityItem = new(world, (float)x + offsetX, (float)y + offsetY, (float)z + offsetZ, new ItemStack(Item.Seeds));
                    entityItem.delayBeforeCanPickup = 10;
                    ctx.Entities.SpawnEntity(entityItem);
                }
            }
        }
    }

    public override int getDroppedItemId(int blockMeta) => blockMeta == 7 ? Item.Wheat.id : -1;

    public override int getDroppedItemCount() => 1;
}
