using BetaSharp.Client.Options;

namespace BetaSharp.Client.Rendering.Presentation;

public static class FramePresenterFactory
{
    public static IFramePresenter Create(RendererBackendKind backend, int width, int height, GameOptions options)
    {
        return backend switch
        {
            RendererBackendKind.OpenGL => new OpenGlRenderPresentation(width, height, options),
            RendererBackendKind.Vulkan => new NoOpFramePresenter(backend, width, height),
            _ => throw new NotSupportedException($"Unsupported renderer backend for presentation: {backend}")
        };
    }
}
