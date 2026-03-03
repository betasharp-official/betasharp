using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
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
    private readonly PooledQueue<MeshBuildResult> results = new();
    private readonly ObjectPool<PooledList<ChunkVertex>> listPool =
        new(() => new PooledList<ChunkVertex>(), 64);

    private ushort maxConcurrentTasks;
    private SemaphoreSlim? concurrencySemaphore;

    public ChunkMeshGenerator(ushort maxConcurrentTasks = 0)
    {
        MaxConcurrentTasks = maxConcurrentTasks;
    }

    public MeshBuildResult? Mesh
    {
        get
        {
            lock (results)
            {
                if (results.IsEmpty) return null;
                return results.Dequeue();
            }
        }
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

    public void MeshChunk(World world, Vector3D<int> pos, long version)
    {
        WorldRegionSnapshot cache = new(
            world,
            pos.X - 1, pos.Y - 1, pos.Z - 1,
            pos.X + SubChunkRenderer.Size + 1,
            pos.Y + SubChunkRenderer.Size + 1,
            pos.Z + SubChunkRenderer.Size + 1
        );

        Task.Run(async () =>
        {
            if (concurrencySemaphore != null)
                await concurrencySemaphore.WaitAsync();

            try
            {
                MeshBuildResult mesh = GenerateMesh(pos, version, cache);
                lock (results)
                    results.Enqueue(mesh);
            }
            finally
            {
                cache.Dispose();
                concurrencySemaphore?.Release();
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
                        int id = cache.getBlockId(x, y, z);
                        if (id <= 0) continue;

                        Block b = Block.Blocks[id];
                        int blockPass = b.getRenderLayer();

                        if (blockPass != pass)
                            hasNextPass = true;
                        else
                            BlockRenderer.RenderBlockByRenderType(cache, b, new BlockPos(x, y, z), tess);
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
        results.Dispose();
        listPool.Dispose();
    }
}
