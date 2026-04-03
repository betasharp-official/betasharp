using BetaSharp.Diagnostics;

namespace BetaSharp.Client.Diagnostics;

internal static class RenderMetrics
{
    public static readonly MetricHandle<int> ChunksTotal = MetricRegistry.Register<int>("render:chunks.total");
    public static readonly MetricHandle<int> ChunksFrustum = MetricRegistry.Register<int>("render:chunks.frustum");
    public static readonly MetricHandle<int> ChunksOccluded = MetricRegistry.Register<int>("render:chunks.occluded");
    public static readonly MetricHandle<int> ChunksRendered = MetricRegistry.Register<int>("render:chunks.rendered");
    public static readonly MetricHandle<float> VboAllocatedMb = MetricRegistry.Register<float>("render:vbo.mb");
    public static readonly MetricHandle<int> MeshVersionAllocated = MetricRegistry.Register<int>("render:mesh.version.allocated");
    public static readonly MetricHandle<int> MeshVersionReleased = MetricRegistry.Register<int>("render:mesh.version.released");
    public static readonly MetricHandle<int> MeshActive = MetricRegistry.Register<int>("render:mesh.active");
    public static readonly MetricHandle<int> MeshMinBytes = MetricRegistry.Register<int>("render:mesh.min_bytes");
    public static readonly MetricHandle<int> MeshMaxBytes = MetricRegistry.Register<int>("render:mesh.max_bytes");
    public static readonly MetricHandle<int> MeshAvgBytes = MetricRegistry.Register<int>("render:mesh.avg_bytes");
    public static readonly MetricHandle<int> TextureBindsLastFrame = MetricRegistry.Register<int>("render:texture.binds_last_frame");
    public static readonly MetricHandle<float> TextureAvgBinds = MetricRegistry.Register<float>("render:texture.avg_binds");
    public static readonly MetricHandle<int> TextureActive = MetricRegistry.Register<int>("render:texture.active");

    public static readonly MetricHandle<int> EntitiesRendered = MetricRegistry.Register<int>("render:entities.rendered");
    public static readonly MetricHandle<int> EntitiesHidden = MetricRegistry.Register<int>("render:entities.hidden");
    public static readonly MetricHandle<int> EntitiesTotal = MetricRegistry.Register<int>("render:entities.total");
    public static readonly MetricHandle<int> ParticlesActive = MetricRegistry.Register<int>("render:particles.active");

    static RenderMetrics() { }
}
