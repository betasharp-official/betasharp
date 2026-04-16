using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core.Textures;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Rendering.Backends.Vulkan;

internal sealed class VulkanRenderBackendBootstrap : IRenderBackendBootstrap
{
    public RendererBackendKind Kind => RendererBackendKind.Vulkan;
    public RendererBackendCapabilities Capabilities => RendererBackendCapabilities.For(Kind);
    public IRendererFactory RendererFactory => VulkanRendererFactory.Instance;

    public void InitializeGraphicsContext(DebugTelemetry telemetry)
    {
        throw new NotSupportedException("Vulkan graphics context initialization is not implemented yet.");
    }

    public void ConfigureDefaultRenderState(GameOptions options, ILogger logger)
    {
        throw new NotSupportedException("Vulkan default render-state initialization is not implemented yet.");
    }

    public void ConfigurePresentationMode(GameOptions options)
    {
        // Vulkan presentation mode (FIFO/MAILBOX/IMMEDIATE) will be configured during swapchain creation.
    }

    public void SetVSyncEnabled(bool enabled)
    {
        // TODO: map to swapchain present mode when Vulkan swapchain management is implemented.
    }

    public void SetMainViewport(int width, int height)
    {
        throw new NotSupportedException("Vulkan viewport binding is not implemented yet.");
    }

    public void PrepareFrameRenderState()
    {
        // No global state mutation in Vulkan render loops.
    }

    public void UpdateDynamicTextures(ITextureManager textureManager, bool isGamePaused)
    {
        // TODO: route dynamic texture updates through Vulkan image upload paths.
    }

    public void CheckBackendErrors(string location, ILogger logger)
    {
        // Vulkan validation output is handled by the Vulkan debug messenger when implemented.
    }

    public void UpdateWindow(bool processMessages)
    {
        if (processMessages)
        {
            Display.processMessages();
        }
    }

    public bool TryCaptureScreenshot(int framebufferWidth, int framebufferHeight, out byte[] rgbPixels)
    {
        rgbPixels = [];
        return false;
    }

    public void CleanupRenderResources()
    {
    }

    public void LogRenderResourceReport()
    {
    }

    public void RenderStartupScreen(GameOptions options, int displayWidth, int displayHeight, int framebufferWidth,
        int framebufferHeight, int splashTextureId)
    {
    }
}
