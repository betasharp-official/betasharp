using BetaSharp.Client.Rendering.Chunks.Occlusion;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Profiling;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Chunks;

public class ChunkRenderer : IChunkVisibilityVisitor
{
    static ChunkRenderer()
    {
        var offsets = new List<Vector3D<int>>();

        for (int x = -MaxRenderDistance; x <= MaxRenderDistance; x++)
        {
            for (int y = -8; y <= 8; y++)
            {
                for (int z = -MaxRenderDistance; z <= MaxRenderDistance; z++)
                {
                    offsets.Add(new Vector3D<int>(x, y, z));
                }
            }
        }

        offsets.Sort((a, b) =>
            (a.X * a.X + a.Y * a.Y + a.Z * a.Z).CompareTo(b.X * b.X + b.Y * b.Y + b.Z * b.Z));

        s_spiralOffsets = [.. offsets];
    }

    private class SubChunkState(bool isLit, SubChunkRenderer renderer)
    {
        public bool IsLit { get; set; } = isLit;
        public SubChunkRenderer Renderer { get; } = renderer;
    }

    private struct ChunkToMeshInfo(Vector3D<int> pos, long version, bool priority)
    {
        public Vector3D<int> Pos = pos;
        public long Version = version;
        public bool priority = priority;
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

    private static readonly Vector3D<int>[] s_spiralOffsets;
    private const int MaxRenderDistance = 32 + 1;
    private readonly Dictionary<Vector3D<int>, SubChunkState> _renderers = [];
    private readonly List<SubChunkRenderer> _translucentRenderers = [];
    private readonly List<SubChunkRenderer> _renderersToRemove = [];
    private readonly ChunkMeshGenerator _meshGenerator;
    private readonly World _world;
    private readonly Dictionary<Vector3D<int>, ChunkMeshVersion> _chunkVersions = [];
    private readonly List<Vector3D<int>> _chunkVersionsToRemove = [];
    private readonly List<ChunkToMeshInfo> _dirtyChunks = [];
    private readonly List<ChunkToMeshInfo> _lightingUpdates = [];
    private readonly Shader _chunkShader;
    private int _lastRenderDistance;
    private Vector3D<double> _lastViewPos;
    private int _currentIndex;
    private Matrix4X4<float> _modelView;
    private Matrix4X4<float> _projection;
    private int _fogMode;
    private float _fogDensity;
    private float _fogStart;
    private float _fogEnd;
    private Vector4D<float> _fogColor;
    private readonly ChunkOcclusionCuller _occlusionCuller = new();
    private readonly List<SubChunkRenderer> _visibleRenderers = [];
    private readonly List<SubChunkRenderer> _occludedRenderersBuffer = [];
    private readonly TranslucentDistanceComparer _translucentDistanceComparer = new();
    private int _frameIndex = 0;
    private readonly Func<bool> _alternateBlocks;

    public bool UseOcclusionCulling { get; set; } = true;

    public int TotalChunks => _renderers.Count;
    public int ChunksInFrustum { get; private set; }
    public int ChunksOccluded { get; private set; }
    public int ChunksRendered { get; private set; }
    public int TranslucentMeshes { get; private set; }

    public ChunkRenderer(World world, Func<bool> alternateBlocks)
    {
        _meshGenerator = new();
        _world = world;
        _alternateBlocks = alternateBlocks;

        _chunkShader = new(AssetManager.Instance.getAsset("shaders/chunk.vert").GetTextContent(), AssetManager.Instance.getAsset("shaders/chunk.frag").GetTextContent());

        GLManager.GL.UseProgram(0);
    }

    public void Render(ChunkRenderParams renderParams)
    {
        _lastRenderDistance = renderParams.RenderDistance;
        _lastViewPos = renderParams.ViewPos;

        _chunkShader.Bind();
        _chunkShader.SetUniform1("textureSampler", 0);
        _chunkShader.SetUniform1("fogMode", _fogMode);
        _chunkShader.SetUniform1("fogDensity", _fogDensity);
        _chunkShader.SetUniform1("fogStart", _fogStart);
        _chunkShader.SetUniform1("fogEnd", _fogEnd);
        _chunkShader.SetUniform4("fogColor", _fogColor);

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

        float renderDistWorld = renderParams.RenderDistance * SubChunkRenderer.Size;

        using (Profiler.Begin("FindVisible"))
        {
            _occlusionCuller.FindVisible(
                this,
                cameraState?.Renderer,
                renderParams.ViewPos,
                renderParams.Camera,
                renderDistWorld,
                UseOcclusionCulling,
                _frameIndex
            );
        }

        AddNearbySections(cameraChunkPos, _frameIndex, renderParams.Camera);

        int frustumCount = 0;
        int visitedVisibleCount = _visibleRenderers.Count;

        foreach (SubChunkState state in _renderers.Values)
        {
            if (renderParams.Camera.IsBoundingBoxInFrustum(state.Renderer.BoundingBox))
            {
                frustumCount++;
            }
        }

        ChunksInFrustum = frustumCount;
        ChunksOccluded = frustumCount - visitedVisibleCount;
        ChunksRendered = visitedVisibleCount;

        if (renderParams.RenderOccluded)
        {
            _occludedRenderersBuffer.Clear();
            foreach (SubChunkState state in _renderers.Values)
            {
                SubChunkRenderer renderer = state.Renderer;
                if (renderer.LastVisibleFrame != _frameIndex)
                {
                    if (renderer.IsVisible(renderParams.Camera, renderParams.ViewPos, renderDistWorld))
                    {
                        _occludedRenderersBuffer.Add(renderer);
                    }
                }
            }
            _visibleRenderers.Clear();
            _visibleRenderers.AddRange(_occludedRenderersBuffer);
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

        foreach (SubChunkState state in _renderers.Values)
        {
            if (!IsChunkInRenderDistance(state.Renderer.Position, renderParams.ViewPos))
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

        ProcessOneMeshUpdate(renderParams.Camera);
        ProcessOneLightingMeshUpdate();
        LoadNewMeshes(renderParams.ViewPos);

        GLManager.GL.UseProgram(0);
        Core.VertexArray.Unbind();
    }

    public void SetFogMode(int mode)
    {
        _fogMode = mode;
    }

    public void SetFogDensity(float density)
    {
        _fogDensity = density;
    }

    public void SetFogStart(float start)
    {
        _fogStart = start;
    }

    public void SetFogEnd(float end)
    {
        _fogEnd = end;
    }

    public void SetFogColor(float r, float g, float b, float a)
    {
        _fogColor = new(r, g, b, a);
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

    private void LoadNewMeshes(Vector3D<double> viewPos, int maxChunks = 8)
    {
        for (int i = 0; i < maxChunks; i++)
        {
            if (!_meshGenerator.TryDequeueMesh(out MeshBuildResult mesh)) break;

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
                        _meshGenerator.MeshChunk(_world, mesh.Pos, snapshot.Value, _alternateBlocks());
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

    private void AddNearbySections(Vector3D<int> cameraChunkPos, int frame, ICuller camera)
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
                            if (camera.IsBoundingBoxInFrustum(state.Renderer.BoundingBox))
                            {
                                Visit(state.Renderer);
                            }
                        }
                    }
                }
            }
        }
    }

    private void ProcessOneMeshUpdate(ICuller camera)
    {
        _dirtyChunks.RemoveAll(c => !IsChunkInRenderDistance(c.Pos, _lastViewPos));
        int bestIndex = -1;
        double bestDist = double.MaxValue;
        for (int i = 0; i < _dirtyChunks.Count; i++)
        {
            ChunkToMeshInfo info = _dirtyChunks[i];
            var aabb = new Box(
                info.Pos.X, info.Pos.Y, info.Pos.Z,
                info.Pos.X + SubChunkRenderer.Size,
                info.Pos.Y + SubChunkRenderer.Size,
                info.Pos.Z + SubChunkRenderer.Size
            );

            double dist = Vector3D.DistanceSquared(ToDoubleVec(info.Pos), _lastViewPos);
            if (dist < bestDist && camera.IsBoundingBoxInFrustum(aabb))
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        if (bestIndex != -1)
        {
            ChunkToMeshInfo closest = _dirtyChunks[bestIndex];
            _meshGenerator.MeshChunk(_world, closest.Pos, closest.Version, _alternateBlocks());
            _dirtyChunks.RemoveAt(bestIndex);
        }
    }

    private void ProcessOneLightingMeshUpdate()
    {
        _lightingUpdates.RemoveAll(c => !IsChunkInRenderDistance(c.Pos, _lastViewPos));
        int bestIndex = -1;
        double bestDist = double.MaxValue;
        for (int i = 0; i < _lightingUpdates.Count; i++)
        {
            double dist = Vector3D.DistanceSquared(ToDoubleVec(_lightingUpdates[i].Pos), _lastViewPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        if (bestIndex != -1)
        {
            ChunkToMeshInfo update = _lightingUpdates[bestIndex];
            _meshGenerator.MeshChunk(_world, update.Pos, update.Version, _alternateBlocks());
            _lightingUpdates.RemoveAt(bestIndex);
        }
    }

    public void UpdateAllRenderers()
    {
        foreach (SubChunkState state in _renderers.Values)
        {
            if (IsChunkInRenderDistance(state.Renderer.Position, _lastViewPos) && state.IsLit)
            {
                if (!_chunkVersions.TryGetValue(state.Renderer.Position, out ChunkMeshVersion? version))
                {
                    version = ChunkMeshVersion.Get();
                    _chunkVersions[state.Renderer.Position] = version;
                }

                version.MarkDirty();

                long? snapshot = version.SnapshotIfNeeded();
                if (snapshot.HasValue)
                {
                    _lightingUpdates.Add(new(state.Renderer.Position, snapshot.Value, false));
                }
            }
        }
    }

    public void Tick(Vector3D<double> viewPos)
    {
        using var _chunkTick = Profiler.Begin("ChunkTick");

        _lastViewPos = viewPos;

        Vector3D<int> currentChunk = new(
            (int)Math.Floor(viewPos.X / SubChunkRenderer.Size),
            (int)Math.Floor(viewPos.Y / SubChunkRenderer.Size),
            (int)Math.Floor(viewPos.Z / SubChunkRenderer.Size)
        );

        int radiusSq = _lastRenderDistance * _lastRenderDistance;
        int enqueuedCount = 0;
        bool priorityPassClean = true;

        //TODO: MAKE THESE CONFIGURABLE
        const int MAX_CHUNKS_PER_FRAME = 32;
        const int PRIORITY_PASS_LIMIT = 1024;
        const int BACKGROUND_PASS_LIMIT = 2048;

        for (int i = 0; i < PRIORITY_PASS_LIMIT && i < s_spiralOffsets.Length; i++)
        {
            Vector3D<int> offset = s_spiralOffsets[i];
            int distSq = offset.X * offset.X + offset.Y * offset.Y + offset.Z * offset.Z;

            if (distSq > radiusSq)
                break;

            Vector3D<int> chunkPos = (currentChunk + offset) * SubChunkRenderer.Size;

            if (chunkPos.Y < 0 || chunkPos.Y >= ChuckFormat.WorldHeight)
                continue;

            if (_renderers.ContainsKey(chunkPos) || _chunkVersions.ContainsKey(chunkPos))
                continue;

            if (MarkDirty(chunkPos))
            {
                enqueuedCount++;
                priorityPassClean = false;
            }
            else
            {
                priorityPassClean = false;
            }

            if (enqueuedCount >= MAX_CHUNKS_PER_FRAME)
                break;
        }

        if (priorityPassClean && enqueuedCount < MAX_CHUNKS_PER_FRAME)
        {
            for (int i = 0; i < BACKGROUND_PASS_LIMIT; i++)
            {
                Vector3D<int> offset = s_spiralOffsets[_currentIndex];
                int distSq = offset.X * offset.X + offset.Y * offset.Y + offset.Z * offset.Z;

                if (distSq <= radiusSq)
                {
                    Vector3D<int> chunkPos = (currentChunk + offset) * SubChunkRenderer.Size;
                    if (!_renderers.ContainsKey(chunkPos) && !_chunkVersions.ContainsKey(chunkPos))
                    {
                        if (MarkDirty(chunkPos))
                        {
                            enqueuedCount++;
                        }
                    }
                }

                _currentIndex = (_currentIndex + 1) % s_spiralOffsets.Length;

                if (enqueuedCount >= MAX_CHUNKS_PER_FRAME)
                    break;
            }
        }

        using (Profiler.Begin("RemoveVersions"))
        {
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
        }
    }

    public void MarkAllVisibleChunksDirty()
    {
        if (_lastRenderDistance <= 0) return;

        foreach (Vector3D<int> pos in new List<Vector3D<int>>(_chunkVersions.Keys))
            MarkDirty(pos, true);
    }

    public bool MarkDirty(Vector3D<int> chunkPos, bool priority = false)
    {
        if (!_world.BlockHost.IsRegionLoaded(chunkPos.X - 1, chunkPos.Y - 1, chunkPos.Z - 1, chunkPos.X + SubChunkRenderer.Size + 1, chunkPos.Y + SubChunkRenderer.Size + 1, chunkPos.Z + SubChunkRenderer.Size + 1) | !IsChunkInRenderDistance(chunkPos, _lastViewPos))
            return false;

        if (!_chunkVersions.TryGetValue(chunkPos, out ChunkMeshVersion? version))
        {
            version = ChunkMeshVersion.Get();
            _chunkVersions[chunkPos] = version;
        }
        version.MarkDirty();

        long? snapshot = version.SnapshotIfNeeded();
        if (snapshot.HasValue)
        {
            for (int i = 0; i < _dirtyChunks.Count; i++)
            {
                if (_dirtyChunks[i].Pos == chunkPos)
                {
                    _dirtyChunks[i] = new(chunkPos, snapshot.Value, priority || _dirtyChunks[i].priority);
                    return true;
                }
            }

            _dirtyChunks.Add(new(chunkPos, snapshot.Value, priority));
            return true;
        }

        return false;
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
