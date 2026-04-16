using BetaSharp.Client.DynamicTexture;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.Rendering.Backends.OpenGL;

internal sealed class OpenGlRendererServices : IRendererServices
{
    public ITextureManager TextureManager { get; }
    public ITextRenderer TextRenderer { get; }
    public ISkinManager SkinManager { get; }
    public IEntityRenderDispatcher EntityRenderDispatcher { get; }
    public IBlockEntityRenderDispatcher BlockEntityRenderDispatcher { get; }
    public IUiRenderBackend UiRenderBackend { get; }
    public ILegacyFixedFunctionApi LegacyFixedFunctionApi { get; }

    public OpenGlRendererServices(BetaSharp client, TexturePacks texturePacks, GameOptions options)
    {
        TextureManager = new TextureManager(
            client,
            texturePacks,
            options,
            new OpenGlTextureResourceFactory(),
            new DirectTextureUploadService());
        TextRenderer = new TextRenderer(options, TextureManager);
        SkinManager = new SkinManager(TextureManager);
        EntityRenderDispatcher = global::BetaSharp.Client.Rendering.Entities.EntityRenderDispatcher.Instance;
        BlockEntityRenderDispatcher = BlockEntityRenderer.Instance;
        UiRenderBackend = new OpenGlUiRenderBackend();
        LegacyFixedFunctionApi = new OpenGlLegacyFixedFunctionApi();
    }

    public void ConfigureEntityRendering(BetaSharp client)
    {
        EntityRenderDispatcher.SkinManager = SkinManager;
        EntityRenderDispatcher.HeldItemRenderer = new HeldItemRenderer(client);
        BlockEntityRenderDispatcher.EntityDispatcher = EntityRenderDispatcher;
    }

    public void RegisterDynamicTextures(BetaSharp client)
    {
        TextureManager.AddDynamicTexture(new LavaSprite());
        TextureManager.AddDynamicTexture(new WaterSprite());
        TextureManager.AddDynamicTexture(new NetherPortalSprite());
        TextureManager.AddDynamicTexture(new CompassSprite(client));
        TextureManager.AddDynamicTexture(new ClockSprite(client));
        TextureManager.AddDynamicTexture(new WaterSideSprite());
        TextureManager.AddDynamicTexture(new LavaSideSprite());
        TextureManager.AddDynamicTexture(new FireSprite(0));
        TextureManager.AddDynamicTexture(new FireSprite(1));
    }
}
