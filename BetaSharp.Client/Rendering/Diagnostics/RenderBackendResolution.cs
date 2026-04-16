namespace BetaSharp.Client.Rendering;

public readonly record struct RenderBackendResolution(
    RendererBackendKind RequestedBackend,
    RendererBackendKind ActiveBackend,
    string? FallbackReason);
