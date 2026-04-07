using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockFarmland : Block
{
    private const sbyte CropRadius = 0;
    public BlockFarmland(int id) : base(id, Material.Soil)
    {
        TextureId = 87;
        setTickRandomly(true);
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 15.0F / 16.0F, 1.0F);
        setOpacity(255);
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => new Box(x + 0, y + 0, z + 0, x + 1, y + 1, z + 1);

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override int GetTexture(Side side, int meta) => side switch
    {
        Side.Up when meta > 0 => TextureId - 1,
        Side.Up => TextureId,
        _ => 2
    };


    public override void onTick(OnTickEvent @event)
    {
        if (Random.Shared.Next(5) != 0) return;


        if (!isWaterNearby(@event.World.Reader, @event.X, @event.Y, @event.Z) && !@event.World.Environment.IsRaining)
        {
            int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            if (meta > 0)
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta - 1);
            }
            else if (!hasCrop(@event.World.Reader, @event.X, @event.Y, @event.Z))
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Dirt.id);
            }
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 7);
        }
    }

    public override void onSteppedOn(OnEntityStepEvent @event)
    {
        if (Random.Shared.Next(4) == 0)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Dirt.id);
        }
    }

    private static bool hasCrop(IBlockReader world, int x, int y, int z)
    {
        for (int dx = x - CropRadius; dx <= x + CropRadius; ++dx)
        {
            for (int dy = z - CropRadius; dy <= z + CropRadius; ++dy)
            {
                if (world.GetBlockId(dx, y + 1, dy) == Wheat.id) return true;
            }
        }

        return false;
    }

    private static bool isWaterNearby(IBlockReader reader, int x, int y, int z)
    {
        for (int checkX = x - 4; checkX <= x + 4; ++checkX)
        {
            for (int checkY = y; checkY <= y + 1; ++checkY)
            {
                for (int checkZ = z - 4; checkZ <= z + 4; ++checkZ)
                {
                    if (reader.GetMaterial(checkX, checkY, checkZ) == Material.Water)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        base.neighborUpdate(@event);
        if (@event.World.Reader.GetMaterial(@event.X, @event.Y + 1, @event.Z).IsSolid)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Dirt.id);
        }
    }

    public override int getDroppedItemId(int blockMeta) => Dirt.getDroppedItemId(0);
}
