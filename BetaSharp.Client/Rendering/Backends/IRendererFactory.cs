using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Presentation;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Backends;

internal interface IRendererFactory
{
    IRendererServices CreateServices(BetaSharp client, TexturePacks texturePacks, GameOptions options);
    ILoadingScreenRenderer CreateLoadingScreenRenderer(BetaSharp client);
    ISceneOrchestrator CreateSceneOrchestrator(BetaSharp client);
    IWorldRenderer CreateWorldRenderer(BetaSharp client, ITextureManager textureManager);
    IParticleManager CreateParticleManager(World? world, ITextureManager textureManager);
    IFramePresenter CreateFramePresenter(int width, int height, GameOptions options);
    IImGuiRendererBackend CreateImGuiRendererBackend();
}
