namespace BetaSharp.Client.Rendering;

public readonly record struct RendererBackendCapabilities(
    bool UsesDisplaySwapBuffers,
    bool UsesOpenGlContext)
{
    public static RendererBackendCapabilities For(RendererBackendKind backendKind)
    {
        return backendKind switch
        {
            RendererBackendKind.OpenGL => new(
                UsesDisplaySwapBuffers: true,
                UsesOpenGlContext: true),
            RendererBackendKind.Vulkan => new(
                UsesDisplaySwapBuffers: false,
                UsesOpenGlContext: false),
            _ => new(
                UsesDisplaySwapBuffers: true,
                UsesOpenGlContext: true)
        };
    }
}
