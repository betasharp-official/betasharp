using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockStationary : BlockFluid
{
    public BlockStationary(int id, Material material) : base(id, material)
    {
        setTickRandomly(false);
        if (material == Material.Lava) setTickRandomly(true);
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        base.neighborUpdate(@event);
        if (@event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z) != id) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        @event.World.Writer.SetBlockWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, id - 1, meta, false);
        @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, id - 1, getTickRate());
    }

    private void convertToFlowing(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        @event.World.Writer.SetBlockWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, id - 1, meta, false);
        @event.World.Broadcaster.SetBlocksDirty(@event.X, @event.Y, @event.Z, @event.X, @event.Y, @event.Z);
        @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, id - 1, getTickRate());
    }

    public override void onTick(OnTickEvent @event)
    {
        (int x, int y, int z) = (@event.X, @event.Y, @event.Z);
        if (@event.World.Reader.GetBlockId(x, y, z) == id) convertToFlowing(@event);

        if (material != Material.Lava) return;

        int attempts = @event.World.Random.NextInt(3);

        for (int attempt = 0; attempt < attempts; ++attempt)
        {
            x += @event.World.Random.NextInt(3) - 1;
            ++y;
            z += @event.World.Random.NextInt(3) - 1;
            int neighborBlockId = @event.World.Reader.GetBlockId(x, y, z);
            if (neighborBlockId == 0)
            {
                if (!isFlammable(@event.World.Reader, x - 1, y, z) && !isFlammable(@event.World.Reader, x + 1, y, z) && !isFlammable(@event.World.Reader, x, y, z - 1) &&
                    !isFlammable(@event.World.Reader, x, y, z + 1) && !isFlammable(@event.World.Reader, x, y - 1, z) && !isFlammable(@event.World.Reader, x, y + 1, z))
                {
                    continue;
                }

                @event.World.Writer.SetBlock(x, y, z, Fire.id);
                return;
            }

            if (Blocks[neighborBlockId].material.BlocksMovement)
            {
                return;
            }
        }
    }

    private static bool isFlammable(IBlockReader world, int x, int y, int z) => world.GetMaterial(x, y, z).IsBurnable;
}
