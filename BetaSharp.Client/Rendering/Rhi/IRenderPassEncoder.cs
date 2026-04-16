namespace BetaSharp.Client.Rendering.Rhi;

public interface IRenderPassEncoder : IDisposable
{
    void SetPipeline(IGraphicsPipeline pipeline);
    void Draw(int vertexCount, int instanceCount = 1, int firstVertex = 0, int firstInstance = 0);

    void DrawIndexed(int indexCount, int instanceCount = 1, int firstIndex = 0, int vertexOffset = 0,
        int firstInstance = 0);
}
