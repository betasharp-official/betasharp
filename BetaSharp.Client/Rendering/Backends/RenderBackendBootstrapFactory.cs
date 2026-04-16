using BetaSharp.Client.Rendering.Backends.OpenGL;
using BetaSharp.Client.Rendering.Backends.Vulkan;

namespace BetaSharp.Client.Rendering.Backends;

internal static class RenderBackendBootstrapFactory
{
    public static IRenderBackendBootstrap Create(RendererBackendKind backend)
    {
        return backend switch
        {
            RendererBackendKind.OpenGL => new OpenGlRenderBackendBootstrap(),
            RendererBackendKind.Vulkan => new VulkanRenderBackendBootstrap(),
            _ => throw new NotSupportedException($"Unsupported renderer backend: {backend}")
        };
    }
}
