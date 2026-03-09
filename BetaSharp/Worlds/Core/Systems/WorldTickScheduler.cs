using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Core.Systems;

public class WorldTickScheduler
{
    private readonly WorldBlockView _blockView;
    private readonly BlockHost _host;
    private readonly JavaRandom _random;
    private readonly bool _isRemote;
    private readonly WorldEventBroadcaster _broadcaster;
    private long _absoluteTickCounter = 0;
    private readonly PriorityQueue<BlockUpdate, (long, long)> _scheduledUpdates = new();
    public bool instantBlockUpdateEnabled = false;

    public WorldTickScheduler(WorldBlockView blockView, BlockHost host, JavaRandom random, bool isRemote, WorldEventBroadcaster broadcaster)
    {
        _blockView = blockView;
        _host = host;
        _random = random;
        _isRemote = isRemote;
        _broadcaster = broadcaster;
    }

    public void Tick(bool forceFlush = false)
    {
        _absoluteTickCounter++;
        ProcessScheduledTicks(forceFlush);
    }

    public void ScheduleBlockUpdate(int x, int y, int z, int blockId, int tickRate)
    {
        if (_isRemote) return;

        const byte loadRadius = 8;
        if (_host.IsRegionLoaded(x - loadRadius, y - loadRadius, z - loadRadius, x + loadRadius, y + loadRadius, z + loadRadius))
        {
            if (instantBlockUpdateEnabled)
            {
                int currentBlockId = _blockView.GetBlockId(x, y, z);
                if (currentBlockId == blockId && currentBlockId > 0)
                {
                    Block.Blocks[currentBlockId].onTick(_blockView, x, y, z, _random, _broadcaster, _isRemote);
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
        if (_isRemote) return;

        for (int i = 0; i < 1000; ++i)
        {
            if (_scheduledUpdates.Count == 0) break;

            if (!forceFlush && _scheduledUpdates.Peek().ScheduledTime > _absoluteTickCounter) break;

            var blockUpdate = _scheduledUpdates.Dequeue();

            const byte loadRadius = 8;
            if (_host.IsRegionLoaded(
                    blockUpdate.X - loadRadius,
                    blockUpdate.Y - loadRadius,
                    blockUpdate.Z - loadRadius,
                    blockUpdate.X + loadRadius,
                    blockUpdate.Y + loadRadius,
                    blockUpdate.Z + loadRadius
                )
               )
            {
                int currentBlockId = _blockView.GetBlockId(blockUpdate.X, blockUpdate.Y, blockUpdate.Z);
                if (currentBlockId == blockUpdate.BlockId && currentBlockId > 0)
                {
                    Block.Blocks[currentBlockId].onTick(_blockView, blockUpdate.X, blockUpdate.Y, blockUpdate.Z, _random, _broadcaster, _isRemote);
                }
            }
        }
    }
}
