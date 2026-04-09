using BetaSharp.Client.Rendering;

namespace BetaSharp.Client.Diagnostics.GuiBackends;

internal sealed class NoOpImGuiRendererBackend(RendererBackendKind backendKind) : IImGuiRendererBackend
{
    public RendererBackendKind BackendKind { get; } = backendKind;

    public unsafe void Initialize(nint windowHandle)
    {
    }

    public void NewFrame()
    {
    }

    public void RenderDrawData()
    {
    }
}
