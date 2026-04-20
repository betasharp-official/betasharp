using BetaSharp.Blocks;

namespace BetaSharp.Tests.Blocks;

public record InstantUpdate(int X, int Y, int Z, int BlockId);

public record ScheduledTick(int X, int Y, int Z, int BlockId, long TargetTick);

public record UpdateKey(int X, int Y, int Z, int BlockId);

public class SimulationRunner
{
    private const int MaxInstantUpdatesPerTick = 100_000;
    private const int MaxScheduledExecutionsPerAdvance = 1_000_000;
    private readonly Queue<InstantUpdate> _instantQueue = new();

    private readonly PriorityQueue<ScheduledTick, (long targetTick, long sequence)> _scheduledQueue = new();
    private readonly FakeWorldContext _world;

    private int _importedScheduledCount;
    private long _sequenceCounter;

    public SimulationRunner(FakeWorldContext world) => _world = world;

    public long CurrentWorldTime { get; private set; }

    public void EnqueueInstantUpdate(int x, int y, int z, int blockId) => _instantQueue.Enqueue(new InstantUpdate(x, y, z, blockId));

    public void ScheduleUpdate(int x, int y, int z, int blockId, int delay)
    {
        long target = CurrentWorldTime + delay;
        _scheduledQueue.Enqueue(
            new ScheduledTick(x, y, z, blockId, target),
            (target, _sequenceCounter++)
        );
    }

    public void ProcessInstantQueue()
    {
        DrainInstantUpdates();
        PullScheduledTicksFromWorldSpy();
    }

    public void AdvanceTime(long ticks)
    {
        long targetTime = CurrentWorldTime + ticks;
        int scheduledExecutions = 0;

        while (CurrentWorldTime <= targetTime)
        {
            PullScheduledTicksFromWorldSpy();
            DrainInstantUpdates();
            PullScheduledTicksFromWorldSpy();

            while (_scheduledQueue.TryPeek(out _, out (long targetTick, long sequence) priority) && priority.targetTick == CurrentWorldTime)
            {
                if (++scheduledExecutions > MaxScheduledExecutionsPerAdvance)
                {
                    throw new InvalidOperationException($"Simulation trapped in scheduled update loop. Exceeded {MaxScheduledExecutionsPerAdvance} executions.");
                }

                ScheduledTick tick = _scheduledQueue.Dequeue();
                int blockId = _world.Reader.GetBlockId(tick.X, tick.Y, tick.Z);
                if (blockId <= 0 || blockId != tick.BlockId)
                {
                    continue;
                }

                int meta = _world.Reader.GetBlockMeta(tick.X, tick.Y, tick.Z);
                Block.Blocks[blockId].OnTick(new OnTickEvent(_world, tick.X, tick.Y, tick.Z, meta, blockId));

                PullScheduledTicksFromWorldSpy();
                DrainInstantUpdates();
                PullScheduledTicksFromWorldSpy();
            }

            CurrentWorldTime++;
        }
    }

    private void DrainInstantUpdates()
    {
        int instantExecutions = 0;
        HashSet<UpdateKey> processedThisPhase = new();

        while (_instantQueue.TryDequeue(out InstantUpdate? update))
        {
            if (++instantExecutions > MaxInstantUpdatesPerTick)
            {
                throw new InvalidOperationException($"Simulation trapped in instant update storm. Exceeded {MaxInstantUpdatesPerTick} updates.");
            }

            UpdateKey key = new(update.X, update.Y, update.Z, update.BlockId);

            // Optional: Deduplication guard to prevent pathological re-enqueues in the same tick
            if (!processedThisPhase.Add(key))
            {
                continue;
            }

            int blockId = _world.Reader.GetBlockId(update.X, update.Y, update.Z);
            if (blockId <= 0)
            {
                continue;
            }

            int meta = _world.Reader.GetBlockMeta(update.X, update.Y, update.Z);
            Block.Blocks[blockId].NeighborUpdate(new OnTickEvent(_world, update.X, update.Y, update.Z, meta, update.BlockId));
        }
    }

    private void PullScheduledTicksFromWorldSpy()
    {
        IReadOnlyList<(int X, int Y, int Z, int BlockId, int TickRate)> scheduled = _world.TickSchedulerSpy.ScheduledTicks;
        while (_importedScheduledCount < scheduled.Count)
        {
            (int x, int y, int z, int blockId, int delay) = scheduled[_importedScheduledCount++];
            ScheduleUpdate(x, y, z, blockId, delay);
        }
    }
}
