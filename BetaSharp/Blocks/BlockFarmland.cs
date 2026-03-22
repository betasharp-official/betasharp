using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

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

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        return new Box(x + 0, y + 0, z + 0, x + 1, y + 1, z + 1);
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
        return side == 1 && meta > 0 ? textureId - 1 : side == 1 ? textureId : 2;
    }

    public override void onTick(OnTickEvent @event)
    {
        if (Random.Shared.Next(5) == 0)
        {
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
        sbyte cropRadius = 0;

        for (int var6 = x - cropRadius; var6 <= x + cropRadius; ++var6)
        {
            for (int var7 = z - cropRadius; var7 <= z + cropRadius; ++var7)
            {
                if (world.GetBlockId(var6, y + 1, var7) == Wheat.id)
                {
                    return true;
                }
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
        Material material = @event.World.Reader.GetMaterial(@event.X, @event.Y + 1, @event.Z);
        if (material.IsSolid)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, Dirt.id);
        }
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return Dirt.getDroppedItemId(0);
    }
}
