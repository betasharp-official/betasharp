using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockSand : Block
{
    private static readonly ThreadLocal<bool> s_fallInstantly = new(() => false);

    public static bool fallInstantly
    {
        get => s_fallInstantly.Value;
        set => s_fallInstantly.Value = value;
    }

    public BlockSand(int id, int textureId) : base(id, textureId, Material.Sand)
    {
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        world.ScheduleBlockUpdate(x, y, z, id, getTickRate());
    }

    public override void neighborUpdate(OnTickContext ctx)
    {
        ctx.WorldView.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
    }

    public override void onTick(OnTickContext ctx)
    {
        processFall(ctx);
    }

    private void processFall(OnTickContext ctx)
    {
        if (canFallThrough(ctx) && ctx.Y >= 0)
        {
            sbyte checkRadius = 32;
            if (!fallInstantly && ctx.WorldView.IsRegionLoaded(ctx.X - checkRadius, ctx.Y - checkRadius, ctx.Z - checkRadius, ctx.X + checkRadius, ctx.Y + checkRadius, ctx.Z + checkRadius))
            {
                EntityFallingSand fallingSand = new EntityFallingSand(ctx.WorldView, (double)(ctx.X + 0.5F), (double)(ctx.Y + 0.5F), (double)(ctx.Z + 0.5F), id);
                ctx.Entities.SpawnEntity(fallingSand);
            }
            else
            {
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);

                while (canFallThrough(ctx) && ctx.Y > 0)
                {
                    --ctx.Y;
                }

                if (ctx.Y > 0)
                {
                    ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, id);
                }
            }
        }

    }

    public override int getTickRate()
    {
        return 3;
    }

    public static bool canFallThrough(OnTickContext ctx)
    {
        int blockId = ctx.WorldView.GetBlockId(ctx.X, ctx.Y, ctx.Z);
        if (blockId == 0)
        {
            return true;
        }
        else if (blockId == Block.Fire.id)
        {
            return true;
        }
        else
        {
            Material material = Block.Blocks[blockId].material;
            return material == Material.Water || material == Material.Lava;
        }
    }
}
