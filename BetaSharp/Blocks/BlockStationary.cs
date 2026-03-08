using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockStationary : BlockFluid
{
    public BlockStationary(int id, Material material) : base(id, material)
    {
        setTickRandomly(false);
        if (material == Material.Lava)
        {
            setTickRandomly(true);
        }

    }

    public override void neighborUpdate(OnTickContext ctx)
    {
        base.neighborUpdate(ctx);
        if (ctx.WorldView.GetBlockId(ctx.X, ctx.Y, ctx.Z) == base.id)
        {
            convertToFlowing(ctx);
        }

    }

    private void convertToFlowing(OnTickContext ctx)
    {
        int meta = ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
        ctx.WorldView.PauseTicking = true;
        ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, id - 1, meta);
        ctx.WorldWrite.SetBlocksDirty(ctx.X, ctx.Y, ctx.Z, ctx.X, ctx.Y, ctx.Z);
        ctx.WorldView.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id - 1, getTickRate());
        ctx.WorldView.PauseTicking = false;
    }

    public override void onTick(OnTickContext ctx)
    {
        if (material == Material.Lava)
        {
            int attempts = ctx.WorldView.random.NextInt(3);

            for (int attempt = 0; attempt < attempts; ++attempt)
            {
                ctx.X += ctx.WorldView.random.NextInt(3) - 1;
                ++ctx.Y;
                ctx.Z += ctx.WorldView.random.NextInt(3) - 1;
                int neighborBlockId = ctx.WorldView.GetBlockId(ctx.X, ctx.Y, ctx.Z);
                if (neighborBlockId == 0)
                {
                    if (isFlammable(ctx.WorldView, ctx.X - 1, ctx.Y, ctx.Z) || isFlammable(ctx.WorldView, ctx.X + 1, ctx.Y, ctx.Z) || isFlammable(ctx.WorldView, ctx.X, ctx.Y, ctx.Z - 1) || isFlammable(ctx.WorldView, ctx.X, ctx.Y, ctx.Z + 1) || isFlammable(ctx.WorldView, ctx.X, ctx.Y - 1, ctx.Z) || isFlammable(ctx.WorldView, ctx.X, ctx.Y + 1, ctx.Z))
                    {
                        ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Block.Fire.id);
                        return;
                    }
                }
                else if (Blocks[neighborBlockId].material.BlocksMovement)
                {
                    return;
                }
            }
        }

    }

    private bool isFlammable(World world, int x, int y, int z)
    {
        return world.getMaterial(x, y, z).IsBurnable;
    }
}
