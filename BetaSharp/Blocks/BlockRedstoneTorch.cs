using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockRedstoneTorch : BlockTorch
{
    private static readonly ThreadLocal<List<RedstoneUpdateInfo>> s_torchUpdates = new(() => []);
    private readonly bool _lit;

    public BlockRedstoneTorch(int id, int textureId, bool lit) : base(id, textureId)
    {
        _lit = lit;
        SetTickRandomly(true);
    }

    public override int GetTexture(Side side, int meta) => side == Side.Up ? RedstoneWire.GetTexture(side, meta) : base.GetTexture(side, meta);

    private bool isBurnedOut(OnTickEvent ctx, bool recordUpdate)
    {
        List<RedstoneUpdateInfo> updates = s_torchUpdates.Value!;
        if (recordUpdate)
        {
            updates.Add(new RedstoneUpdateInfo(ctx.X, ctx.Y, ctx.Z, ctx.World.GetTime()));
        }

        int updateCount = 0;

        foreach (RedstoneUpdateInfo _ in updates.Where(updateInfo => updateInfo.x == ctx.X && updateInfo.y == ctx.Y && updateInfo.z == ctx.Z))
        {
            ++updateCount;
            if (updateCount >= 8)
            {
                return true;
            }
        }

        return false;
    }

    public override int GetTickRate() => 2;

    public override void OnPlaced(OnPlacedEvent @event)
    {
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) == 0)
        {
            base.OnPlaced(@event);
        }

        if (!_lit)
        {
            return;
        }

        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, Id);
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        if (!_lit)
        {
            return;
        }

        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y + 1, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, Id);
        @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, Id);
    }

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side)
    {
        if (!_lit)
        {
            return false;
        }

        int meta = reader.GetBlockMeta(x, y, z);
        return (meta != 5 || side != (int)Side.Up) && (meta != 3 || side != (int)Side.South) && (meta != 4 || side != (int)Side.North) && (meta != 1 || side != (int)Side.East) && (meta != 2 || side != (int)Side.West);
    }

    private bool shouldUnpower(OnTickEvent @event)
    {
        int x = @event.X;
        int y = @event.Y;
        int z = @event.Z;
        RedstoneEngine redstoneEngine = @event.World.Redstone;
        int meta = @event.World.Reader.GetBlockMeta(x, y, z);
        return (meta == 5 && redstoneEngine.IsPoweringSide(x, y - 1, z, (int)Side.Down)) || (meta == 3 && redstoneEngine.IsPoweringSide(x, y, z - 1, (int)Side.North)) ||
               (meta == 4 && redstoneEngine.IsPoweringSide(x, y, z + 1, (int)Side.South)) || (meta == 1 && redstoneEngine.IsPoweringSide(x - 1, y, z, (int)Side.West)) ||
               (meta == 2 && redstoneEngine.IsPoweringSide(x + 1, y, z, (int)Side.East));
    }

    public override void OnTick(OnTickEvent @event)
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
            if (!shouldTurnOff) return;

            @event.World.Writer.SetBlock(x, y, z, RedstoneTorch.Id, @event.World.Reader.GetBlockMeta(x, y, z));

            if (!isBurnedOut(@event, true)) return;

            @event.World.Broadcaster.PlaySoundAtPos(x + 0.5F, y + 0.5F, z + 0.5F, "random.fizz", 0.5F, 2.6F + (Random.Shared.NextSingle() - Random.Shared.NextSingle()) * 0.8F);

            for (int particleIndex = 0; particleIndex < 5; ++particleIndex)
            {
                double particleX = x + Random.Shared.NextDouble() * 0.6D + 0.2D;
                double particleY = y + Random.Shared.NextDouble() * 0.6D + 0.2D;
                double particleZ = z + Random.Shared.NextDouble() * 0.6D + 0.2D;
                @event.World.Broadcaster.AddParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }
        }
        else if (!shouldTurnOff && !isBurnedOut(@event, false))
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, LitRedstoneTorch.Id, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z));
        }
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        base.NeighborUpdate(@event);
        @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, Id, GetTickRate());
    }

    public override bool IsStrongPoweringSide(IBlockReader reader, int x, int y, int z, int side) => side == (int)Side.Down && IsPoweringSide(reader, x, y, z, side);

    public override int GetDroppedItemId(int blockMeta) => LitRedstoneTorch.Id;

    public override bool CanEmitRedstonePower() => true;

    public override void RandomDisplayTick(OnTickEvent @event)
    {
        if (!_lit) return;

        const double verticalOffset = 0.22F;
        const double horizontalOffset = 0.27F;
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        double particleX = @event.X + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleY = @event.Y + 0.7F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        double particleZ = @event.Z + 0.5F + (Random.Shared.NextSingle() - 0.5F) * 0.2D;
        switch (meta)
        {
            case 1:
                @event.World.Broadcaster.AddParticle("reddust", particleX - horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
                break;
            case 2:
                @event.World.Broadcaster.AddParticle("reddust", particleX + horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
                break;
            case 3:
                @event.World.Broadcaster.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ - horizontalOffset, 0.0D, 0.0D, 0.0D);
                break;
            case 4:
                @event.World.Broadcaster.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ + horizontalOffset, 0.0D, 0.0D, 0.0D);
                break;
            default:
                @event.World.Broadcaster.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                break;
        }
    }
}
