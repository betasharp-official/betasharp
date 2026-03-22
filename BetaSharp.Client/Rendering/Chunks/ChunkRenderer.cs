using BetaSharp.Client.Rendering.Chunks.Occlusion;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Profiling;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Chunks;

public class ChunkRenderer : IChunkVisibilityVisitor
{
    private readonly ILogger<ChunkRenderer> _logger = Log.Instance.For<ChunkRenderer>();

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
    private const int MaxMeshJobsPerFrame = 4;
    private readonly Dictionary<Vector3D<int>, SubChunkState> _renderers = [];
    private readonly List<SubChunkRenderer> _translucentRenderers = [];
    private readonly List<SubChunkRenderer> _renderersToRemove = [];
    private readonly ChunkMeshGenerator _meshGenerator;
    private readonly World _world;
    private readonly Dictionary<Vector3D<int>, ChunkMeshVersion> _chunkVersions = [];
    private readonly List<Vector3D<int>> _chunkVersionsToRemove = [];
    private readonly List<ChunkToMeshInfo> _initialMeshQueue = [];
    private readonly HashSet<Vector3D<int>> _initialMeshQueued = [];
    private readonly List<ChunkToMeshInfo> _urgentMeshQueue = [];
    private readonly HashSet<Vector3D<int>> _urgentMeshQueued = [];
    private readonly List<ChunkToMeshInfo> _lightMeshQueue = [];
    private readonly HashSet<Vector3D<int>> _lightMeshQueued = [];
    private readonly Core.Shader _chunkShader;
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

        ProcessMeshUpdates(renderParams.Camera);
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
            if (_meshGenerator.GetBestMesh(viewPos) is MeshBuildResult mesh)
            {
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

    private bool HasRenderableMesh(Vector3D<int> chunkPos) => _renderers.ContainsKey(chunkPos);

    private void RemoveFromInitialQueue(Vector3D<int> pos)
    {
        if (!_initialMeshQueued.Remove(pos))
            return;
        _initialMeshQueue.RemoveAll(c => c.Pos == pos);
    }

    private void RemoveFromUrgentQueue(Vector3D<int> pos)
    {
        if (!_urgentMeshQueued.Remove(pos))
            return;
        _urgentMeshQueue.RemoveAll(c => c.Pos == pos);
    }

    private void RemoveFromLightQueue(Vector3D<int> pos)
    {
        if (!_lightMeshQueued.Remove(pos))
            return;
        _lightMeshQueue.RemoveAll(c => c.Pos == pos);
    }

    private void PruneMeshQueuesByDistance()
    {
        _initialMeshQueue.RemoveAll(c => !IsChunkInRenderDistance(c.Pos, _lastViewPos));
        _initialMeshQueued.Clear();
        foreach (ChunkToMeshInfo c in _initialMeshQueue)
            _initialMeshQueued.Add(c.Pos);

        _urgentMeshQueue.RemoveAll(c => !IsChunkInRenderDistance(c.Pos, _lastViewPos));
        _urgentMeshQueued.Clear();
        foreach (ChunkToMeshInfo c in _urgentMeshQueue)
            _urgentMeshQueued.Add(c.Pos);

        _lightMeshQueue.RemoveAll(c => !IsChunkInRenderDistance(c.Pos, _lastViewPos));
        _lightMeshQueued.Clear();
        foreach (ChunkToMeshInfo c in _lightMeshQueue)
            _lightMeshQueued.Add(c.Pos);
    }

    private static int FindBestInFrustumIndex(List<ChunkToMeshInfo> list, Culler camera, Vector3D<double> viewPos)
    {
        int bestIndex = -1;
        double bestDist = double.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            ChunkToMeshInfo info = list[i];
            var aabb = new Box(
                info.Pos.X, info.Pos.Y, info.Pos.Z,
                info.Pos.X + SubChunkRenderer.Size,
                info.Pos.Y + SubChunkRenderer.Size,
                info.Pos.Z + SubChunkRenderer.Size
            );

            double dist = Vector3D.DistanceSquared(ToDoubleVec(info.Pos), viewPos);
            if (dist < bestDist && camera.isBoundingBoxInFrustum(aabb))
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void EnqueueInitialOrUpdate(Vector3D<int> chunkPos, long version, bool priority)
    {
        if (HasRenderableMesh(chunkPos))
        {
            RemoveFromInitialQueue(chunkPos);
            if (priority)
            {
                int idx = _urgentMeshQueue.FindIndex(c => c.Pos == chunkPos);
                if (idx >= 0)
                {
                    _urgentMeshQueue[idx] = new(chunkPos, version, true);
                    _urgentMeshQueued.Add(chunkPos);
                }
                else
                {
                    RemoveFromLightQueue(chunkPos);
                    _urgentMeshQueued.Add(chunkPos);
                    _urgentMeshQueue.Add(new(chunkPos, version, true));
                }
            }
            else
            {
                int urgentIdx = _urgentMeshQueue.FindIndex(c => c.Pos == chunkPos);
                if (urgentIdx >= 0)
                {
                    _urgentMeshQueue[urgentIdx] = new(chunkPos, version, true);
                    _urgentMeshQueued.Add(chunkPos);
                }
                else
                {
                    int lightIdx = _lightMeshQueue.FindIndex(c => c.Pos == chunkPos);
                    if (lightIdx >= 0)
                    {
                        _lightMeshQueue[lightIdx] = new(chunkPos, version, false);
                        _lightMeshQueued.Add(chunkPos);
                    }
                    else
                    {
                        _lightMeshQueued.Add(chunkPos);
                        _lightMeshQueue.Add(new(chunkPos, version, false));
                    }
                }
            }
        }
        else
        {
            RemoveFromUrgentQueue(chunkPos);
            RemoveFromLightQueue(chunkPos);
            int initialIdx = _initialMeshQueue.FindIndex(c => c.Pos == chunkPos);
            if (initialIdx >= 0)
            {
                ChunkToMeshInfo existing = _initialMeshQueue[initialIdx];
                _initialMeshQueue[initialIdx] = new(chunkPos, version, priority || existing.priority);
                _initialMeshQueued.Add(chunkPos);
            }
            else
            {
                _initialMeshQueued.Add(chunkPos);
                _initialMeshQueue.Add(new(chunkPos, version, priority));
            }
        }
    }

    private void ProcessMeshUpdates(Culler camera)
    {
        for (int n = 0; n < MaxMeshJobsPerFrame; n++)
        {
            if (_meshGenerator.ActiveTasks >= MaxMeshJobsPerFrame)
                break;

            PruneMeshQueuesByDistance();

            if (_initialMeshQueue.Count == 0 && _urgentMeshQueue.Count == 0 && _lightMeshQueue.Count == 0)
                break;

            (List<ChunkToMeshInfo> list, Action<Vector3D<int>> remove)[] order = n switch
            {
                0 => [(_urgentMeshQueue, RemoveFromUrgentQueue), (_lightMeshQueue, RemoveFromLightQueue), (_initialMeshQueue, RemoveFromInitialQueue)],
                1 => [(_lightMeshQueue, RemoveFromLightQueue), (_urgentMeshQueue, RemoveFromUrgentQueue), (_initialMeshQueue, RemoveFromInitialQueue)],
                2 => [(_urgentMeshQueue, RemoveFromUrgentQueue), (_lightMeshQueue, RemoveFromLightQueue), (_initialMeshQueue, RemoveFromInitialQueue)],
                3 => [(_initialMeshQueue, RemoveFromInitialQueue), (_urgentMeshQueue, RemoveFromUrgentQueue), (_lightMeshQueue, RemoveFromLightQueue)],
                _ => []
            };

            ChunkToMeshInfo? chosen = null;
            Action<Vector3D<int>>? removeFunc = null;

            foreach ((List<ChunkToMeshInfo> list, Action<Vector3D<int>> remove) in order)
            {
                int bestIndex = FindBestInFrustumIndex(list, camera, _lastViewPos);
                if (bestIndex >= 0)
                {
                    chosen = list[bestIndex];
                    removeFunc = remove;
                    break;
                }
            }

            if (chosen is not { } c || removeFunc is null)
                break;

            _meshGenerator.MeshChunk(_world, c.Pos, c.Version);
            removeFunc(c.Pos);
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
                    EnqueueInitialOrUpdate(state.Renderer.Position, snapshot.Value, false);
                }
            }
        }
    }

    public void Tick(Vector3D<double> viewPos)
    {
        Profiler.Start("WorldRenderer.Tick");

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

            if (chunkPos.Y < 0 || chunkPos.Y >= 128)
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

        Profiler.Start("WorldRenderer.Tick.RemoveVersions");
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
        Profiler.Stop("WorldRenderer.Tick.RemoveVersions");

        Profiler.Stop("WorldRenderer.Tick");
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
            EnqueueInitialOrUpdate(chunkPos, snapshot.Value, priority);
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

        _initialMeshQueue.Clear();
        _initialMeshQueued.Clear();
        _urgentMeshQueue.Clear();
        _urgentMeshQueued.Clear();
        _lightMeshQueue.Clear();
        _lightMeshQueued.Clear();

        foreach (ChunkMeshVersion version in _chunkVersions.Values)
        {
            version.Release();
        }

        _chunkVersions.Clear();
    }
}
