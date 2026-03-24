using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Rules;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockCrops : BlockPlant
{
    public BlockCrops(int i, int j) : base(i, j)
    {
        TextureId = j;
        SetTickRandomly(true);
        float halfWidth = 0.5F;
        SetBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, 0.25F, 0.5F + halfWidth);
    }

    protected override bool canPlantOnTop(int id) => id == Farmland.Id;

    public override void OnTick(OnTickEvent @event)
    {
        base.OnTick(@event);
        if (@event.World.Lighting.GetBrightness(LightType.Block, @event.X, @event.Y + 1, @event.Z) < 9)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (meta >= 7)
        {
            return;
        }

        float var7 = getAvailableMoisture(@event.World.Reader, @event.X, @event.Y, @event.Z);
        if (Random.Shared.Next(100) / var7 != 0)
        {
            return;
        }

        ++meta;
        @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta);
    }

    public void applyFullGrowth(IWorldContext world, int x, int y, int z) => world.Writer.SetBlockMeta(x, y, z, 7);

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
        bool cropsEastWest = blockWest == Id || blockEast == Id;
        bool cropsNorthSouth = blockNorth == Id || blockSouth == Id;
        bool cropsDiagonals = blockNorthWest == Id || blockNorthEast == Id || blockSouthEast == Id || blockSouthWest == Id;

        for (int dx = x - 1; dx <= x + 1; ++dx)
        {
            for (int dz = z - 1; dz <= z + 1; ++dz)
            {
                int blockBelow = read.GetBlockId(dx, y - 1, dz);
                float cellMoisture = 0.0F;
                if (blockBelow == Farmland.Id)
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

    public override int GetTexture(Side side, int meta)
    {
        if (meta < 0)
        {
            meta = 7;
        }

        return TextureId + meta;
    }

    public override BlockRendererType GetRenderType() => BlockRendererType.Crops;

    public override void DropStacks(OnDropEvent @event)
    {
        base.DropStacks(@event);
        if (@event.World.IsRemote || !@event.World.Rules.GetBool(DefaultRules.DoTileDrops))
        {
            return;
        }

        for (int attempt = 0; attempt < 3; ++attempt)
        {
            if (Random.Shared.Next(15) > @event.Meta)
            {
                continue;
            }

            const float spreadFactor = 0.7F;
            float offsetX = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
            float offsetY = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
            float offsetZ = Random.Shared.NextSingle() * spreadFactor + (1.0F - spreadFactor) * 0.5F;
            EntityItem entityItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(Item.Seeds))
            {
                delayBeforeCanPickup = 10
            };
            @event.World.Entities.SpawnEntity(entityItem);
        }
    }

    public override int GetDroppedItemId(int blockMeta) => blockMeta == 7 ? Item.Wheat.id : -1;

    public override int GetDroppedItemCount() => 1;
}
