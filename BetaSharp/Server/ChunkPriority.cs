using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Server;

internal readonly struct ChunkPriority(int ring, double directionPenalty, long sequence) : IComparable<ChunkPriority>
{
    public int Ring { get; } = ring;
    public double DirectionPenalty { get; } = directionPenalty;
    public long Sequence { get; } = sequence;

    public int CompareTo(ChunkPriority other)
    {
        return (Ring, DirectionPenalty, Sequence).CompareTo((other.Ring, other.DirectionPenalty, other.Sequence));
    }
}

internal sealed class PlayerChunkSendQueue
{
    private readonly Dictionary<ChunkPos, QueueEntry> _entries = [];
    private readonly List<QueueEntry> _orderedEntries = [];
    private long _nextSequence;
    private bool _dirty = true;

    public int Count => _entries.Count;

    public void Clear()
    {
        _entries.Clear();
        _orderedEntries.Clear();
        _dirty = true;
    }

    public void EnqueueOrPromote(ServerPlayerEntity player, ChunkPos chunkPos)
    {
        if (_entries.TryGetValue(chunkPos, out QueueEntry? entry))
        {
            entry.Priority = player.GetChunkPriority(chunkPos, entry.Sequence);
        }
        else
        {
            entry = new QueueEntry(chunkPos, _nextSequence++);
            entry.Priority = player.GetChunkPriority(chunkPos, entry.Sequence);
            _entries[chunkPos] = entry;
        }

        _dirty = true;
    }

    public void Remove(ChunkPos chunkPos)
    {
        if (_entries.Remove(chunkPos))
        {
            _dirty = true;
        }
    }

    public void ReprioritizeAll(ServerPlayerEntity player)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        foreach (QueueEntry entry in _entries.Values)
        {
            entry.Priority = player.GetChunkPriority(entry.ChunkPos, entry.Sequence);
        }

        _dirty = true;
    }

    public bool TryDequeue(out ChunkPos chunkPos)
    {
        EnsureOrdered();

        if (_orderedEntries.Count == 0)
        {
            chunkPos = default;
            return false;
        }

        int lastIndex = _orderedEntries.Count - 1;
        QueueEntry entry = _orderedEntries[lastIndex];
        _orderedEntries.RemoveAt(lastIndex);
        _entries.Remove(entry.ChunkPos);
        chunkPos = entry.ChunkPos;
        return true;
    }

    private void EnsureOrdered()
    {
        if (!_dirty)
        {
            return;
        }

        _orderedEntries.Clear();
        _orderedEntries.AddRange(_entries.Values);
        _orderedEntries.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        _dirty = false;
    }

    private sealed class QueueEntry(ChunkPos chunkPos, long sequence)
    {
        public ChunkPos ChunkPos { get; } = chunkPos;
        public long Sequence { get; } = sequence;
        public ChunkPriority Priority { get; set; } = new(int.MaxValue, 0.0, sequence);
    }
}
