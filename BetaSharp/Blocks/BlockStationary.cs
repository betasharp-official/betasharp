using BetaSharp.Blocks.Materials;
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

    public override void neighborUpdate(OnTickEvt ctx)
    {
        base.neighborUpdate(ctx);
        if (ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z) == id)
        {
            convertToFlowing(ctx);
        }
    }

    private void convertToFlowing(OnTickEvt ctx)
    {
        int meta = ctx.WorldRead.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
        ctx.WorldRead.PauseTicking = true;
        ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, id - 1, meta);
        ctx.WorldWrite.SetBlocksDirty(ctx.X, ctx.Y, ctx.Z, ctx.X, ctx.Y, ctx.Z);
        ctx.WorldRead.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id - 1, getTickRate());
        ctx.WorldRead.PauseTicking = false;
    }

    public override void onTick(OnTickEvt ctx)
    {
        if (material == Material.Lava)
        {
            int attempts = ctx.WorldRead.random.NextInt(3);

            for (int attempt = 0; attempt < attempts; ++attempt)
            {
                ctx.X += ctx.WorldRead.random.NextInt(3) - 1;
                ++ctx.Y;
                ctx.Z += ctx.WorldRead.random.NextInt(3) - 1;
                int neighborBlockId = ctx.WorldRead.GetBlockId(ctx.X, ctx.Y, ctx.Z);
                if (neighborBlockId == 0)
                {
                    if (isFlammable(ctx.WorldRead, ctx.X - 1, ctx.Y, ctx.Z) || isFlammable(ctx.WorldRead, ctx.X + 1, ctx.Y, ctx.Z) || isFlammable(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z - 1) ||
                        isFlammable(ctx.WorldRead, ctx.X, ctx.Y, ctx.Z + 1) || isFlammable(ctx.WorldRead, ctx.X, ctx.Y - 1, ctx.Z) || isFlammable(ctx.WorldRead, ctx.X, ctx.Y + 1, ctx.Z))
                    {
                        ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, Fire.id);
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

    private bool isFlammable(World world, int x, int y, int z) => world.getMaterial(x, y, z).IsBurnable;
}
