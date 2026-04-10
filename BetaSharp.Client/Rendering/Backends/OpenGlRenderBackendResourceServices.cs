using BetaSharp.Client.DynamicTexture;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.Resource.Pack;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class OpenGlRenderBackendResourceServices : IRenderBackendResourceServices
{
    public TextureManager TextureManager { get; }
    public ITextRenderer TextRenderer { get; }
    public ISkinManager SkinManager { get; }

    public OpenGlRenderBackendResourceServices(BetaSharp client, TexturePacks texturePacks, GameOptions options)
    {
        TextureManager = new TextureManager(
            client,
            texturePacks,
            options,
            new OpenGlTextureResourceFactory(),
            new DirectTextureUploadService());
        TextRenderer = new TextRenderer(options, TextureManager);
        SkinManager = new SkinManager(TextureManager);
    }

    public void ConfigureEntityRendering(BetaSharp client)
    {
        EntityRenderDispatcher.Instance.SkinManager = SkinManager;
        EntityRenderDispatcher.Instance.HeldItemRenderer = new HeldItemRenderer(client);
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
