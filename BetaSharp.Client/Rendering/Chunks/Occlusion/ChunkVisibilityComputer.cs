using System.Runtime.CompilerServices;
using BetaSharp.Blocks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Chunks.Occlusion;

public static class ChunkVisibilityComputer
{
    public static ChunkVisibilityStore Compute(WorldRegionSnapshot cache, int minX, int minY, int minZ)
    {
        ChunkVisibilityStore store = new();

        // We use a bitset to track visited blocks (4096 bits = 512 bytes)
        Span<uint> visited = stackalloc uint[(SubChunkRenderer.Size * SubChunkRenderer.Size * SubChunkRenderer.Size) / 32];

        // Check connectivity from each face
        for (int f = 0; f < ChunkDirectionExtensions.Count; f++)
        {
            ChunkDirection startFace = (ChunkDirection)f;
            visited.Clear();

            ChunkDirectionMask reachable = FloodFill(cache, minX, minY, minZ, startFace, visited);

            // For each reachable face, set visibility
            for (int t = 0; t < ChunkDirectionExtensions.Count; t++)
            {
                if ((reachable & (ChunkDirectionMask)(1 << t)) != 0)
                {
                    store.SetVisible(startFace, (ChunkDirection)t);
                }
            }
        }

        return store;
    }

    private static ChunkDirectionMask FloodFill(
        WorldRegionSnapshot cache,
        int minX, int minY, int minZ,
        ChunkDirection startFace,
        Span<uint> visited)
    {
        ChunkDirectionMask reachable = ChunkDirectionMask.None;
        const int totalBlocks = SubChunkRenderer.Size * SubChunkRenderer.Size * SubChunkRenderer.Size;

        Span<ushort> queue = stackalloc ushort[totalBlocks];
        int head = 0, tail = 0;

        // Add all air blocks on the start face to the queue
        for (int i = 0; i < SubChunkRenderer.Size; i++)
        {
            for (int j = 0; j < SubChunkRenderer.Size; j++)
            {
                int lx = 0, ly = 0, lz = 0;
                switch (startFace)
                {
                    case ChunkDirection.Down: lx = i; ly = 0; lz = j; break;
                    case ChunkDirection.Up: lx = i; ly = SubChunkRenderer.Size - 1; lz = j; break;
                    case ChunkDirection.North: lx = i; ly = j; lz = 0; break;
                    case ChunkDirection.South: lx = i; ly = j; lz = SubChunkRenderer.Size - 1; break;
                    case ChunkDirection.West: lx = 0; ly = i; lz = j; break;
                    case ChunkDirection.East: lx = SubChunkRenderer.Size - 1; ly = i; lz = j; break;
                }

                if (IsAir(cache, minX + lx, minY + ly, minZ + lz))
                {
                    int idx = GetIndex(lx, ly, lz);
                    if (!IsVisited(visited, idx))
                    {
                        MarkVisited(visited, idx);
                        queue[tail++] = (ushort)idx;
                    }
                }
            }
        }

        while (head < tail)
        {
            ushort idx = queue[head++];
            int lx = idx & 0xF;
            int ly = (idx >> 4) & 0xF;
            int lz = (idx >> 8) & 0xF;

            // Check if we touched any other face
            if (lx == 0) reachable |= ChunkDirectionMask.West;
            if (lx == SubChunkRenderer.Size - 1) reachable |= ChunkDirectionMask.East;
            if (ly == 0) reachable |= ChunkDirectionMask.Down;
            if (ly == SubChunkRenderer.Size - 1) reachable |= ChunkDirectionMask.Up;
            if (lz == 0) reachable |= ChunkDirectionMask.North;
            if (lz == SubChunkRenderer.Size - 1) reachable |= ChunkDirectionMask.South;

            TryVisit(cache, minX, minY, minZ, lx - 1, ly, lz, visited, queue, ref tail);
            TryVisit(cache, minX, minY, minZ, lx + 1, ly, lz, visited, queue, ref tail);
            TryVisit(cache, minX, minY, minZ, lx, ly - 1, lz, visited, queue, ref tail);
            TryVisit(cache, minX, minY, minZ, lx, ly + 1, lz, visited, queue, ref tail);
            TryVisit(cache, minX, minY, minZ, lx, ly, lz - 1, visited, queue, ref tail);
            TryVisit(cache, minX, minY, minZ, lx, ly, lz + 1, visited, queue, ref tail);
        }

        return reachable;
    }

    private static void TryVisit(
        WorldRegionSnapshot cache,
        int minX, int minY, int minZ,
        int lx, int ly, int lz,
        Span<uint> visited,
        Span<ushort> queue,
        ref int tail)
    {
        if (lx < 0 || lx >= SubChunkRenderer.Size || ly < 0 || ly >= SubChunkRenderer.Size || lz < 0 || lz >= SubChunkRenderer.Size) return;

        int idx = GetIndex(lx, ly, lz);
        if (IsVisited(visited, idx)) return;

        if (IsAir(cache, minX + lx, minY + ly, minZ + lz))
        {
            MarkVisited(visited, idx);
            queue[tail++] = (ushort)idx;
        }
    }

    private static bool IsAir(WorldRegionSnapshot cache, int x, int y, int z)
    {
        int id = cache.GetBlockId(x, y, z);
        if (id <= 0) return true;
        return !Block.BlocksOpaque[id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndex(int x, int y, int z) => x | (y << 4) | (z << 8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVisited(Span<uint> visited, int idx) => (visited[idx >> 5] & (1u << (idx & 31))) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MarkVisited(Span<uint> visited, int idx) => visited[idx >> 5] |= (1u << (idx & 31));
}
