using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core.Textures;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Rendering.Backends;

internal interface IRenderBackendBootstrap
{
    RendererBackendKind Kind { get; }
    RendererBackendCapabilities Capabilities { get; }
    IRendererFactory RendererFactory { get; }
    void InitializeGraphicsContext(DebugTelemetry telemetry);
    void ConfigureDefaultRenderState(GameOptions options, ILogger logger);
    void ConfigurePresentationMode(GameOptions options);
    void SetVSyncEnabled(bool enabled);
    void SetMainViewport(int width, int height);
    void PrepareFrameRenderState();
    void UpdateDynamicTextures(ITextureManager textureManager, bool isGamePaused);
    void CheckBackendErrors(string location, ILogger logger);
    void UpdateWindow(bool processMessages);
    bool TryCaptureScreenshot(int framebufferWidth, int framebufferHeight, out byte[] rgbPixels);
    void CleanupRenderResources();
    void LogRenderResourceReport();

    void RenderStartupScreen(GameOptions options, int displayWidth, int displayHeight, int framebufferWidth,
        int framebufferHeight, int splashTextureId);
}
