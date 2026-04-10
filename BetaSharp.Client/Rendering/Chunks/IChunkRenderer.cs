using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Chunks;

/// <summary>
/// Backend-neutral world chunk renderer contract used by <see cref="IWorldRenderer"/>.
/// </summary>
/// <remarks>
/// Scope:
/// - chunk visibility and draw submission for opaque/translucent passes
/// - chunk mesh invalidation and rebuild scheduling
/// - chunk-level fog state inputs and basic render diagnostics
/// </remarks>
public interface IChunkRenderer : IDisposable
{
    int SectionSize { get; }
    int TotalChunks { get; }
    int ChunksInFrustum { get; }
    int ChunksOccluded { get; }
    int ChunksRendered { get; }
    int TranslucentMeshes { get; }

    void Tick(Vector3D<double> viewPosition);
    void Render(ChunkRenderParams renderParams);
    void RenderTransparent(ChunkRenderParams renderParams);
    void MarkAllVisibleChunksDirty();
    bool MarkDirty(Vector3D<int> chunkPosition, bool priority = false);
    void UpdateAllRenderers();
    void SetFogColor(float red, float green, float blue, float alpha);
    void SetFogMode(int mode);
    void SetFogDensity(float density);
    void SetFogStart(float start);
    void SetFogEnd(float end);
}
