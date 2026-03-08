using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

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

    public override void onPlaced(World world, int x, int y, int z)
    {
        if (world.getBlockMeta(x, y, z) == 0)
        {
            base.onPlaced(world, x, y, z);
        }

        if (_lit)
        {
            world.notifyNeighbors(x, y - 1, z, id);
            world.notifyNeighbors(x, y + 1, z, id);
            world.notifyNeighbors(x - 1, y, z, id);
            world.notifyNeighbors(x + 1, y, z, id);
            world.notifyNeighbors(x, y, z - 1, id);
            world.notifyNeighbors(x, y, z + 1, id);
        }
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        if (_lit)
        {
            world.notifyNeighbors(x, y - 1, z, id);
            world.notifyNeighbors(x, y + 1, z, id);
            world.notifyNeighbors(x - 1, y, z, id);
            world.notifyNeighbors(x + 1, y, z, id);
            world.notifyNeighbors(x, y, z - 1, id);
            world.notifyNeighbors(x, y, z + 1, id);
        }
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        if (!_lit)
        {
            return false;
        }

        int meta = iBlockReader.getBlockMeta(x, y, z);
        return (meta != 5 || side != 1) && (meta != 3 || side != 3) && (meta != 4 || side != 2) && (meta != 1 || side != 5) && (meta != 2 || side != 4);
    }

    private bool shouldUnpower(OnTickEvt ctx)
    {
        int x = ctx.X;
        int y = ctx.Y;
        int z = ctx.Z;
        RedstoneEngine redstoneEngine = ctx.Redstone;
        int meta = ctx.WorldRead.getBlockMeta(x, y, z);
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
                ctx.WorldWrite.SetBlock(x, y, z, RedstoneTorch.id, ctx.WorldRead.getBlockMeta(x, y, z));
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
            ctx.WorldRead.setBlock(x, y, z, LitRedstoneTorch.id, ctx.WorldRead.getBlockMeta(x, y, z));
        }
    }

    public override void neighborUpdate(WorldBlockView world, int x, int y, int z, int id)
    {
        base.neighborUpdate(world, x, y, z, id);
        world.ScheduleBlockUpdate(x, y, z, this.id, getTickRate());
    }

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => side == 0 && isPoweringSide(world, x, y, z, side);

    public override int getDroppedItemId(int blockMeta) => LitRedstoneTorch.id;

    public override bool canEmitRedstonePower() => true;

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (_lit)
        {
            int meta = world.getBlockMeta(x, y, z);
            double particleX = x + 0.5F + (random.NextFloat() - 0.5F) * 0.2D;
            double particleY = y + 0.7F + (random.NextFloat() - 0.5F) * 0.2D;
            double particleZ = z + 0.5F + (random.NextFloat() - 0.5F) * 0.2D;
            double verticalOffset = 0.22F;
            double horizontalOffset = 0.27F;
            if (meta == 1)
            {
                world.addParticle("reddust", particleX - horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 2)
            {
                world.addParticle("reddust", particleX + horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 3)
            {
                world.addParticle("reddust", particleX, particleY + verticalOffset, particleZ - horizontalOffset, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 4)
            {
                world.addParticle("reddust", particleX, particleY + verticalOffset, particleZ + horizontalOffset, 0.0D, 0.0D, 0.0D);
            }
            else
            {
                world.addParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }
        }
    }
}
