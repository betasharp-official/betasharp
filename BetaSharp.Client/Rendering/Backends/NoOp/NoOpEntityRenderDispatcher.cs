using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class NoOpEntityRenderDispatcher : IEntityRenderDispatcher
{
    private sealed class NoOpEntityRendererImpl : EntityRenderer
    {
        public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
        {
        }
    }

    private readonly NoOpEntityRendererImpl _renderer;
    private static readonly NoOpTextRenderer s_textRenderer = new();

    public NoOpEntityRenderDispatcher()
    {
        _renderer = new NoOpEntityRendererImpl
        {
            Dispatcher = this
        };
    }

    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double OffsetZ { get; set; }
    public ITextureManager TextureManager => null!;
    public ISkinManager SkinManager { get; set; } = null!;
    public IHeldItemRenderer HeldItemRenderer { get; set; } = new NoOpHeldItemRenderer();
    public ILegacyFixedFunctionApi SceneRenderBackend { get; } = new NoOpLegacyFixedFunctionApi();
    public World World { get; set; } = null!;
    public EntityLiving CameraEntity => null!;
    public float PlayerViewY { get; set; }
    public float PlayerViewX => 0.0f;
    public GameOptions Options => null!;

    public EntityRenderer GetEntityRenderObject(Entity entity) => _renderer;

    public void CacheRenderInfo(World world, ITextureManager textureManager, ITextRenderer textRenderer,
        EntityLiving camera, GameOptions options, ILegacyFixedFunctionApi sceneRenderBackend, float tickDelta)
    {
    }

    public void RenderEntity(Entity target, float tickDelta)
    {
    }

    public void RenderEntityWithPosYaw(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
    }

    public double GetSquareDistanceTo(double x, double y, double z) => 0.0;

    public ITextRenderer GetTextRenderer() => s_textRenderer;
}
