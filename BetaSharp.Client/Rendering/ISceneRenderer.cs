using BetaSharp.Worlds.Core;
namespace BetaSharp.Client.Rendering;

public interface ISceneRenderer
{
    void OnFrameUpdate(float tickDelta);
    void Tick(float partialTicks);
    void UpdateCamera();
    void UpdateTargetedEntity(float tickDelta);
    void ResetEquippedItemProgress();
    void MarkVisibleChunksDirty();
    void UpdateClouds();
    void PublishRenderMetrics();
    void ChangeWorld(World world);
}
