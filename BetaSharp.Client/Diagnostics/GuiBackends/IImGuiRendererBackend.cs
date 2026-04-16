using BetaSharp.Client.Rendering;

namespace BetaSharp.Client.Diagnostics.GuiBackends;

internal interface IImGuiRendererBackend
{
    RendererBackendKind BackendKind { get; }

    unsafe void Initialize(nint windowHandle);
    void NewFrame();
    void RenderDrawData();
}
