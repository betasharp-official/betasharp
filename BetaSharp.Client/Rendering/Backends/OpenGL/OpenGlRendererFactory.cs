using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Chunks;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Presentation;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Backends.OpenGL;

internal sealed class OpenGlRendererFactory : IRendererFactory
{
    public static OpenGlRendererFactory Instance { get; } = new();

    private OpenGlRendererFactory()
    {
    }

    public IRendererServices CreateServices(BetaSharp client, TexturePacks texturePacks, GameOptions options)
        => new OpenGlRendererServices(client, texturePacks, options);

    public ILoadingScreenRenderer CreateLoadingScreenRenderer(BetaSharp client)
        => new LoadingScreenRenderer(client);

    public ISceneOrchestrator CreateSceneOrchestrator(BetaSharp client)
        => new GameRenderer(client);

    public IWorldRenderer CreateWorldRenderer(BetaSharp client, ITextureManager textureManager)
    {
        IChunkRendererFactory chunkRendererFactory = new OpenGlChunkRendererFactory();
        return new WorldRenderer(client, textureManager, chunkRendererFactory);
    }

    public IParticleManager CreateParticleManager(World? world, ITextureManager textureManager)
        => new ParticleManager(world!, textureManager);

    public IFramePresenter CreateFramePresenter(int width, int height, GameOptions options)
        => FramePresenterFactory.Create(RendererBackendKind.OpenGL, width, height, options);

    public IImGuiRendererBackend CreateImGuiRendererBackend()
        => ImGuiRendererBackendFactory.Create(RendererBackendKind.OpenGL);
}
