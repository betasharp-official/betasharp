namespace BetaSharp.Client.Rendering.Presentation;

/// <summary>
/// Presentation fallback used when no GPU-backed presentation path is available.
/// </summary>
public sealed class NoOpFramePresenter : IFramePresenter
{
    public RendererBackendKind BackendKind { get; }
    public PresentationViewportImage ViewportImage => PresentationViewportImage.Empty;
    public int FramebufferWidth { get; private set; }
    public int FramebufferHeight { get; private set; }
    public bool SkipBlit { get; set; }

    public NoOpFramePresenter(RendererBackendKind backendKind, int width = 1, int height = 1)
    {
        BackendKind = backendKind;
        FramebufferWidth = width > 0 ? width : 1;
        FramebufferHeight = height > 0 ? height : 1;
    }

    public void Begin()
    {
    }

    public void End()
    {
    }

    public void Resize(int width, int height)
    {
        if (width > 0)
        {
            FramebufferWidth = width;
        }

        if (height > 0)
        {
            FramebufferHeight = height;
        }
    }
}
