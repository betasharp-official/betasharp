using System.Collections.Concurrent;
using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Chunks.Occlusion;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Chunks;

internal struct MeshBuildResult : IDisposable
{
    public PooledList<ChunkVertex> Solid;
    public PooledList<ChunkVertex> Translucent;
    public bool IsLit;
    public ChunkVisibilityStore VisibilityData;
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
    private readonly ILogger<ChunkMeshGenerator> _logger = Log.Instance.For<ChunkMeshGenerator>();

    private readonly ConcurrentQueue<MeshBuildResult> _results = new();
    private readonly ObjectPool<PooledList<ChunkVertex>> _listPool =
        new(() => new PooledList<ChunkVertex>(), 64);

    private SemaphoreSlim? _concurrencySemaphore;

    public ChunkMeshGenerator(ushort maxConcurrentTasks = 0)
    {
        MaxConcurrentTasks = maxConcurrentTasks;
    }

    public bool TryDequeueMesh(out MeshBuildResult result)
    {
        return _results.TryDequeue(out result);
    }

    public ushort MaxConcurrentTasks
    {
        get;
        set
        {
            field = value;

            _concurrencySemaphore?.Dispose();
            _concurrencySemaphore = field > 0
                ? new SemaphoreSlim(field, field)
                : null;
        }
    }

    //TODO: Make a chunk mesh config struct for alternateBlocks and other flags
    public void MeshChunk(World world, Vector3D<int> pos, long version, bool alternateBlocks)
    {
        //TODO: OPTIMIZE THIS
        WorldRegionSnapshot cache = new(
            world,
            pos.X - 1, pos.Y - 1, pos.Z - 1,
            pos.X + SubChunkRenderer.Size + 1,
            pos.Y + SubChunkRenderer.Size + 1,
            pos.Z + SubChunkRenderer.Size + 1
        );

        Task.Run(async () =>
        {
            if (_concurrencySemaphore != null)
                await _concurrencySemaphore.WaitAsync();

            try
            {
                MeshBuildResult mesh = GenerateMesh(pos, version, cache, alternateBlocks);
                _results.Enqueue(mesh);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chunk mesh at {Pos}", pos);
            }
            finally
            {
                cache.Dispose();
                _concurrencySemaphore?.Release();
            }
        });
    }

    private MeshBuildResult GenerateMesh(Vector3D<int> pos, long version, WorldRegionSnapshot cache, bool alternateBlocks)
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
                        {
                            hasNextPass = true;
                        }
                        else
                        {
                            BlockRenderer.RenderBlockByRenderType(cache, cache, b, new BlockPos(x, y, z), tess, doVariance: alternateBlocks);
                        }
                    }
                }
            }

            tess.draw();
            tess.setTranslationD(0, 0, 0);

            PooledList<ChunkVertex> verts = tess.endCaptureChunkVertices();
            if (verts.Count > 0)
            {
                PooledList<ChunkVertex> list = _listPool.Get();
                list.AddRange(verts.Span);

                if (pass == 0)
                {
                    result.Solid = list;
                }
                else
                {
                    result.Translucent = list;
                }
            }

            if (!hasNextPass) break;
        }

        result.IsLit = cache.IsLit;
        result.VisibilityData = ChunkVisibilityComputer.Compute(cache, pos.X, pos.Y, pos.Z);
        return result;
    }

    public void Dispose()
    {
        _listPool.Dispose();
    }
}
