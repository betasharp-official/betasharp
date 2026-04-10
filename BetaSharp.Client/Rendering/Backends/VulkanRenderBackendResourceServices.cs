using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Resource.Pack;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class VulkanRenderBackendResourceServices : IRenderBackendResourceServices
{
    public ITextureManager TextureManager { get; }
    public ITextRenderer TextRenderer { get; }
    public ISkinManager SkinManager { get; }

    public VulkanRenderBackendResourceServices(BetaSharp client, TexturePacks texturePacks, GameOptions options)
    {
        TextureManager = new TextureManager(
            client,
            texturePacks,
            options,
            new NoOpTextureResourceFactory(),
            new DirectTextureUploadService());
        TextRenderer = new NoOpTextRenderer();
        SkinManager = new NoOpSkinManager();
    }

    public void ConfigureEntityRendering(BetaSharp client)
    {
    }

    public void RegisterDynamicTextures(BetaSharp client)
    {
    }
}
