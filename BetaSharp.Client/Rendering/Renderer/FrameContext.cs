using BetaSharp.Client.Rendering.Legacy;

namespace BetaSharp.Client.Rendering;

/// <summary>
/// Explicit per-frame render dependencies passed into legacy scene systems.
/// This replaces the global current-backend locator.
/// </summary>
public sealed class FrameContext
{
    public FrameContext(ILegacyFixedFunctionApi legacyFixedFunctionApi)
    {
        LegacyFixedFunctionApi = legacyFixedFunctionApi;
    }

    public ILegacyFixedFunctionApi LegacyFixedFunctionApi { get; }
}
