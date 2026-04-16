namespace BetaSharp.Client.Rendering;

public readonly record struct RendererBackendStateSnapshot(
    RendererBackendKind RequestedBackend,
    RendererBackendKind ActiveBackend,
    RendererBackendKind DisplayBackend,
    RendererBackendKind ImGuiBackend,
    RendererBackendKind PresentationBackend,
    RendererBackendCapabilities RuntimeCapabilities,
    bool IsRuntimeInitialized,
    bool DisplaySupportsWindowBufferSwap,
    bool DisplayHasOpenGlContext,
    int PresentationTargetWidth,
    int PresentationTargetHeight,
    bool IsPresentationBlitSkipped,
    string? FallbackReason)
{
    public bool IsFallbackActive => RequestedBackend != ActiveBackend;
    public bool SupportsLegacyOpenGlRenderPath => RuntimeCapabilities.SupportsLegacyOpenGlRenderPath;
    public bool SupportsScreenshotCapture => RuntimeCapabilities.SupportsScreenshotCapture;
}
