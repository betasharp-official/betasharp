namespace BetaSharp.Client.Rendering.Rhi;

public readonly record struct RenderDeviceCapabilities(
    bool SupportsGraphics,
    bool SupportsTransfer,
    bool SupportsCompute,
    bool SupportsPresentation,
    bool SupportsLegacyFixedFunctionCompat);
