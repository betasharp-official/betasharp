using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Core.Systems;

public class WorldTickScheduler
{
    private readonly IWorldContext _context;
    private readonly PriorityQueue<BlockUpdate, (long, long)> _scheduledUpdates = new();

    public WorldTickScheduler(IWorldContext context) => _context = context;
    public void Tick(bool forceFlush = false) => ProcessScheduledTicks(forceFlush);

    /// <summary>
    ///     Schedules a block update when restoring from chunk NBT (TileTicks). Skips the load-radius check
    ///     because Chunk.Load() runs when the chunk is being loaded—neighbor chunks for the ±8 block radius
    ///     may not be loaded yet, which would cause ticks to be silently dropped.
    /// </summary>
    public void ScheduleBlockUpdateFromChunkLoad(int x, int y, int z, int blockId, int tickRate)
    {
        long scheduledTime = _context.GetTime() + tickRate;
        BlockUpdate blockUpdate = new(x, y, z, blockId, scheduledTime);
        _scheduledUpdates.Enqueue(blockUpdate, (blockUpdate.ScheduledTime, blockUpdate.ScheduledOrder));
    }

    public virtual void ScheduleBlockUpdate(int x, int y, int z, int blockId, int tickRate, bool instantBlockUpdateEnabled = false)
    {
        const byte loadRadius = 8;
        if (_context.BlockHost.IsPosLoaded(x - loadRadius, y - loadRadius, z - loadRadius) && _context.BlockHost.IsPosLoaded(x + loadRadius, y + loadRadius, z + loadRadius))
        {
            if (instantBlockUpdateEnabled)
            {
                int currentBlockId = _context.Reader.GetBlockId(x, y, z);
                if (currentBlockId == blockId && currentBlockId > 0)
                {
                    int meta = _context.Reader.GetBlockMeta(x, y, z);
                    Block.Blocks[currentBlockId].onTick(new OnTickEvt(_context, x, y, z, meta, currentBlockId));
                }
            }
            else
            {
                long executionTime = _context.GetTime() + tickRate;
                BlockUpdate blockUpdate = new(x, y, z, blockId, executionTime);
                _scheduledUpdates.Enqueue(blockUpdate, (blockUpdate.ScheduledTime, blockUpdate.ScheduledOrder));
            }
        }
    }

    private void ProcessScheduledTicks(bool forceFlush)
    {
        if (_context.IsRemote)
        {
            return;
        }

        long currentTime = _context.GetTime();

        for (int i = 0; i < 1000; ++i)
        {
            if (_scheduledUpdates.Count == 0)
            {
                break;
            }

            BlockUpdate peeked = _scheduledUpdates.Peek();
            if (!forceFlush && peeked.ScheduledTime > currentTime)
            {
                break;
            }

            BlockUpdate blockUpdate = _scheduledUpdates.Dequeue();

            const byte loadRadius = 8;
            bool posLoaded = _context.Reader.IsPosLoaded(blockUpdate.X - loadRadius, blockUpdate.Y - loadRadius, blockUpdate.Z - loadRadius) &&
                             _context.Reader.IsPosLoaded(blockUpdate.X + loadRadius, blockUpdate.Y + loadRadius, blockUpdate.Z + loadRadius);
            if (!posLoaded)
            {
                continue;
            }

            int currentBlockId = _context.Reader.GetBlockId(blockUpdate.X, blockUpdate.Y, blockUpdate.Z);
            if (currentBlockId != blockUpdate.BlockId || currentBlockId <= 0)
            {
                continue;
            }

            Block.Blocks[currentBlockId].onTick(new OnTickEvt(_context, blockUpdate.X, blockUpdate.Y, blockUpdate.Z, _context.Reader.GetBlockMeta(blockUpdate.X, blockUpdate.Y, blockUpdate.Z), currentBlockId));
        }
    }

    public void TriggerInstantTick(int x, int y, int z, int blockId)
    {
        int meta = _context.Reader.GetBlockMeta(x, y, z);
        Block.Blocks[blockId].onTick(new OnTickEvt(_context, x, y, z, meta, blockId));
    }

    /// <summary>
    ///     Returns pending block updates whose (x,z) falls within the given chunk bounds.
    ///     Used for persisting scheduled ticks to chunk NBT (TileTicks) on save.
    /// </summary>
    public IEnumerable<(int X, int Y, int Z, int BlockId, long ScheduledTime)> GetPendingTicksInChunk(int chunkX, int chunkZ)
    {
        int minX = chunkX * 16;
        int maxX = minX + 15;
        int minZ = chunkZ * 16;
        int maxZ = minZ + 15;

        foreach ((BlockUpdate blockUpdate, (long, long) _) in _scheduledUpdates.UnorderedItems)
        {
            if (blockUpdate.X >= minX && blockUpdate.X <= maxX && blockUpdate.Z >= minZ && blockUpdate.Z <= maxZ)
            {
                yield return (blockUpdate.X, blockUpdate.Y, blockUpdate.Z, blockUpdate.BlockId, blockUpdate.ScheduledTime);
            }
        }
    }
}
