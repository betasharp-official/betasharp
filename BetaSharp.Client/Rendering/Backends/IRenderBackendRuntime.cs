using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Presentation;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Rendering.Backends;

internal interface IRenderBackendRuntime
{
    RendererBackendKind Kind { get; }

    void InitializeGraphicsContext(DebugTelemetry telemetry);
    void ConfigureDefaultRenderState(GameOptions options, ILogger logger);
    void ConfigurePresentationMode(GameOptions options);
    void SetVSyncEnabled(bool enabled);
    void SetMainViewport(int width, int height);
    void PrepareFrameRenderState();
    void UpdateWindow(bool processMessages);
    bool TryCaptureScreenshot(int framebufferWidth, int framebufferHeight, out byte[] rgbPixels);

    IRenderPresentation CreatePresentation(int width, int height, GameOptions options);
    IImGuiRendererBackend CreateImGuiRendererBackend();
}
