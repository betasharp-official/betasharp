using BetaSharp.Client.Rendering;

namespace BetaSharp.Client.Diagnostics.GuiBackends;

internal static class ImGuiRendererBackendFactory
{
    public static IImGuiRendererBackend Create(RendererBackendKind backend)
    {
        return backend switch
        {
            RendererBackendKind.OpenGL => new OpenGlImGuiRendererBackend(),
            RendererBackendKind.Vulkan => new NoOpImGuiRendererBackend(RendererBackendKind.Vulkan),
            _ => new NoOpImGuiRendererBackend(backend)
        };
    }
}
