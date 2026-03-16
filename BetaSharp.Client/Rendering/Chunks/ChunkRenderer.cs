using BetaSharp.Client.Rendering.Chunks.Occlusion;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Profiling;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Chunks;

public class ChunkRenderer : IChunkVisibilityVisitor
{
    private const int UpdateChunksPerFrame = 16;
    private const int MeshChunksPerFrame = 32;

    private readonly ILogger<ChunkRenderer> _logger = Log.Instance.For<ChunkRenderer>();

    private class SubChunkState(bool isLit, SubChunkRenderer renderer)
    {
        public bool IsLit { get; set; } = isLit;
        public SubChunkRenderer Renderer { get; } = renderer;
    }

    private sealed class TranslucentDistanceComparer : IComparer<SubChunkRenderer>
    {
        public Vector3D<double> Origin;
        public int Compare(SubChunkRenderer? a, SubChunkRenderer? b)
        {
            if (a == null || b == null) return 0;
            double distA = Vector3D.DistanceSquared(ToDoubleVec(a.Position), Origin);
            double distB = Vector3D.DistanceSquared(ToDoubleVec(b.Position), Origin);
            return distB.CompareTo(distA); // descending
        }
    }

    public int FogMode { get; set; }
    public float FogDensity { get; set; }
    public float FogStart { get; set; }
    public float FogEnd { get; set; }
    public Vector4D<float> FogColor { get; set; }

    private readonly Dictionary<Vector3D<int>, SubChunkState> _renderers = [];
    private readonly List<SubChunkRenderer> _translucentRenderers = [];
    private readonly List<SubChunkRenderer> _renderersToRemove = [];
    private readonly ChunkMeshGenerator _meshGenerator;
    private readonly World _world;
    private readonly Dictionary<Vector3D<int>, ChunkMeshVersion> _chunkVersions = [];
    private readonly List<Vector3D<int>> _chunkVersionsToRemove = [];
    private readonly PriorityQueue<Vector3D<int>, double> _dirtyChunks = new();
    private readonly Core.Shader _chunkShader;
    private int _lastRenderDistance;
    private Vector3D<double> _lastViewPos;
    private Matrix4X4<float> _modelView;
    private Matrix4X4<float> _projection;
    private readonly ChunkOcclusionCuller _occlusionCuller = new();
    private readonly List<SubChunkRenderer> _visibleRenderers = [];
    private readonly TranslucentDistanceComparer _translucentDistanceComparer = new();
    private int _frameIndex = 0;
    private bool _firstRender = true;

    public bool UseOcclusionCulling { get; set; } = true;

    public int TotalChunks => _renderers.Count;
    public int ChunksInFrustum { get; private set; }
    public int ChunksOccluded { get; private set; }
    public int ChunksRendered { get; private set; }
    public int TranslucentMeshes { get; private set; }

    public ChunkRenderer(World world)
    {
        _meshGenerator = new();
        _world = world;

        _chunkShader = new(AssetManager.Instance.getAsset("shaders/chunk.vert").GetTextContent(), AssetManager.Instance.getAsset("shaders/chunk.frag").GetTextContent());

        GLManager.GL.UseProgram(0);
    }

    public void Render(ChunkRenderParams renderParams)
    {
        _lastRenderDistance = renderParams.RenderDistance;
        _lastViewPos = renderParams.ViewPos;

        _chunkShader.Bind();
        _chunkShader.SetUniform1("textureSampler", 0);
        _chunkShader.SetUniform1("fogMode", FogMode);
        _chunkShader.SetUniform1("fogDensity", FogDensity);
        _chunkShader.SetUniform1("fogStart", FogStart);
        _chunkShader.SetUniform1("fogEnd", FogEnd);
        _chunkShader.SetUniform4("fogColor", FogColor);

        int wrappedTicks = (int)(renderParams.Ticks % 24000);
        _chunkShader.SetUniform1("time", (wrappedTicks + renderParams.PartialTicks) / 20.0f);
        _chunkShader.SetUniform1("envAnim", renderParams.EnvironmentAnimation ? 1 : 0);
        _chunkShader.SetUniform1("chunkFadeEnabled", renderParams.ChunkFade ? 1 : 0);

        var modelView = new Matrix4X4<float>();
        var projection = new Matrix4X4<float>();

        unsafe
        {
            GLManager.GL.GetFloat(GLEnum.ModelviewMatrix, (float*)&modelView);
        }

        unsafe
        {
            GLManager.GL.GetFloat(GLEnum.ProjectionMatrix, (float*)&projection);
        }

        _modelView = modelView;
        _projection = projection;

        _chunkShader.SetUniformMatrix4("projectionMatrix", projection);

        _visibleRenderers.Clear();
        _frameIndex++;

        Vector3D<int> cameraChunkPos = new(
            (int)Math.Floor(renderParams.ViewPos.X / SubChunkRenderer.Size) * SubChunkRenderer.Size,
            (int)Math.Floor(renderParams.ViewPos.Y / SubChunkRenderer.Size) * SubChunkRenderer.Size,
            (int)Math.Floor(renderParams.ViewPos.Z / SubChunkRenderer.Size) * SubChunkRenderer.Size
        );

        _renderers.TryGetValue(cameraChunkPos, out SubChunkState? cameraState);

        if (cameraState == null)
        {
            int y = Math.Clamp(cameraChunkPos.Y, 0, 112);
            _renderers.TryGetValue(new Vector3D<int>(cameraChunkPos.X, y, cameraChunkPos.Z), out cameraState);
        }

        if (_firstRender)
        {
            _firstRender = false;
            for (int x = -_lastRenderDistance; x <= _lastRenderDistance; x++)
            {
                for (int z = -_lastRenderDistance; z <= _lastRenderDistance; z++)
                {
                    Vector3D<int> chunkPos = cameraChunkPos + new Vector3D<int>(x, 0, z) * SubChunkRenderer.Size;
                    if (IsChunkInRenderDistance(chunkPos, _lastViewPos))
                    {
                        for (int y = 0; y < 128; y += SubChunkRenderer.Size)
                        {
                            MarkDirty(chunkPos with { Y = y });
                        }
                    }
                }
            }
        }

        float renderDistWorld = renderParams.RenderDistance * SubChunkRenderer.Size;

        Profiler.Start("FindVisible");

        _occlusionCuller.FindVisible(
            this,
            cameraState?.Renderer,
            renderParams.ViewPos,
            renderParams.Camera,
            renderDistWorld,
            UseOcclusionCulling,
            _frameIndex
        );

        Profiler.Stop("FindVisible");

        AddNearbySections(cameraChunkPos, _frameIndex, renderParams.Camera);

        int frustumCount = 0;
        int visitedVisibleCount = _visibleRenderers.Count;

        foreach (SubChunkState state in _renderers.Values)
        {
            if (renderParams.Camera.isBoundingBoxInFrustum(state.Renderer.BoundingBox))
            {
                frustumCount++;
            }
        }

        ChunksInFrustum = frustumCount;
        ChunksOccluded = frustumCount - visitedVisibleCount;
        ChunksRendered = visitedVisibleCount;

        if (renderParams.RenderOccluded)
        {
            _visibleRenderers.Clear();
            foreach (SubChunkState state in _renderers.Values)
            {
                SubChunkRenderer renderer = state.Renderer;
                if (renderer.LastVisibleFrame != _frameIndex)
                {
                    if (renderer.IsVisible(renderParams.Camera, renderParams.ViewPos, renderDistWorld))
                    {
                        _visibleRenderers.Add(renderer);
                    }
                }
            }
            ChunksRendered = _visibleRenderers.Count;
        }

        int translucentCount = 0;
        foreach (SubChunkRenderer renderer in _visibleRenderers)
        {
            renderer.Update(renderParams.DeltaTime);

            if (renderer.HasTranslucentMesh)
            {
                translucentCount++;
            }

            float fadeProgress = Math.Clamp(renderer.Age / SubChunkRenderer.FadeDuration, 0.0f, 1.0f);
            _chunkShader.SetUniform1("fadeProgress", fadeProgress);
            renderer.Render(_chunkShader, 0, renderParams.ViewPos, modelView);

            if (renderer.HasTranslucentMesh)
            {
                _translucentRenderers.Add(renderer);
            }
        }

        TranslucentMeshes = translucentCount;

        ProcessUpdates();
        LoadNewMeshes(renderParams.ViewPos);

        GLManager.GL.UseProgram(0);
        Core.VertexArray.Unbind();
    }

    public void RenderTransparent(ChunkRenderParams renderParams)
    {
        _chunkShader.Bind();
        _chunkShader.SetUniform1("textureSampler", 0);

        _chunkShader.SetUniformMatrix4("projectionMatrix", _projection);

        _translucentDistanceComparer.Origin = renderParams.ViewPos;
        _translucentRenderers.Sort(_translucentDistanceComparer);

        foreach (SubChunkRenderer renderer in _translucentRenderers)
        {
            float fadeProgress = Math.Clamp(renderer.Age / SubChunkRenderer.FadeDuration, 0.0f, 1.0f);
            _chunkShader.SetUniform1("fadeProgress", fadeProgress);
            renderer.Render(_chunkShader, 1, renderParams.ViewPos, _modelView);
        }

        _translucentRenderers.Clear();

        GLManager.GL.UseProgram(0);
        Core.VertexArray.Unbind();
    }

    private void LoadNewMeshes(Vector3D<double> viewPos)
    {
        for (int i = 0; i < MeshChunksPerFrame; i++)
        {
            if (!_meshGenerator.TryDequeueMesh(out var mesh)) break;

            if (IsChunkInRenderDistance(mesh.Pos, viewPos))
            {
                if (!_chunkVersions.TryGetValue(mesh.Pos, out ChunkMeshVersion? version))
                {
                    version = ChunkMeshVersion.Get();
                    _chunkVersions[mesh.Pos] = version;
                }

                version.CompleteMesh(mesh.Version);

                if (version.IsStale(mesh.Version))
                {
                    long? snapshot = version.SnapshotIfNeeded();
                    if (snapshot.HasValue)
                    {
                        _meshGenerator.MeshChunk(_world, mesh.Pos, snapshot.Value);
                    }
                    continue;
                }

                if (_renderers.TryGetValue(mesh.Pos, out SubChunkState? state))
                {
                    state.Renderer.UploadMeshData(mesh.Solid, mesh.Translucent);
                    state.IsLit = mesh.IsLit;
                    state.Renderer.VisibilityData = mesh.VisibilityData;
                }
                else
                {
                    var renderer = new SubChunkRenderer(mesh.Pos);
                    renderer.UploadMeshData(mesh.Solid, mesh.Translucent);
                    renderer.VisibilityData = mesh.VisibilityData;
                    _renderers[mesh.Pos] = new SubChunkState(mesh.IsLit, renderer);
                    UpdateAdjacency(renderer, true);
                }

                mesh.Dispose();
            }
        }
    }

    private void UpdateAdjacency(SubChunkRenderer renderer, bool added)
    {
        Vector3D<int> pos = renderer.Position;
        int size = SubChunkRenderer.Size;

        SubChunkRenderer? Get(Vector3D<int> p) => _renderers.TryGetValue(p, out SubChunkState? s) ? s.Renderer : null;

        SubChunkRenderer? down = Get(pos + new Vector3D<int>(0, -size, 0));
        SubChunkRenderer? up = Get(pos + new Vector3D<int>(0, size, 0));
        SubChunkRenderer? north = Get(pos + new Vector3D<int>(0, 0, -size));
        SubChunkRenderer? south = Get(pos + new Vector3D<int>(0, 0, size));
        SubChunkRenderer? west = Get(pos + new Vector3D<int>(-size, 0, 0));
        SubChunkRenderer? east = Get(pos + new Vector3D<int>(size, 0, 0));

        if (added)
        {
            renderer.AdjacentDown = down;
            renderer.AdjacentUp = up;
            renderer.AdjacentNorth = north;
            renderer.AdjacentSouth = south;
            renderer.AdjacentWest = west;
            renderer.AdjacentEast = east;

            down?.AdjacentUp = renderer;
            up?.AdjacentDown = renderer;
            north?.AdjacentSouth = renderer;
            south?.AdjacentNorth = renderer;
            west?.AdjacentEast = renderer;
            east?.AdjacentWest = renderer;
        }
        else
        {
            down?.AdjacentUp = null;
            up?.AdjacentDown = null;
            north?.AdjacentSouth = null;
            south?.AdjacentNorth = null;
            west?.AdjacentEast = null;
            east?.AdjacentWest = null;
        }
    }

    public void Visit(SubChunkRenderer renderer)
    {
        _visibleRenderers.Add(renderer);
    }

    private void AddNearbySections(Vector3D<int> cameraChunkPos, int frame, Culler camera)
    {
        int size = SubChunkRenderer.Size;
        for (int x = -size; x <= size; x += size)
        {
            for (int y = -size; y <= size; y += size)
            {
                for (int z = -size; z <= size; z += size)
                {
                    Vector3D<int> pos = cameraChunkPos + new Vector3D<int>(x, y, z);
                    if (_renderers.TryGetValue(pos, out SubChunkState? state))
                    {
                        if (state.Renderer.LastVisibleFrame != frame)
                        {
                            state.Renderer.LastVisibleFrame = frame;
                            if (camera.isBoundingBoxInFrustum(state.Renderer.BoundingBox))
                            {
                                Visit(state.Renderer);
                            }
                        }
                    }
                }
            }
        }
    }

    private void ProcessUpdates()
    {
        int processed = 0;

        while (_dirtyChunks.TryDequeue(out var pos, out double _))
        {
            if (!IsChunkInRenderDistance(pos, _lastViewPos)) {
                continue;
            }

            if (!_chunkVersions.TryGetValue(pos, out var version))
            {
                version = ChunkMeshVersion.Get();
                _chunkVersions[pos] = version;
            }

            long? snapshot = version.SnapshotIfNeeded();
            if (snapshot.HasValue)
            {
                _meshGenerator.MeshChunk(_world, pos, snapshot.Value);
                processed++;
                if (processed >= UpdateChunksPerFrame) return;
            }
        }
    }

    public void UpdateAllRenderers()
    {
        foreach (SubChunkState state in _renderers.Values)
        {
            if (IsChunkInRenderDistance(state.Renderer.Position, _lastViewPos) && state.IsLit)
            {
                MarkDirty(state.Renderer.Position);
            }
        }
    }

    public void Tick(Vector3D<double> viewPos)
    {
        Profiler.Start("ChunkRenderer.Tick");

        _lastViewPos = viewPos;

        Profiler.Start("ChunkRenderer.Tick.RemoveVersions");
        foreach (KeyValuePair<Vector3D<int>, ChunkMeshVersion> version in _chunkVersions)
        {
            if (!IsChunkInRenderDistance(version.Key, _lastViewPos))
            {
                _chunkVersionsToRemove.Add(version.Key);
            }
        }

        foreach (Vector3D<int> pos in _chunkVersionsToRemove)
        {
            _chunkVersions[pos].Release();
            _chunkVersions.Remove(pos);
        }

        _chunkVersionsToRemove.Clear();

        foreach (SubChunkState state in _renderers.Values)
        {
            if (!IsChunkInRenderDistance(state.Renderer.Position, _lastViewPos))
            {
                _renderersToRemove.Add(state.Renderer);
            }
        }

        foreach (SubChunkRenderer renderer in _renderersToRemove)
        {
            UpdateAdjacency(renderer, false);
            _renderers.Remove(renderer.Position);
            renderer.Dispose();

            _chunkVersions.Remove(renderer.Position);
        }

        _renderersToRemove.Clear();

        Profiler.Stop("ChunkRenderer.Tick.RemoveVersions");

        Profiler.Stop("ChunkRenderer.Tick");
    }

    public void MarkDirty(Vector3D<int> chunkPos)
    {
        if (!_world.isRegionLoaded(chunkPos.X - 1, chunkPos.Y - 1, chunkPos.Z - 1, chunkPos.X + SubChunkRenderer.Size + 1, chunkPos.Y + SubChunkRenderer.Size + 1, chunkPos.Z + SubChunkRenderer.Size + 1) | !IsChunkInRenderDistance(chunkPos, _lastViewPos))
            return;

        if (!_chunkVersions.TryGetValue(chunkPos, out ChunkMeshVersion? version))
        {
            version = ChunkMeshVersion.Get();
            _chunkVersions[chunkPos] = version;
        }
        version.MarkDirty();

        _dirtyChunks.Enqueue(chunkPos, Vector3D.DistanceSquared(ToDoubleVec(chunkPos), _lastViewPos));
    }

    private bool IsChunkInRenderDistance(Vector3D<int> chunkWorldPos, Vector3D<double> viewPos)
    {
        int chunkX = chunkWorldPos.X / SubChunkRenderer.Size;
        int chunkZ = chunkWorldPos.Z / SubChunkRenderer.Size;

        int viewChunkX = (int)Math.Floor(viewPos.X / SubChunkRenderer.Size);
        int viewChunkZ = (int)Math.Floor(viewPos.Z / SubChunkRenderer.Size);

        int dx = chunkX - viewChunkX;
        int dz = chunkZ - viewChunkZ;
        return dx * dx + dz * dz <= _lastRenderDistance * _lastRenderDistance;
    }

    public void GetMeshSizeStats(out int minSize, out int maxSize, out int avgSize, out Dictionary<int, int> buckets)
    {
        int curMin = int.MaxValue;
        int curMax = 0;
        long totalSize = 0;
        int count = 0;
        var b = new Dictionary<int, int>();

        foreach (SubChunkState state in _renderers.Values)
        {
            void AddSize(int size)
            {
                if (size == 0) return;
                if (size < curMin) curMin = size;
                if (size > curMax) curMax = size;
                totalSize += size;
                count++;

                int sizeKb = (int)Math.Ceiling(size / 1024.0);
                if (sizeKb <= 0) sizeKb = 1;
                int po2 = 1;
                while (po2 < sizeKb) po2 *= 2;

                if (!b.TryGetValue(po2, out int val))
                    val = 0;
                b[po2] = val + 1;
            }

            AddSize(state.Renderer.SolidMeshSizeBytes);
            AddSize(state.Renderer.TranslucentMeshSizeBytes);
        }

        minSize = count == 0 ? 0 : curMin;
        maxSize = curMax;
        avgSize = count > 0 ? (int)(totalSize / count) : 0;
        buckets = b;
    }

    private static Vector3D<double> ToDoubleVec(Vector3D<int> vec) => new(vec.X, vec.Y, vec.Z);

    public void Dispose()
    {
        foreach (SubChunkState state in _renderers.Values)
        {
            state.Renderer.Dispose();
        }

        _chunkShader.Dispose();

        _renderers.Clear();

        _translucentRenderers.Clear();
        _renderersToRemove.Clear();

        foreach (ChunkMeshVersion version in _chunkVersions.Values)
        {
            version.Release();
        }
        _chunkVersions.Clear();
    }
}
