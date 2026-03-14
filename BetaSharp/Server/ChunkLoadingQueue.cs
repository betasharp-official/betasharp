using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Server;

internal class ChunkLoadingQueue(ChunkMap chunkMap)
{
    private readonly ChunkMap _chunkMap = chunkMap;
    private readonly Dictionary<long, PendingChunk> _pendingChunks = [];
    //TODO: MAKE THIS CONFIGURABLE
    private const int MAX_CHUNKS_PER_TICK = 5;
    private long _nextSequence;

    public void Add(int x, int z, ServerPlayerEntity player)
    {
        long hash = ChunkMap.GetChunkHash(x, z);

        if (_pendingChunks.TryGetValue(hash, out var pending))
        {
            pending.AddPlayer(player);
        }
        else
        {
            _pendingChunks[hash] = new PendingChunk(hash, x, z, _nextSequence++, player);
        }
    }

    public void RemovePlayer(ServerPlayerEntity player)
    {
        var toRemove = new List<long>();
        foreach ((long hash, PendingChunk chunk) in _pendingChunks)
        {
            chunk.RemovePlayer(player);
            if (chunk.IsEmpty)
            {
                toRemove.Add(hash);
            }
        }
        foreach (long hash in toRemove)
        {
            _pendingChunks.Remove(hash);
        }
    }

    public void Remove(int x, int z, ServerPlayerEntity player)
    {
        long hash = ChunkMap.GetChunkHash(x, z);
        if (!_pendingChunks.TryGetValue(hash, out PendingChunk? pending))
        {
            return;
        }

        pending.RemovePlayer(player);
        if (pending.IsEmpty)
        {
            _pendingChunks.Remove(hash);
        }
    }

    public void Tick()
    {
        if (_pendingChunks.Count == 0) return;

        // Convert to list once and sort once — avoids repeated full-sort of the backing store.
        var sortedChunks = new List<PendingChunk>(_pendingChunks.Values);
        sortedChunks.Sort((a, b) => a.GetPriority().CompareTo(b.GetPriority()));

        int chunksLoaded = 0;
        foreach (PendingChunk chunkToLoad in sortedChunks)
        {
            if (chunksLoaded >= MAX_CHUNKS_PER_TICK) break;

            _pendingChunks.Remove(chunkToLoad.Hash);

            ChunkMap.TrackedChunk? chunk = _chunkMap.GetOrCreateChunk(chunkToLoad.X, chunkToLoad.Z, true);
            if (chunk != null)
            {
                foreach (var player in chunkToLoad.Players)
                {
                    if (!chunk.HasPlayer(player))
                    {
                        chunk.addPlayer(player);
                    }
                }
            }
            chunksLoaded++;
        }
    }

    private class PendingChunk
    {
        public long Hash { get; }
        public int X { get; }
        public int Z { get; }
        public ChunkPos ChunkPos { get; }
        public HashSet<ServerPlayerEntity> Players { get; } = [];
        public long Sequence { get; }
        public bool IsEmpty => Players.Count == 0;

        public PendingChunk(long hash, int x, int z, long sequence, ServerPlayerEntity initiator)
        {
            Hash = hash;
            X = x;
            Z = z;
            ChunkPos = new ChunkPos(x, z);
            Sequence = sequence;
            AddPlayer(initiator);
        }

        public void AddPlayer(ServerPlayerEntity player)
        {
            Players.Add(player);
        }

        public void RemovePlayer(ServerPlayerEntity player)
        {
            Players.Remove(player);
        }

        public ChunkPriority GetPriority()
        {
            bool hasAny = false;
            ChunkPriority bestPriority = default;

            foreach (var player in Players)
            {
                ChunkPriority candidate = player.GetChunkPriority(ChunkPos, Sequence);
                if (!hasAny || candidate.CompareTo(bestPriority) < 0)
                {
                    bestPriority = candidate;
                    hasAny = true;
                }
            }

            return hasAny
                ? bestPriority
                : new ChunkPriority(int.MaxValue, double.MaxValue, Sequence);
        }
    }
}
