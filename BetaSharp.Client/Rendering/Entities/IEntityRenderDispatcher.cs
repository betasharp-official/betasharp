using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Entities;

/// <summary>
/// Backend-facing entity render dispatcher surface used by world/UI render orchestration.
/// </summary>
public interface IEntityRenderDispatcher
{
    double OffsetX { get; set; }
    double OffsetY { get; set; }
    double OffsetZ { get; set; }

    ITextureManager TextureManager { get; }
    ISkinManager SkinManager { get; set; }
    IHeldItemRenderer HeldItemRenderer { get; set; }
    ILegacyFixedFunctionApi SceneRenderBackend { get; }
    World World { get; set; }
    EntityLiving CameraEntity { get; }
    float PlayerViewY { get; set; }
    float PlayerViewX { get; }
    GameOptions Options { get; }

    EntityRenderer GetEntityRenderObject(Entity entity);

    void CacheRenderInfo(World world, ITextureManager textureManager, ITextRenderer textRenderer, EntityLiving camera,
        GameOptions options, ILegacyFixedFunctionApi sceneRenderBackend, float tickDelta);

    void RenderEntity(Entity target, float tickDelta);
    void RenderEntityWithPosYaw(Entity target, double x, double y, double z, float yaw, float tickDelta);
    double GetSquareDistanceTo(double x, double y, double z);
    ITextRenderer GetTextRenderer();
}
