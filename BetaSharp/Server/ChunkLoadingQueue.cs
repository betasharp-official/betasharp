using BetaSharp.Entities;

namespace BetaSharp.Server;

internal class ChunkLoadingQueue(ChunkMap chunkMap)
{
    private readonly ChunkMap chunkMap = chunkMap;
    private readonly Dictionary<long, PendingChunk> _pendingChunks = [];
    //TODO: MAKE THIS CONFIGURABLE
    private const int MAX_CHUNKS_PER_TICK = 5;

    public void Add(int x, int z, ServerPlayerEntity player)
    {
        long hash = ChunkMap.GetChunkHash(x, z);

        if (_pendingChunks.TryGetValue(hash, out var pending))
        {
            if (!pending.Players.Contains(player))
            {
                pending.Players.Add(player);
            }
        }
        else
        {
            _pendingChunks[hash] = new PendingChunk(hash, x, z, player);
        }
    }

    public void RemovePlayer(ServerPlayerEntity player)
    {
        var toRemove = new List<long>();
        foreach (var (hash, chunk) in _pendingChunks)
        {
            chunk.Players.Remove(player);
            if (chunk.Players.Count == 0)
            {
                toRemove.Add(hash);
            }
        }
        foreach (var hash in toRemove)
        {
            _pendingChunks.Remove(hash);
        }
    }

    public void Tick()
    {
        if (_pendingChunks.Count == 0) return;

        // Convert to list once and sort once — avoids repeated full-sort of the backing store.
        var sortedChunks = new List<PendingChunk>(_pendingChunks.Values);
        sortedChunks.Sort((a, b) => a.GetMinDistanceSqr().CompareTo(b.GetMinDistanceSqr()));

        int chunksLoaded = 0;
        foreach (var chunkToLoad in sortedChunks)
        {
            if (chunksLoaded >= MAX_CHUNKS_PER_TICK) break;

            _pendingChunks.Remove(chunkToLoad.Hash);

            var chunk = chunkMap.GetOrCreateChunk(chunkToLoad.X, chunkToLoad.Z, true);
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
        public List<ServerPlayerEntity> Players { get; } = [];

        // Cached chunk-center coordinates — computed once at construction.
        public double CenterX { get; }
        public double CenterZ { get; }

        public PendingChunk(long hash, int x, int z, ServerPlayerEntity initiator)
        {
            Hash = hash;
            X = x;
            Z = z;
            CenterX = x * 16 + 8;
            CenterZ = z * 16 + 8;
            Players.Add(initiator);
        }

        public double GetMinDistanceSqr()
        {
            double min = double.MaxValue;
            foreach (var p in Players)
            {
                double dx = p.x - CenterX;
                double dz = p.z - CenterZ;
                double d = dx * dx + dz * dz;
                if (d < min) min = d;
            }
            return min;
        }
    }
}
