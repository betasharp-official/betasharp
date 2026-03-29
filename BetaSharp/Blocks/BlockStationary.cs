using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockStationary : BlockFluid
{
    public BlockStationary(int id, Material material) : base(id, material)
    {
        SetTickRandomly(false);
        if (material == Material.Lava) SetTickRandomly(true);
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        base.NeighborUpdate(@event);
        if (@event.World.Reader.GetBlockId(@event.X, @event.Y, @event.Z) != Id) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        @event.World.Writer.SetBlockWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, Id - 1, meta, false);
        @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, Id - 1, TickRate);
    }

    private void convertToFlowing(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        @event.World.Writer.SetBlockWithoutNotifyingNeighbors(@event.X, @event.Y, @event.Z, Id - 1, meta, false);
        @event.World.Broadcaster.SetBlocksDirty(@event.X, @event.Y, @event.Z, @event.X, @event.Y, @event.Z);
        @event.World.TickScheduler.ScheduleBlockUpdate(@event.X, @event.Y, @event.Z, Id - 1, TickRate);
    }

    public override void OnTick(OnTickEvent @event)
    {
        (int x, int y, int z) = (@event.X, @event.Y, @event.Z);
        if (@event.World.Reader.GetBlockId(x, y, z) == Id)
        {
            convertToFlowing(@event);
        }

        if (Material != Material.Lava) return;

        int attempts = @event.World.Random.NextInt(3);

        for (int attempt = 0; attempt < attempts; ++attempt)
        {
            x += @event.World.Random.NextInt(3) - 1;
            ++y;
            z += @event.World.Random.NextInt(3) - 1;
            int neighborBlockId = @event.World.Reader.GetBlockId(x, y, z);
            if (neighborBlockId == 0)
            {
                if (IsFlammable(@event.World.Reader, x - 1, y, z) || IsFlammable(@event.World.Reader, x + 1, y, z) || IsFlammable(@event.World.Reader, x, y, z - 1) ||
                    IsFlammable(@event.World.Reader, x, y, z + 1) || IsFlammable(@event.World.Reader, x, y - 1, z) || IsFlammable(@event.World.Reader, x, y + 1, z))
                {
                    @event.World.Writer.SetBlock(x, y, z, Fire.Id);
                    return;
                }
            }
            else if (Blocks[neighborBlockId]!.Material.BlocksMovement)
            {
                return;
            }
        }
    }

    public override bool IsFlammable(IBlockReader world, int x, int y, int z) => world.GetMaterial(x, y, z).IsBurnable;
}
