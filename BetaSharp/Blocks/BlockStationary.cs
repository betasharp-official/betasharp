using BetaSharp.Blocks.Materials;
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

    public override void neighborUpdate(OnTickEvt evt)
    {
        base.neighborUpdate(evt);
        if (evt.Level.BlocksReader.GetBlockId(evt.X, evt.Y, evt.Z) == id)
        {
            int meta = evt.Level.BlocksReader.GetMeta(evt.X, evt.Y, evt.Z);
            evt.Level.BlockWriter.SetBlockWithoutNotifyingNeighbors(evt.X, evt.Y, evt.Z, id - 1, meta, notifyBlockPlaced: false);
            evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, id - 1, getTickRate());
        }
    }

    private void convertToFlowing(OnTickEvt evt)
    {
        int meta = evt.Level.BlocksReader.GetMeta(evt.X, evt.Y, evt.Z);
        evt.Level.BlockWriter.SetBlockWithoutNotifyingNeighbors(evt.X, evt.Y, evt.Z, id - 1, meta, notifyBlockPlaced: false);
        evt.Level.Broadcaster.SetBlocksDirty(evt.X, evt.Y, evt.Z, evt.X, evt.Y, evt.Z);
        evt.Level.TickScheduler.ScheduleBlockUpdate(evt.X, evt.Y, evt.Z, id - 1, getTickRate());
    }

    public override void onTick(OnTickEvt evt)
    {
        if (evt.Level.BlocksReader.GetBlockId(evt.X, evt.Y, evt.Z) == id)
        {
            convertToFlowing(evt);
        }

        if (material == Material.Lava)
        {
            int attempts = evt.Level.random.NextInt(3);

            for (int attempt = 0; attempt < attempts; ++attempt)
            {
                evt.X += evt.Level.random.NextInt(3) - 1;
                ++evt.Y;
                evt.Z += evt.Level.random.NextInt(3) - 1;
                int neighborBlockId = evt.Level.BlocksReader.GetBlockId(evt.X, evt.Y, evt.Z);
                if (neighborBlockId == 0)
                {
                    if (isFlammable(evt.Level.BlocksReader, evt.X - 1, evt.Y, evt.Z) || isFlammable(evt.Level.BlocksReader, evt.X + 1, evt.Y, evt.Z) || isFlammable(evt.Level.BlocksReader, evt.X, evt.Y, evt.Z - 1) ||
                        isFlammable(evt.Level.BlocksReader, evt.X, evt.Y, evt.Z + 1) || isFlammable(evt.Level.BlocksReader, evt.X, evt.Y - 1, evt.Z) || isFlammable(evt.Level.BlocksReader, evt.X, evt.Y + 1, evt.Z))
                    {
                        evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, Fire.id);
                        return;
                    }
                }
                else if (Blocks[neighborBlockId].material.BlocksMovement)
                {
                    return;
                }
            }
        }
    }

    private bool isFlammable(IBlockReader world, int x, int y, int z) => world.GetMaterial(x, y, z).IsBurnable;
}
