using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.Rendering.Presentation;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Rendering.Backends;

internal interface IRenderBackendRuntime
{
    RendererBackendKind Kind { get; }
    RendererBackendCapabilities Capabilities { get; }
    bool SupportsLegacyOpenGlRenderPath => Capabilities.SupportsLegacyOpenGlRenderPath;
    bool SupportsScreenshotCapture => Capabilities.SupportsScreenshotCapture;

    void InitializeGraphicsContext(DebugTelemetry telemetry);
    void ConfigureDefaultRenderState(GameOptions options, ILogger logger);
    void ConfigurePresentationMode(GameOptions options);
    void SetVSyncEnabled(bool enabled);
    void SetMainViewport(int width, int height);
    void PrepareFrameRenderState();
    void CheckBackendErrors(string location, ILogger logger);
    void UpdateWindow(bool processMessages);
    bool TryCaptureScreenshot(int framebufferWidth, int framebufferHeight, out byte[] rgbPixels);
    void RenderStartupScreen(
        GameOptions options,
        int displayWidth,
        int displayHeight,
        int framebufferWidth,
        int framebufferHeight,
        TextureHandle logoTexture);
    void CleanupRenderResources();
    void LogRenderResourceReport();
    ISceneRenderer CreateSceneRenderer(BetaSharp client);
    IWorldRenderer CreateWorldRenderer(BetaSharp client, TextureManager textureManager);

    IRenderPresentation CreatePresentation(int width, int height, GameOptions options);
    IImGuiRendererBackend CreateImGuiRendererBackend();
}
