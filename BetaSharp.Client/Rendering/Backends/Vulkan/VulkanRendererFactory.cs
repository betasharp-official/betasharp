using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Presentation;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Backends.Vulkan;

internal sealed class VulkanRendererFactory : IRendererFactory
{
    public static VulkanRendererFactory Instance { get; } = new();

    private VulkanRendererFactory()
    {
    }

    public IRendererServices CreateServices(BetaSharp client, TexturePacks texturePacks, GameOptions options)
        => new VulkanRendererServices(client, texturePacks, options);

    public ILoadingScreenRenderer CreateLoadingScreenRenderer(BetaSharp client)
        => new NoOpLoadingScreenRenderer(client);

    public ISceneOrchestrator CreateSceneOrchestrator(BetaSharp client)
        => new NoOpSceneOrchestrator();

    public IWorldRenderer CreateWorldRenderer(BetaSharp client, ITextureManager textureManager)
        => new NoOpWorldRenderer();

    public IParticleManager CreateParticleManager(World? world, ITextureManager textureManager)
        => new NoOpParticleManager();

    public IFramePresenter CreateFramePresenter(int width, int height, GameOptions options)
        => FramePresenterFactory.Create(RendererBackendKind.Vulkan, width, height, options);

    public IImGuiRendererBackend CreateImGuiRendererBackend()
        => ImGuiRendererBackendFactory.Create(RendererBackendKind.Vulkan);
}
