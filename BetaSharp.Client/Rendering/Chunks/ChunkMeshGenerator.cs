using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Chunks;

internal struct MeshBuildResult
{
    public PooledList<ChunkVertex> Solid;
    public PooledList<ChunkVertex> Translucent;
    public bool IsLit;
    public Occlusion.ChunkVisibilityStore VisibilityData;
    public Vector3D<int> Pos;
    public long Version;

    public readonly void Dispose()
    {
        Solid?.Dispose();
        Translucent?.Dispose();
    }
}

internal class ChunkMeshGenerator : IDisposable
{
    private readonly object _meshResultsLock = new();
    private readonly List<MeshBuildResult> _pendingResults = [];
    private readonly ObjectPool<PooledList<ChunkVertex>> listPool =
        new(() => new PooledList<ChunkVertex>(), 64);

    private ushort maxConcurrentTasks;
    private SemaphoreSlim? concurrencySemaphore;
    private int _activeTasks;

    public ChunkMeshGenerator(ushort maxConcurrentTasks = 0)
    {
        MaxConcurrentTasks = maxConcurrentTasks;
    }

    public MeshBuildResult? GetBestMesh(Vector3D<double> cameraPos)
    {
        lock (_meshResultsLock)
        {
            int n = _pendingResults.Count;
            if (n == 0)
                return null;

            int bestIndex = 0;
            double bestDist = DistanceSquaredChunkToCamera(_pendingResults[0].Pos, cameraPos);
            for (int i = 1; i < n; i++)
            {
                double d = DistanceSquaredChunkToCamera(_pendingResults[i].Pos, cameraPos);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIndex = i;
                }
            }

            MeshBuildResult picked = _pendingResults[bestIndex];
            _pendingResults.RemoveAt(bestIndex);
            return picked;
        }
    }

    private static double DistanceSquaredChunkToCamera(Vector3D<int> chunkMin, Vector3D<double> cameraPos)
    {
        double h = SubChunkRenderer.Size * 0.5;
        double dx = chunkMin.X + h - cameraPos.X;
        double dy = chunkMin.Y + h - cameraPos.Y;
        double dz = chunkMin.Z + h - cameraPos.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    public ushort MaxConcurrentTasks
    {
        get => maxConcurrentTasks;
        set
        {
            maxConcurrentTasks = value;

            concurrencySemaphore?.Dispose();
            concurrencySemaphore = maxConcurrentTasks > 0
                ? new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks)
                : null;
        }
    }

    public int ActiveTasks => Volatile.Read(ref _activeTasks);

    public void MeshChunk(World world, Vector3D<int> pos, long version)
    {
        WorldRegionSnapshot cache = new(
            world,
            pos.X - 1, pos.Y - 1, pos.Z - 1,
            pos.X + SubChunkRenderer.Size + 1,
            pos.Y + SubChunkRenderer.Size + 1,
            pos.Z + SubChunkRenderer.Size + 1
        );

        Interlocked.Increment(ref _activeTasks);

        Task.Run(async () =>
        {
            try
            {
                if (concurrencySemaphore != null)
                    await concurrencySemaphore.WaitAsync();

                try
                {
                    MeshBuildResult mesh = GenerateMesh(pos, version, cache);
                    lock (_meshResultsLock)
                        _pendingResults.Add(mesh);
                }
                finally
                {
                    cache.Dispose();
                    concurrencySemaphore?.Release();
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeTasks);
            }
        });
    }

    private MeshBuildResult GenerateMesh(Vector3D<int> pos, long version, WorldRegionSnapshot cache)
    {
        int minX = pos.X;
        int minY = pos.Y;
        int minZ = pos.Z;
        int maxX = pos.X + SubChunkRenderer.Size;
        int maxY = pos.Y + SubChunkRenderer.Size;
        int maxZ = pos.Z + SubChunkRenderer.Size;

        var result = new MeshBuildResult
        {
            Pos = pos,
            Version = version
        };

        var tess = new Tessellator();

        for (int pass = 0; pass < 2; pass++)
        {
            bool hasNextPass = false;

            tess.startCapture(TesselatorCaptureVertexFormat.Chunk);
            tess.startDrawingQuads();
            tess.setTranslationD(-pos.X, -pos.Y, -pos.Z);

            for (int y = minY; y < maxY; y++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        int id = cache.GetBlockId(x, y, z);
                        if (id <= 0) continue;

                        Block b = Block.Blocks[id];
                        int blockPass = b.getRenderLayer();

                        if (blockPass != pass)
                            hasNextPass = true;
                        else
                            BlockRenderer.RenderBlockByRenderType(cache, cache, b, new BlockPos(x, y, z), tess);
                    }
                }
            }

            tess.draw();
            tess.setTranslationD(0, 0, 0);

            PooledList<ChunkVertex> verts = tess.endCaptureChunkVertices();
            if (verts.Count > 0)
            {
                PooledList<ChunkVertex> list = listPool.Get();
                list.AddRange(verts.Span);

                if (pass == 0)
                    result.Solid = list;
                else
                    result.Translucent = list;
            }

            if (!hasNextPass) break;
        }

        result.IsLit = cache.getIsLit();
        result.VisibilityData = Occlusion.ChunkVisibilityComputer.Compute(cache, pos.X, pos.Y, pos.Z);
        return result;
    }

    public void Dispose()
    {
        lock (_meshResultsLock)
        {
            foreach (MeshBuildResult m in _pendingResults)
                m.Dispose();
            _pendingResults.Clear();
        }

        listPool.Dispose();
    }
}
