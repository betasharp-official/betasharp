namespace BetaSharp.Client.Rendering;

public readonly record struct RendererBackendSelection(
    RendererBackendKind Requested,
    RendererBackendKind Effective,
    string? FallbackReason = null
);
