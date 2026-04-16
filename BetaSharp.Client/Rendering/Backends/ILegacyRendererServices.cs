using BetaSharp.Client;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Blocks.Entities;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.Rendering.Backends;

/// <summary>
/// Transitional startup bundle used while the client is still migrating off the legacy fixed-function path.
/// The bundle keeps legacy resource/bootstrap concerns together, but names the OpenGL compatibility surface
/// explicitly so it does not look backend-neutral.
/// </summary>
internal interface ILegacyRendererServices
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
