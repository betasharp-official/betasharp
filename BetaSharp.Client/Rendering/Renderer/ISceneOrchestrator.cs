using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering;

/// <summary>
/// Owns per-frame scene orchestration for the active renderer.
/// </summary>
/// <remarks>
/// Scope:
/// - frame lifecycle coordination (camera update, frame update, and world-change hooks)
/// - high-level render-side gameplay integration (targeting, equipped-item resets, cloud updates)
/// - render metric publication and scene-level invalidation requests
/// </remarks>
public interface ISceneOrchestrator
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
    void SetDamagePartialTime(float value);
}
