namespace BetaSharp.Client.Rendering.Rhi;

public interface IRenderDevice
{
    RenderCapabilities Capabilities { get; }
    IBuffer CreateBuffer(BufferDesc description);
    ITexture CreateTexture(TextureDesc description);
    ISampler CreateSampler(SamplerDesc description);
    IGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc description);
    ICommandEncoder CreateCommandEncoder();
}
