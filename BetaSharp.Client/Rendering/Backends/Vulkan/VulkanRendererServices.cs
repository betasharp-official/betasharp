using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.Rendering.Backends.Vulkan;

internal sealed class VulkanRendererServices : IRendererServices
{
    public ITextureManager TextureManager { get; }
    public ITextRenderer TextRenderer { get; }
    public ISkinManager SkinManager { get; }
    public IEntityRenderDispatcher EntityRenderDispatcher { get; }
    public IBlockEntityRenderDispatcher BlockEntityRenderDispatcher { get; }
    public IUiRenderBackend UiRenderBackend { get; }
    public ILegacyFixedFunctionApi LegacyFixedFunctionApi { get; }

    public VulkanRendererServices(BetaSharp client, TexturePacks texturePacks, GameOptions options)
    {
        TextureManager = new TextureManager(
            client,
            texturePacks,
            options,
            new NoOpTextureResourceFactory(),
            new DirectTextureUploadService());
        TextRenderer = new NoOpTextRenderer();
        SkinManager = new NoOpSkinManager();
        EntityRenderDispatcher = new NoOpEntityRenderDispatcher();
        BlockEntityRenderDispatcher = new NoOpBlockEntityRenderDispatcher
        {
            EntityDispatcher = EntityRenderDispatcher
        };
        UiRenderBackend = new NoOpUiRenderBackend();
        LegacyFixedFunctionApi = new NoOpLegacyFixedFunctionApi();
    }

    public void ConfigureEntityRendering(BetaSharp client)
    {
    }

    public void RegisterDynamicTextures(BetaSharp client)
    {
    }
}
