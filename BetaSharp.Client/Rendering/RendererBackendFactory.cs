namespace BetaSharp.Client.Rendering;

public static class RendererBackendFactory
{
    public static RendererBackendSelection Resolve(RendererBackendKind requestedBackend)
    {
        return requestedBackend switch
        {
            RendererBackendKind.OpenGL => new(requestedBackend, RendererBackendKind.OpenGL),
            RendererBackendKind.Vulkan => new(
                requestedBackend,
                RendererBackendKind.OpenGL,
                "Vulkan backend is not implemented yet; falling back to OpenGL."),
            _ => new(requestedBackend, RendererBackendKind.OpenGL, "Unknown renderer backend; falling back to OpenGL.")
        };
    }

    public static bool TryParse(string value, out RendererBackendKind backend)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "gl":
            case "opengl":
                backend = RendererBackendKind.OpenGL;
                return true;
            case "vk":
            case "vulkan":
                backend = RendererBackendKind.Vulkan;
                return true;
            default:
                backend = default;
                return false;
        }
    }
}
