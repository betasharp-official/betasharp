using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockDetectorRail : BlockRail
{
    public BlockDetectorRail(int id, int textureId) : base(id, textureId, true)
    {
        setTickRandomly(true);
    }

    public override int getTickRate()
    {
        return 20;
    }

    public override bool canEmitRedstonePower()
    {
        return true;
    }

    public override void onEntityCollision(World world, int x, int y, int z, Entity entity)
    {
        if (!world.isRemote)
        {
            int meta = world.getBlockMeta(x, y, z);
            if ((meta & 8) == 0)
            {
                updatePoweredStatus(world, x, y, z, meta);
            }
        }
    }

    public override void onTick(OnTickContext ctx)
    {
        if (!ctx.IsRemote)
        {
            int meta = ctx.WorldView.getBlockMeta(ctx.X, ctx.Y, ctx.Z);
            if ((meta & 8) != 0)
            {
                updatePoweredStatus(ctx, meta);
            }
        }
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        return (iBlockReader.getBlockMeta(x, y, z) & 8) != 0;
    }

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side)
    {
        return (world.getBlockMeta(x, y, z) & 8) == 0 ? false : side == 1;
    }

    private void updatePoweredStatus(OnTickContext ctx, int meta)
    {
        bool isPowered = (meta & 8) != 0;
        bool hasMinecart = false;
        float detectionInset = 2.0F / 16.0F;
        var minecartsOnRail = ctx.Entities.CollectEntitiesOfType<EntityMinecart>(new Box((double)((float)ctx.X + detectionInset), (double)ctx.Y, (double)((float)ctx.Z + detectionInset), (double)((float)(ctx.X + 1) - detectionInset), (double)ctx.Y + 0.25D, (double)((float)(ctx.Z + 1) - detectionInset)));
        if (minecartsOnRail.Count > 0)
        {
            hasMinecart = true;
        }

        if (hasMinecart && !isPowered)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta | 8);
            ctx.WorldView.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
            ctx.WorldView.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
            ctx.WorldWrite.SetBlocksDirty(ctx.X, ctx.Y, ctx.Z, ctx.X, ctx.Y, ctx.Z);
        }

        if (!hasMinecart && isPowered)
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta & 7);
            ctx.WorldView.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
            ctx.WorldView.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
            ctx.WorldWrite.SetBlocksDirty(ctx.X, ctx.Y, ctx.Z, ctx.X, ctx.Y, ctx.Z);
        }

        if (hasMinecart)
        {
            ctx.WorldView.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
        }

    }
}
