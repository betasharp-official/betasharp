using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockRedstoneTorch : BlockTorch
{
    private static readonly ThreadLocal<List<RedstoneUpdateInfo>> s_torchUpdates = new(() => []);
    private readonly bool _lit;

    public BlockRedstoneTorch(int id, int textureId, bool lit) : base(id, textureId)
    {
        _lit = lit;
        setTickRandomly(true);
    }

    public override int getTexture(int side, int meta) => side == 1 ? RedstoneWire.getTexture(side, meta) : base.getTexture(side, meta);

    private bool isBurnedOut(OnTickEvt ctx, bool recordUpdate)
    {
        List<RedstoneUpdateInfo> updates = s_torchUpdates.Value!;
        if (recordUpdate)
        {
            updates.Add(new RedstoneUpdateInfo(ctx.X, ctx.Y, ctx.Z, ctx.Time));
        }

        int updateCount = 0;

        for (int i = 0; i < updates.Count; ++i)
        {
            RedstoneUpdateInfo updateInfo = updates[i];
            if (updateInfo.x == ctx.X && updateInfo.y == ctx.Y && updateInfo.z == ctx.Z)
            {
                ++updateCount;
                if (updateCount >= 8)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override int getTickRate() => 2;

    public override void onPlaced(OnPlacedEvt ctx)
    {
        if (ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) == 0)
        {
            base.onPlaced(ctx);
        }

        if (_lit)
        {
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y + 1, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X - 1, ctx.Y, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, id);
        }
    }

    public override void onBreak(OnBreakEvt ctx)
    {
        if (_lit)
        {
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y + 1, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X - 1, ctx.Y, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, id);
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, id);
        }
    }

    public override bool isPoweringSide(IBlockReader world, int x, int y, int z, int side)
    {
        if (!_lit)
        {
            return false;
        }

        int meta = world.GetBlockMeta(x, y, z);
        return (meta != 5 || side != 1) && (meta != 3 || side != 3) && (meta != 4 || side != 2) && (meta != 1 || side != 5) && (meta != 2 || side != 4);
    }

    private bool shouldUnpower(OnTickEvt ctx)
    {
        int x = ctx.X;
        int y = ctx.Y;
        int z = ctx.Z;
        RedstoneEngine redstoneEngine = ctx.Redstone;
        int meta = ctx.WorldRead.GetBlockMeta(x, y, z);
        return (meta == 5 && redstoneEngine.IsPoweringSide(x, y - 1, z, 0)) || (meta == 3 && redstoneEngine.IsPoweringSide(x, y, z - 1, 2)) ||
               (meta == 4 && redstoneEngine.IsPoweringSide(x, y, z + 1, 3)) || (meta == 1 && redstoneEngine.IsPoweringSide(x - 1, y, z, 4)) || (meta == 2 && redstoneEngine.IsPoweringSide(x + 1, y, z, 5));
    }

    public override void onTick(OnTickEvt ctx)
    {
        int x = ctx.X;
        int y = ctx.Y;
        int z = ctx.Z;
        bool shouldTurnOff = shouldUnpower(ctx);
        List<RedstoneUpdateInfo> updates = s_torchUpdates.Value!;

        while (updates.Count > 0 && ctx.Time - updates[0].updateTime > 100L)
        {
            updates.RemoveAt(0);
        }

        if (_lit)
        {
            if (shouldTurnOff)
            {
                ctx.WorldWrite.SetBlock(x, y, z, RedstoneTorch.id, ctx.WorldRead.GetBlockMeta(x, y, z));
                if (isBurnedOut(ctx, true))
                {
                    ctx.Broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, "random.fizz", 0.5F, 2.6F + (ctx.Random.NextFloat() - ctx.Random.NextFloat()) * 0.8F);

                    for (int particleIndex = 0; particleIndex < 5; ++particleIndex)
                    {
                        double particleX = x + ctx.Random.NextDouble() * 0.6D + 0.2D;
                        double particleY = y + ctx.Random.NextDouble() * 0.6D + 0.2D;
                        double particleZ = z + ctx.Random.NextDouble() * 0.6D + 0.2D;
                        ctx.Broadcaster.AddParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                    }
                }
            }
        }
        else if (!shouldTurnOff && !isBurnedOut(ctx, false))
        {
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, LitRedstoneTorch.id, ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z));
        }
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        base.neighborUpdate(ctx);
        // TODO: Implement this
        // ctx.WorldWrite.ScheduleBlockUpdate(ctx.X, ctx.Y, ctx.Z, id, getTickRate());
    }

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => side == 0 && isPoweringSide(world, x, y, z, side);

    public override int getDroppedItemId(int blockMeta) => LitRedstoneTorch.id;

    public override bool canEmitRedstonePower() => true;

    public override void randomDisplayTick(OnTickEvt ctx)
    {
        if (_lit)
        {
            int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
            double particleX = ctx.X + 0.5F + (ctx.Random.NextFloat() - 0.5F) * 0.2D;
            double particleY = ctx.Y + 0.7F + (ctx.Random.NextFloat() - 0.5F) * 0.2D;
            double particleZ = ctx.Z + 0.5F + (ctx.Random.NextFloat() - 0.5F) * 0.2D;
            double verticalOffset = 0.22F;
            double horizontalOffset = 0.27F;
            if (meta == 1)
            {
                ctx.Broadcaster.AddParticle("reddust", particleX - horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 2)
            {
                ctx.Broadcaster.AddParticle("reddust", particleX + horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 3)
            {
                ctx.Broadcaster.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ - horizontalOffset, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 4)
            {
                ctx.Broadcaster.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ + horizontalOffset, 0.0D, 0.0D, 0.0D);
            }
            else
            {
                ctx.Broadcaster.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }
        }
    }
}
