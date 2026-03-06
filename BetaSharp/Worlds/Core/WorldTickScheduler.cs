using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Core;

public class WorldTickScheduler
{
    private readonly World _world;

    private long _absoluteTickCounter = 0;

    private readonly PriorityQueue<BlockUpdate, (long, long)> _scheduledUpdates = new();

    public bool instantBlockUpdateEnabled = false;

    public WorldTickScheduler(World world)
    {
        _world = world;
    }

    public void Tick(bool forceFlush = false)
    {
        _absoluteTickCounter++;
        ProcessScheduledTicks(forceFlush);
    }

    public void ScheduleBlockUpdate(int x, int y, int z, int blockId, int tickRate)
    {
        if (_world.IsRemote) return;

        const byte loadRadius = 8;
        if (_world.IsRegionLoaded(x - loadRadius, y - loadRadius, z - loadRadius, x + loadRadius, y + loadRadius, z + loadRadius))
        {
            if (instantBlockUpdateEnabled)
            {
                int currentBlockId = _world.GetBlockId(x, y, z);
                if (currentBlockId == blockId && currentBlockId > 0)
                {
                    Block.Blocks[currentBlockId].onTick(_world, x, y, z, _world.random);
                }
            }
            else
            {
                long scheduledTime = _absoluteTickCounter + tickRate;
                BlockUpdate blockUpdate = new(x, y, z, blockId, scheduledTime);
                _scheduledUpdates.Enqueue(blockUpdate, (blockUpdate.ScheduledTime, blockUpdate.ScheduledOrder));
            }
        }
    }

    private void ProcessScheduledTicks(bool forceFlush)
    {
        if (_world.IsRemote) return;

        for (int i = 0; i < 1000; ++i)
        {
            if (_scheduledUpdates.Count == 0) break;

            if (!forceFlush && _scheduledUpdates.Peek().ScheduledTime > _absoluteTickCounter) break;

            var blockUpdate = _scheduledUpdates.Dequeue();

            const byte loadRadius = 8;
            if (_world.IsRegionLoaded(
                    blockUpdate.X - loadRadius,
                    blockUpdate.Y - loadRadius,
                    blockUpdate.Z - loadRadius,
                    blockUpdate.X + loadRadius,
                    blockUpdate.Y + loadRadius,
                    blockUpdate.Z + loadRadius
                )
               )
            {
                int currentBlockId = _world.GetBlockId(blockUpdate.X, blockUpdate.Y, blockUpdate.Z);
                if (currentBlockId == blockUpdate.BlockId && currentBlockId > 0)
                {
                    Block.Blocks[currentBlockId].onTick(_world, blockUpdate.X, blockUpdate.Y, blockUpdate.Z, _world.random);
                }
            }
        }
    }
}
