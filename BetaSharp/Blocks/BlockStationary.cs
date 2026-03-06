using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
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

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        base.neighborUpdate(world, x, y, z, id);
        if (world.GetBlockId(x, y, z) == base.id)
        {
            convertToFlowing(world, x, y, z);
        }

    }

    private void convertToFlowing(World world, int x, int y, int z)
    {
        int meta = world.GetBlockMeta(x, y, z);
        world.pauseTicking = true;
        world.SetBlockWithoutNotifyingNeighbors(x, y, z, id - 1, meta);
        world.SetBlocksDirty(x, y, z, x, y, z);
        world.TickScheduler.ScheduleBlockUpdate(x, y, z, id - 1, getTickRate());
        world.pauseTicking = false;
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (material == Material.Lava)
        {
            int attempts = random.NextInt(3);

            for (int attempt = 0; attempt < attempts; ++attempt)
            {
                x += random.NextInt(3) - 1;
                ++y;
                z += random.NextInt(3) - 1;
                int neighborBlockId = world.GetBlockId(x, y, z);
                if (neighborBlockId == 0)
                {
                    if (isFlammable(world, x - 1, y, z) || isFlammable(world, x + 1, y, z) || isFlammable(world, x, y, z - 1) || isFlammable(world, x, y, z + 1) || isFlammable(world, x, y - 1, z) || isFlammable(world, x, y + 1, z))
                    {
                        world.SetBlock(x, y, z, Block.Fire.id);
                        return;
                    }
                }
                else if (Block.Blocks[neighborBlockId].material.BlocksMovement)
                {
                    return;
                }
            }
        }

    }

    private bool isFlammable(World world, int x, int y, int z)
    {
        return world.GetMaterial(x, y, z).IsBurnable;
    }
}
