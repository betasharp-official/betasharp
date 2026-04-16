using BetaSharp.Client.Rendering;

namespace BetaSharp.Client.Rendering.Presentation;

public interface IFramePresenter
{
    RendererBackendKind BackendKind { get; }
    PresentationViewportImage ViewportImage { get; }
    int FramebufferWidth { get; }
    int FramebufferHeight { get; }
    bool SkipBlit { get; set; }
    void Begin();
    void End();
    void Resize(int width, int height);
}
