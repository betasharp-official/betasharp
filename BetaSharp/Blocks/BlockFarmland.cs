using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockFarmland : Block
{
    private const sbyte CropRadius = 0;

    public BlockFarmland(int id) : base(id, Material.Soil)
    {
        TextureId = BlockTextures.FarmlandDry;
        SetTickRandomly(true);
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 15.0F / 16.0F, 1.0F);
        SetOpacity(255);
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => new Box(x + 0, y + 0, z + 0, x + 1, y + 1, z + 1);

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override int GetTexture(Side side, int meta) => side switch
    {
        Side.Up when meta > 0 => BlockTextures.FarmlandWet - 1,
        Side.Up => BlockTextures.FarmlandWet,
        _ => BlockTextures.Dirt
    };


    public override void OnTick(OnTickEvent @event)
    {
        if (Random.Shared.Next(5) != 0)
        {
            return;
        }


        if (!IsWaterNearby(@event.World.Reader, @event.X, @event.Y, @event.Z) && !@event.World.Environment.IsRaining)
        {
            int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            if (meta > 0)
            {
                @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta - 1);
            }
            else if (!HasCrop(@event.World.Reader, @event.X, @event.Y, @event.Z))
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Dirt.ID);
            }
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 7);
        }
    }

    public override void OnSteppedOn(OnEntityStepEvent @event)
    {
        if (Random.Shared.Next(4) == 0)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Dirt.ID);
        }
    }

    private static bool HasCrop(IBlockReader world, int x, int y, int z)
    {
        for (int dx = x - CropRadius; dx <= x + CropRadius; ++dx)
        {
            for (int dy = z - CropRadius; dy <= z + CropRadius; ++dy)
            {
                if (world.GetBlockId(dx, y + 1, dy) == Wheat.ID)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsWaterNearby(IBlockReader reader, int x, int y, int z)
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

    public override void NeighborUpdate(OnTickEvent @event)
    {
        base.NeighborUpdate(@event);
        if (@event.World.Reader.GetMaterial(@event.X, @event.Y + 1, @event.Z).IsSolid)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Dirt.ID);
        }
    }

    public override int GetDroppedItemId(int blockMeta) => Dirt.GetDroppedItemId(0);
}
