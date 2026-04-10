using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.Rendering.Backends;

/// <summary>
/// Backend-owned render resource bundle used during startup.
/// Keeps legacy texture/text/skin bootstrapping and dynamic texture wiring
/// behind the selected backend runtime.
/// </summary>
internal interface IRenderBackendResourceServices
{
    ITextureManager TextureManager { get; }
    ITextRenderer TextRenderer { get; }
    ISkinManager SkinManager { get; }
    IEntityRenderDispatcher EntityRenderDispatcher { get; }
    IBlockEntityRenderDispatcher BlockEntityRenderDispatcher { get; }
    IUiRenderBackend UiRenderBackend { get; }

    void ConfigureEntityRendering(BetaSharp client);
    void RegisterDynamicTextures(BetaSharp client);
}
