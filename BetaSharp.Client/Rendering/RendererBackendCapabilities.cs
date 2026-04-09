namespace BetaSharp.Client.Rendering;

public readonly record struct RendererBackendCapabilities(
    bool UsesDisplaySwapBuffers,
    bool UsesOpenGlContext,
    bool SupportsLegacyOpenGlRenderPath,
    bool SupportsScreenshotCapture)
{
    public static RendererBackendCapabilities For(RendererBackendKind backendKind)
    {
        return backendKind switch
        {
            RendererBackendKind.OpenGL => new(
                UsesDisplaySwapBuffers: true,
                UsesOpenGlContext: true,
                SupportsLegacyOpenGlRenderPath: true,
                SupportsScreenshotCapture: true),
            RendererBackendKind.Vulkan => new(
                UsesDisplaySwapBuffers: false,
                UsesOpenGlContext: false,
                SupportsLegacyOpenGlRenderPath: false,
                SupportsScreenshotCapture: false),
            _ => new(
                UsesDisplaySwapBuffers: true,
                UsesOpenGlContext: true,
                SupportsLegacyOpenGlRenderPath: true,
                SupportsScreenshotCapture: true)
        };
    }
}
