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
            updates.Add(new RedstoneUpdateInfo(ctx.X, ctx.Y, ctx.Z, ctx.Level.GetTime()));
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

    public override void onPlaced(OnPlacedEvt evt)
    {
        if (evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z) == 0)
        {
            base.onPlaced(evt);
        }

        if (_lit)
        {
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y - 1, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y + 1, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X - 1, evt.Y, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X + 1, evt.Y, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z - 1, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z + 1, id);
        }
    }

    public override void onBreak(OnBreakEvt evt)
    {
        if (_lit)
        {
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y - 1, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y + 1, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X - 1, evt.Y, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X + 1, evt.Y, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z - 1, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z + 1, id);
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

    private bool shouldUnpower(OnTickEvt evt)
    {
        int x = evt.X;
        int y = evt.Y;
        int z = evt.Z;
        RedstoneEngine redstoneEngine = evt.Level.Redstone;
        int meta = evt.Level.Reader.GetBlockMeta(x, y, z);
        return (meta == 5 && redstoneEngine.IsPoweringSide(x, y - 1, z, 0)) || (meta == 3 && redstoneEngine.IsPoweringSide(x, y, z - 1, 2)) ||
               (meta == 4 && redstoneEngine.IsPoweringSide(x, y, z + 1, 3)) || (meta == 1 && redstoneEngine.IsPoweringSide(x - 1, y, z, 4)) || (meta == 2 && redstoneEngine.IsPoweringSide(x + 1, y, z, 5));
    }

    public override void onTick(OnTickEvt evt)
    {
        int x = evt.X;
        int y = evt.Y;
        int z = evt.Z;
        bool shouldTurnOff = shouldUnpower(evt);
        List<RedstoneUpdateInfo> updates = s_torchUpdates.Value!;

        while (updates.Count > 0 && evt.Level.GetTime() - updates[0].updateTime > 100L)
        {
            updates.RemoveAt(0);
        }

        if (_lit)
        {
            if (shouldTurnOff)
            {
                evt.Level.BlockWriter.SetBlock(x, y, z, RedstoneTorch.id, evt.Level.Reader.GetBlockMeta(x, y, z));
                if (isBurnedOut(evt, true))
                {
                    evt.Level.Broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, "random.fizz", 0.5F, 2.6F + (Random.Shared.NextSingle() - Random.Shared.NextSingle()) * 0.8F);

                    for (int particleIndex = 0; particleIndex < 5; ++particleIndex)
                    {
                        double particleX = x + Random.Shared.NextDouble() * 0.6D + 0.2D;
                        double particleY = y + Random.Shared.NextDouble() * 0.6D + 0.2D;
                        double particleZ = z + Random.Shared.NextDouble() * 0.6D + 0.2D;
                        evt.Level.Broadcaster.AddParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                    }
                }
            }
        }
        else if (!shouldTurnOff && !isBurnedOut(evt, false))
        {
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, LitRedstoneTorch.id, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z));
        }
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        base.neighborUpdate(evt);
        evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, id, getTickRate());
    }

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => side == 0 && isPoweringSide(world, x, y, z, side);

    public override int getDroppedItemId(int blockMeta) => LitRedstoneTorch.id;

    public override bool canEmitRedstonePower() => true;

    public override void randomDisplayTick(OnTickEvt evt)
    {
        if (!_lit)
        {
            return;
        }

        int meta = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        double particleX = evt.X + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleY = evt.Y + 0.7F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleZ = evt.Z + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double verticalOffset = 0.22F;
        double horizontalOffset = 0.27F;
        if (meta == 1)
        {
            evt.Level.Broadcaster.AddParticle("reddust", particleX - horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 2)
        {
            evt.Level.Broadcaster.AddParticle("reddust", particleX + horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 3)
        {
            evt.Level.Broadcaster.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ - horizontalOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 4)
        {
            evt.Level.Broadcaster.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ + horizontalOffset, 0.0D, 0.0D, 0.0D);
        }
        else
        {
            evt.Level.Broadcaster.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
        }
    }
}
