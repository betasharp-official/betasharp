namespace BetaSharp.Client.Rendering.Chunks;

public readonly record struct ChunkRendererStats(
    int TotalChunks,
    int ChunksInFrustum,
    int ChunksOccluded,
    int ChunksRendered,
    int TranslucentMeshes);
