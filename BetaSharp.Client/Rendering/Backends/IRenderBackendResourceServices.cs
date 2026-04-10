using BetaSharp.Client.Rendering.Core.Textures;

namespace BetaSharp.Client.Rendering.Backends;

/// <summary>
/// Backend-owned render resource bundle used during startup.
/// Keeps legacy texture/text/skin bootstrapping and dynamic texture wiring
/// behind the selected backend runtime.
/// </summary>
internal interface IRenderBackendResourceServices
{
    TextureManager TextureManager { get; }
    ITextRenderer TextRenderer { get; }
    ISkinManager SkinManager { get; }

    void ConfigureEntityRendering(BetaSharp client);
    void RegisterDynamicTextures(BetaSharp client);
}
