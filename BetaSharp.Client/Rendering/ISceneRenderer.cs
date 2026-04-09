namespace BetaSharp.Client.Rendering;

public interface ISceneRenderer
{
    void OnFrameUpdate(float tickDelta);
    void Tick(float partialTicks);
    void UpdateCamera();
    void UpdateTargetedEntity(float tickDelta);
    void ResetEquippedItemProgress();
}
