using BetaSharp.Client.Rendering;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpSceneRenderer : ISceneRenderer
{
    public void OnFrameUpdate(float tickDelta)
    {
    }

    public void Tick(float partialTicks)
    {
    }

    public void UpdateCamera()
    {
    }

    public void UpdateTargetedEntity(float tickDelta)
    {
    }

    public void ResetEquippedItemProgress()
    {
    }

    public void MarkVisibleChunksDirty()
    {
    }

    public void UpdateClouds()
    {
    }

    public void PublishRenderMetrics()
    {
    }
}
