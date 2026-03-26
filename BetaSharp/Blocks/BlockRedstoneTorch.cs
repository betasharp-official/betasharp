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

    private bool isBurnedOut(OnTickEvent ctx, bool recordUpdate)
    {
        List<RedstoneUpdateInfo> updates = s_torchUpdates.Value!;
        if (recordUpdate)
        {
            updates.Add(new RedstoneUpdateInfo(ctx.X, ctx.Y, ctx.Z, ctx.World.GetTime()));
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

    public override void onPlaced(OnPlacedEvent @event)
    {
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) == 0)
        {
            base.onPlaced(@event);
        }

        if (_lit)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, id);
        }
    }

    public override void onBreak(OnBreakEvent @event)
    {
        if (_lit)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, id);
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

    private bool shouldUnpower(OnTickEvent @event)
    {
        int x = @event.X;
        int y = @event.Y;
        int z = @event.Z;
        RedstoneEngine redstoneEngine = @event.World.Redstone;
        int meta = @event.World.Reader.GetBlockMeta(x, y, z);
        return (meta == 5 && redstoneEngine.IsPoweringSide(x, y - 1, z, 0)) || (meta == 3 && redstoneEngine.IsPoweringSide(x, y, z - 1, 2)) ||
               (meta == 4 && redstoneEngine.IsPoweringSide(x, y, z + 1, 3)) || (meta == 1 && redstoneEngine.IsPoweringSide(x - 1, y, z, 4)) || (meta == 2 && redstoneEngine.IsPoweringSide(x + 1, y, z, 5));
    }

    public override void onTick(OnTickEvent @event)
    {
        int x = @event.X;
        int y = @event.Y;
        int z = @event.Z;
        bool shouldTurnOff = shouldUnpower(@event);
        List<RedstoneUpdateInfo> updates = s_torchUpdates.Value!;

        while (updates.Count > 0 && @event.World.GetTime() - updates[0].updateTime > 100L)
        {
            updates.RemoveAt(0);
        }

        if (_lit)
        {
            if (shouldTurnOff)
            {
                @event.World.Writer.SetBlock(x, y, z, RedstoneTorch.id, @event.World.Reader.GetBlockMeta(x, y, z));
                if (isBurnedOut(@event, true))
                {
                    @event.World.Broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, "random.fizz", 0.5F, 2.6F + (Random.Shared.NextSingle() - Random.Shared.NextSingle()) * 0.8F);

                    for (int particleIndex = 0; particleIndex < 5; ++particleIndex)
                    {
                        double particleX = x + Random.Shared.NextDouble() * 0.6D + 0.2D;
                        double particleY = y + Random.Shared.NextDouble() * 0.6D + 0.2D;
                        double particleZ = z + Random.Shared.NextDouble() * 0.6D + 0.2D;
                        @event.World.Broadcaster.AddParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                    }
                }
            }
        }
        else if (!shouldTurnOff && !isBurnedOut(@event, false))
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, LitRedstoneTorch.id, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z));
        }
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        base.neighborUpdate(@event);
        @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, id, getTickRate());
    }

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => side == 0 && isPoweringSide(world, x, y, z, side);

    public override int getDroppedItemId(int blockMeta) => LitRedstoneTorch.id;

    public override bool canEmitRedstonePower() => true;

    public override void randomDisplayTick(OnTickEvent @event)
    {
        if (!_lit)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        double particleX = @event.X + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleY = @event.Y + 0.7F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleZ = @event.Z + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double verticalOffset = 0.22F;
        double horizontalOffset = 0.27F;
        if (meta == 1)
        {
            @event.World.Broadcaster.AddParticle("reddust", particleX - horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 2)
        {
            @event.World.Broadcaster.AddParticle("reddust", particleX + horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 3)
        {
            @event.World.Broadcaster.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ - horizontalOffset, 0.0D, 0.0D, 0.0D);
        }
        else if (meta == 4)
        {
            @event.World.Broadcaster.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ + horizontalOffset, 0.0D, 0.0D, 0.0D);
        }
        else
        {
            @event.World.Broadcaster.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
        }
    }
}
