using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

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

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (!world.isRemote)
        {
            int meta = world.getBlockMeta(x, y, z);
            if ((meta & 8) != 0)
            {
                updatePoweredStatus(world, x, y, z, meta);
            }
        }
    }

    public override bool isPoweringSide(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        return (iBlockAccess.getBlockMeta(x, y, z) & 8) != 0;
    }

    public override bool isStrongPoweringSide(World world, int x, int y, int z, int side)
    {
        return (world.getBlockMeta(x, y, z) & 8) == 0 ? false : side == 1;
    }

    private void updatePoweredStatus(World world, int x, int y, int z, int meta)
    {
        bool isPowered = (meta & 8) != 0;
        bool hasMinecart = false;
        float detectionInset = 2.0F / 16.0F;
        var minecartsOnRail = world.CollectEntitiesOfType<EntityMinecart>(new Box((double)((float)x + detectionInset), (double)y, (double)((float)z + detectionInset), (double)((float)(x + 1) - detectionInset), (double)y + 0.25D, (double)((float)(z + 1) - detectionInset)));
        if (minecartsOnRail.Count > 0)
        {
            hasMinecart = true;
        }

        if (hasMinecart && !isPowered)
        {
            world.setBlockMeta(x, y, z, meta | 8);
            world.notifyNeighbors(x, y, z, id);
            world.notifyNeighbors(x, y - 1, z, id);
            world.setBlocksDirty(x, y, z, x, y, z);
        }

        if (!hasMinecart && isPowered)
        {
            world.setBlockMeta(x, y, z, meta & 7);
            world.notifyNeighbors(x, y, z, id);
            world.notifyNeighbors(x, y - 1, z, id);
            world.setBlocksDirty(x, y, z, x, y, z);
        }

        if (hasMinecart)
        {
            world.ScheduleBlockUpdate(x, y, z, id, getTickRate());
        }

    }
}
