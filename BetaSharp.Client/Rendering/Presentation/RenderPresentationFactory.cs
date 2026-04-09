using BetaSharp.Client.Options;

namespace BetaSharp.Client.Rendering.Presentation;

public static class RenderPresentationFactory
{
    public static IRenderPresentation Create(
        RendererBackendKind backend,
        int width,
        int height,
        GameOptions options)
    {
        return backend switch
        {
            RendererBackendKind.OpenGL => new OpenGlRenderPresentation(width, height, options),
            RendererBackendKind.Vulkan => new NoOpRenderPresentation(backend, width, height),
            _ => throw new NotSupportedException(
                $"Unsupported renderer backend for presentation: {backend}")
        };
    }
}
