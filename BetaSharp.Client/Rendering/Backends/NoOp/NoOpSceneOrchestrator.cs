using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpSceneOrchestrator : ISceneOrchestrator
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

    public void ChangeWorld(World world)
    {
    }

    public void SetDamagePartialTime(float value)
    {
    }
}
