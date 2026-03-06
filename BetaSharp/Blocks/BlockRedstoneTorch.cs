using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockRedstoneTorch : BlockTorch
{

    private readonly bool _lit = false;
    private static readonly ThreadLocal<List<RedstoneUpdateInfo>> s_torchUpdates = new(() => []);

    public override int getTexture(int side, int meta)
    {
        return side == 1 ? Block.RedstoneWire.getTexture(side, meta) : base.getTexture(side, meta);
    }

    private bool isBurnedOut(World world, int x, int y, int z, bool recordUpdate)
    {
        List<RedstoneUpdateInfo> updates = s_torchUpdates.Value!;
        if (recordUpdate)
        {
            updates.Add(new RedstoneUpdateInfo(x, y, z, world.GetTime()));
        }

        int updateCount = 0;

        for (int i = 0; i < updates.Count; ++i)
        {
            RedstoneUpdateInfo updateInfo = updates[i];
            if (updateInfo.x == x && updateInfo.y == y && updateInfo.z == z)
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

    public BlockRedstoneTorch(int id, int textureId, bool lit) : base(id, textureId)
    {
        _lit = lit;
        setTickRandomly(true);
    }

    public override int getTickRate()
    {
        return 2;
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        if (world.GetBlockMeta(x, y, z) == 0)
        {
            base.onPlaced(world, x, y, z);
        }

        if (_lit)
        {
            world.NotifyNeighbors(x, y - 1, z, id);
            world.NotifyNeighbors(x, y + 1, z, id);
            world.NotifyNeighbors(x - 1, y, z, id);
            world.NotifyNeighbors(x + 1, y, z, id);
            world.NotifyNeighbors(x, y, z - 1, id);
            world.NotifyNeighbors(x, y, z + 1, id);
        }

    }

    public override void onBreak(World world, int x, int y, int z)
    {
        if (_lit)
        {
            world.NotifyNeighbors(x, y - 1, z, id);
            world.NotifyNeighbors(x, y + 1, z, id);
            world.NotifyNeighbors(x - 1, y, z, id);
            world.NotifyNeighbors(x + 1, y, z, id);
            world.NotifyNeighbors(x, y, z - 1, id);
            world.NotifyNeighbors(x, y, z + 1, id);
        }

    }

    public override bool isPoweringSide(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        if (!_lit)
        {
            return false;
        }
        else
        {
            int meta = iBlockAccess.GetBlockMeta(x, y, z);
            return (meta != 5 || side != 1) && ((meta != 3 || side != 3) && ((meta != 4 || side != 2) && ((meta != 1 || side != 5) && (meta != 2 || side != 4))));
        }
    }

    private bool shouldUnpower(World world, int x, int y, int z)
    {
        int meta = world.GetBlockMeta(x, y, z);
        return meta == 5 && world.IsPoweringSide(x, y - 1, z, 0) || (meta == 3 && world.IsPoweringSide(x, y, z - 1, 2) || (meta == 4 && world.IsPoweringSide(x, y, z + 1, 3) || (meta == 1 && world.IsPoweringSide(x - 1, y, z, 4) || meta == 2 && world.IsPoweringSide(x + 1, y, z, 5))));
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        bool shouldTurnOff = shouldUnpower(world, x, y, z);
        List<RedstoneUpdateInfo> updates = s_torchUpdates.Value!;

        while (updates.Count > 0 && world.GetTime() - updates[0].updateTime > 100L)
        {
            updates.RemoveAt(0);
        }

        if (_lit)
        {
            if (shouldTurnOff)
            {
                world.SetBlock(x, y, z, Block.RedstoneTorch.id, world.GetBlockMeta(x, y, z));
                if (isBurnedOut(world, x, y, z, true))
                {
                    world.PlaySound((double)((float)x + 0.5F), (double)((float)y + 0.5F), (double)((float)z + 0.5F), "random.fizz", 0.5F, 2.6F + (world.random.NextFloat() - world.random.NextFloat()) * 0.8F);

                    for (int particleIndex = 0; particleIndex < 5; ++particleIndex)
                    {
                        double particleX = (double)x + random.NextDouble() * 0.6D + 0.2D;
                        double particleY = (double)y + random.NextDouble() * 0.6D + 0.2D;
                        double particleZ = (double)z + random.NextDouble() * 0.6D + 0.2D;
                        world.AddParticle("smoke", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
                    }
                }
            }
        }
        else if (!shouldTurnOff && !isBurnedOut(world, x, y, z, false))
        {
            world.SetBlock(x, y, z, Block.LitRedstoneTorch.id, world.GetBlockMeta(x, y, z));
        }

    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        base.neighborUpdate(world, x, y, z, id);
        world.TickScheduler.ScheduleBlockUpdate(x, y, z, base.id, getTickRate());
    }

    public override bool isStrongPoweringSide(World world, int x, int y, int z, int side)
    {
        return side == 0 && isPoweringSide(world, x, y, z, side);
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return Block.LitRedstoneTorch.id;
    }

    public override bool canEmitRedstonePower()
    {
        return true;
    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (_lit)
        {
            int meta = world.GetBlockMeta(x, y, z);
            double particleX = (double)((float)x + 0.5F) + (double)(random.NextFloat() - 0.5F) * 0.2D;
            double particleY = (double)((float)y + 0.7F) + (double)(random.NextFloat() - 0.5F) * 0.2D;
            double particleZ = (double)((float)z + 0.5F) + (double)(random.NextFloat() - 0.5F) * 0.2D;
            double verticalOffset = (double)0.22F;
            double horizontalOffset = (double)0.27F;
            if (meta == 1)
            {
                world.AddParticle("reddust", particleX - horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 2)
            {
                world.AddParticle("reddust", particleX + horizontalOffset, particleY + verticalOffset, particleZ, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 3)
            {
                world.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ - horizontalOffset, 0.0D, 0.0D, 0.0D);
            }
            else if (meta == 4)
            {
                world.AddParticle("reddust", particleX, particleY + verticalOffset, particleZ + horizontalOffset, 0.0D, 0.0D, 0.0D);
            }
            else
            {
                world.AddParticle("reddust", particleX, particleY, particleZ, 0.0D, 0.0D, 0.0D);
            }

        }
    }
}
