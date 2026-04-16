namespace BetaSharp.Client.Rendering.Rhi;

public interface ICommandEncoder
{
    IRenderPassEncoder BeginRenderPass(RenderPassDesc description);
    ICommandBuffer Finish();
}
