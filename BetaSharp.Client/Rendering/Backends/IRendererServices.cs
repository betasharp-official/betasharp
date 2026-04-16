using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.Rendering.Backends;

/// <summary>
/// Backend-owned renderer service bundle used during startup.
/// </summary>
internal interface IRendererServices
{
    ITextureManager TextureManager { get; }
    ITextRenderer TextRenderer { get; }
    ISkinManager SkinManager { get; }
    IEntityRenderDispatcher EntityRenderDispatcher { get; }
    IBlockEntityRenderDispatcher BlockEntityRenderDispatcher { get; }
    IUiRenderBackend UiRenderBackend { get; }
    ILegacyFixedFunctionApi LegacyFixedFunctionApi { get; }
    void ConfigureEntityRendering(BetaSharp client);
    void RegisterDynamicTextures(BetaSharp client);
}
