using BetaSharp.Client.Diagnostics.GuiBackends;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Presentation;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Rendering.Backends;

internal sealed class VulkanRenderBackendRuntime : IRenderBackendRuntime
{
    public RendererBackendKind Kind => RendererBackendKind.Vulkan;

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

    public void SetMainViewport(int width, int height)
    {
        throw new NotSupportedException("Vulkan viewport binding is not implemented yet.");
    }

    public void UpdateWindow(bool processMessages)
    {
        if (processMessages)
        {
            Display.processMessages();
        }
    }

    public IRenderPresentation CreatePresentation(int width, int height, GameOptions options)
    {
        return RenderPresentationFactory.Create(Kind, width, height, options);
    }

    public IImGuiRendererBackend CreateImGuiRendererBackend()
    {
        return ImGuiRendererBackendFactory.Create(Kind);
    }
}
