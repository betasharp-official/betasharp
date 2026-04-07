using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockRedstoneTorch : BlockTorch
{
    private const double VerticalOffset = 0.22F;
    private const double HorizontalOffset = 0.27F;

    private static readonly List<RedstoneUpdateInfo> s_torchUpdates = [];
    private static readonly Lock _updateLock = new();

    private readonly bool _lit;

    public BlockRedstoneTorch(int id, int textureId, bool lit) : base(id, textureId)
    {
        _lit = lit;
        setTickRandomly(true);
    }

    public override int GetTexture(Side side, int meta)
    {
        if (side == Side.Up)
        {
            return RedstoneWire.GetTexture(side, meta);
        }
        return base.GetTexture(side, meta);
    }

    private static bool isBurnedOut(OnTickEvent ctx, bool recordUpdate, long currentTime)
    {
        lock (_updateLock)
        {
            if (recordUpdate)
            {
                s_torchUpdates.Add(new RedstoneUpdateInfo(ctx.X, ctx.Y, ctx.Z, currentTime));
            }

            int updateCount = 0;

            foreach (var updateInfo in s_torchUpdates)
            {
                if (updateInfo.x != ctx.X || updateInfo.y != ctx.Y || updateInfo.z != ctx.Z) continue;

                ++updateCount;
                if (updateCount >= 8) return true;
            }

            return false;
        }
    }

    public override int getTickRate() => 2;

    public override void onPlaced(OnPlacedEvent @event)
    {
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) == 0) base.onPlaced(@event);

        if (!_lit) return;

        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, id);
    }

    public override void onBreak(OnBreakEvent @event)
    {
        if (!_lit) return;

        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, id);
    }

    public override bool isPoweringSide(IBlockReader world, int x, int y, int z, int side)
    {
        if (!_lit) return false;

        int meta = world.GetBlockMeta(x, y, z);
        return (meta != 5 || side != 1) && (meta != 3 || side != 3) && (meta != 4 || side != 2) && (meta != 1 || side != 5) && (meta != 2 || side != 4);
    }

    private static bool shouldUnpower(OnTickEvent @event)
    {
        (int x, int y, int z) = (@event.X, @event.Y, @event.Z);
        RedstoneEngine redstoneEngine = @event.World.Redstone;
        int meta = @event.World.Reader.GetBlockMeta(x, y, z);
        return (meta == 5 && redstoneEngine.IsPoweringSide(x, y - 1, z, 0)) || (meta == 3 && redstoneEngine.IsPoweringSide(x, y, z - 1, 2)) ||
               (meta == 4 && redstoneEngine.IsPoweringSide(x, y, z + 1, 3)) || (meta == 1 && redstoneEngine.IsPoweringSide(x - 1, y, z, 4)) || (meta == 2 && redstoneEngine.IsPoweringSide(x + 1, y, z, 5));
    }

    public override void onTick(OnTickEvent @event)
    {
        (int x, int y, int z) = (@event.X, @event.Y, @event.Z);
        bool shouldTurnOff = shouldUnpower(@event);

        long currentTime = @event.World.GetTime();

        lock (_updateLock)
        {
            while (s_torchUpdates.Count > 0 && currentTime - s_torchUpdates[0].updateTime > 60L)
            {
                s_torchUpdates.RemoveAt(0);
            }
        }

        if (_lit)
        {
            if (!shouldTurnOff) return;

            @event.World.Writer.SetBlock(x, y, z, RedstoneTorch.id, @event.World.Reader.GetBlockMeta(x, y, z));

            if (!isBurnedOut(@event, true, currentTime)) return;

            @event.World.Broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, "random.fizz", 0.5F, 2.6F + (Random.Shared.NextSingle() - Random.Shared.NextSingle()) * 0.8F);

            for (int particleIndex = 0; particleIndex < 5; ++particleIndex)
            {
                double particleX = x + Random.Shared.NextDouble() * 0.6D + 0.2D;
                double particleY = y + Random.Shared.NextDouble() * 0.6D + 0.2D;
                double particleZ = z + Random.Shared.NextDouble() * 0.6D + 0.2D;
                @event.World.Broadcaster.AddParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }

            int spatialBias = (x + y + z) % 3;
            @event.World.TickScheduler.ScheduleBlockUpdate(x, y, z, RedstoneTorch.id, 160 + spatialBias);
        }
        else if (!shouldTurnOff && !isBurnedOut(@event, false, currentTime))
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
        if (!_lit) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        double particleX = @event.X + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleY = @event.Y + 0.7F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleZ = @event.Z + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        switch (meta)
        {
            case 1:
                @event.World.Broadcaster.AddParticle("reddust", particleX - HorizontalOffset, particleY + VerticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
                break;
            case 2:
                @event.World.Broadcaster.AddParticle("reddust", particleX + HorizontalOffset, particleY + VerticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
                break;
            case 3:
                @event.World.Broadcaster.AddParticle("reddust", particleX, particleY + VerticalOffset, particleZ - HorizontalOffset, 0.0D, 0.0D, 0.0D);
                break;
            case 4:
                @event.World.Broadcaster.AddParticle("reddust", particleX, particleY + VerticalOffset, particleZ + HorizontalOffset, 0.0D, 0.0D, 0.0D);
                break;
            default:
                @event.World.Broadcaster.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                break;
        }
    }
}
