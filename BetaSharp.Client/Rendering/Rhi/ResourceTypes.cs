namespace BetaSharp.Client.Rendering.Rhi;

public readonly record struct BufferDesc(int SizeInBytes, BufferUsage Usage);

public readonly record struct TextureDesc(int Width, int Height, TextureFormat Format, TextureUsage Usage);

public readonly record struct SamplerDesc(FilterMode MinFilter, FilterMode MagFilter);

public readonly record struct GraphicsPipelineDesc(string DebugName);

public readonly record struct RenderCapabilities(bool SupportsLegacyFixedFunctionApi, bool SupportsCompute = false);

public enum BufferUsage
{
    Vertex,
    Index,
    Uniform,
    Staging
}

public enum TextureUsage
{
    Sampled,
    RenderTarget,
    DepthStencil,
    TransferSource,
    TransferDestination
}

public enum TextureFormat
{
    Rgba8Unorm,
    Bgra8Unorm,
    Depth24Stencil8
}

public enum FilterMode
{
    Nearest,
    Linear
}

public interface IBuffer
{
}

public interface ITexture
{
}

public interface ISampler
{
}

public interface IGraphicsPipeline
{
}
