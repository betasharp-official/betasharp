using BetaSharp.Entities;

namespace BetaSharp.Server;

internal class ChunkLoadingQueue(ChunkMap chunkMap)
{
    private readonly ChunkMap chunkMap = chunkMap;
    private readonly List<PendingChunk> pendingChunks = [];
    private readonly HashSet<long> pendingChunkSet = [];
    //TODO: MAKE THIS CONFIGURABLE
    private const int MAX_CHUNKS_PER_TICK = 5;

    public void Add(int x, int z, ServerPlayerEntity player)
    {
        long hash = ChunkMap.GetChunkHash(x, z);

        if (pendingChunkSet.Contains(hash))
        {
            var pending = pendingChunks.FirstOrDefault(c => c.Hash == hash);
            if (pending != null && !pending.Players.Contains(player))
            {
                pending.Players.Add(player);
            }
        }
        else
        {
            var pending = new PendingChunk(hash, x, z, player);
            pendingChunks.Add(pending);
            pendingChunkSet.Add(hash);
        }
    }

    public void RemovePlayer(ServerPlayerEntity player)
    {
        foreach (var chunk in pendingChunks)
        {
            chunk.Players.Remove(player);
        }

        int removed = pendingChunks.RemoveAll(c => c.Players.Count == 0);
        if (removed > 0)
        {
            RebuildSet();
        }
    }

    private void RebuildSet()
    {
        pendingChunkSet.Clear();
        foreach (var c in pendingChunks)
        {
            pendingChunkSet.Add(c.Hash);
        }
    }

    public void Tick()
    {
        if (pendingChunks.Count == 0) return;

        pendingChunks.Sort((a, b) =>
        {
            double distA = a.GetMinDistanceSqr();
            double distB = b.GetMinDistanceSqr();
            return distA.CompareTo(distB);
        });

        int chunksLoaded = 0;

        while (chunksLoaded < MAX_CHUNKS_PER_TICK && pendingChunks.Count > 0)
        {
            var chunkToLoad = pendingChunks[0];
            pendingChunks.RemoveAt(0);
            pendingChunkSet.Remove(chunkToLoad.Hash);

            var chunk = chunkMap.GetOrCreateChunk(chunkToLoad.X, chunkToLoad.Z, true);

            if (chunk != null)
            {
                foreach (var player in chunkToLoad.Players)
                {
                    if (!chunk.HasPlayer(player))
                    {
                        chunk.AddPlayer(player);
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

        public PendingChunk(long hash, int x, int z, ServerPlayerEntity initiator)
        {
            Hash = hash;
            X = x;
            Z = z;
            Players.Add(initiator);
        }

        public double GetMinDistanceSqr()
        {
            double min = double.MaxValue;
            double centerX = X * 16 + 8;
            double centerZ = Z * 16 + 8;

            foreach (var p in Players)
            {
                double dx = p.x - centerX;
                double dz = p.z - centerZ;
                double d = dx * dx + dz * dz;
                if (d < min) min = d;
            }
            return min;
        }
    }
}
