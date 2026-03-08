using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

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

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => new Box(x + 0, y + 0, z + 0, x + 1, y + 1, z + 1);

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override int getTexture(int side, int meta) => side == 1 && meta > 0 ? textureId - 1 : side == 1 ? textureId : 2;

    public override void onTick(OnTickEvt ctx)
    {
        if (ctx.Random.NextInt(5) == 0)
        {
            if (!isWaterNearby(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z) && !ctx.WorldRead.isRaining(ctx.X, ctx.Y + 1, ctx.Z))
            {
                int meta = ctx.WorldRead.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
                if (meta > 0)
                {
                    ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta - 1);
                }
                else if (!hasCrop(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z))
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Dirt.id);
                }
            }
            else
            {
                ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, 7);
            }
        }
    }

    public override void onSteppedOn(World world, int x, int y, int z, Entity entity)
    {
        if (world.random.NextInt(4) == 0)
        {
            world.setBlock(x, y, z, Dirt.id);
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

    private static bool isWaterNearby(IBlockReader world, int x, int y, int z)
    {
        for (int checkX = x - 4; checkX <= x + 4; ++checkX)
        {
            for (int checkY = y; checkY <= y + 1; ++checkY)
            {
                for (int checkZ = z - 4; checkZ <= z + 4; ++checkZ)
                {
                    if (world.getMaterial(checkX, checkY, checkZ) == Material.Water)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        base.neighborUpdate(ctx);
        Material material = ctx.WorldRead.getMaterial(ctx.X, ctx.Y + 1, ctx.Z);
        if (material.IsSolid)
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Dirt.id);
        }
    }

    public override int getDroppedItemId(int blockMeta) => Dirt.getDroppedItemId(0);
}
