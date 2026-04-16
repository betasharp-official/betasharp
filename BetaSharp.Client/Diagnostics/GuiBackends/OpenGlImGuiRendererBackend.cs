using BetaSharp.Client.Rendering;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Diagnostics.GuiBackends;

internal sealed class OpenGlImGuiRendererBackend : IImGuiRendererBackend
{
    public RendererBackendKind BackendKind => RendererBackendKind.OpenGL;

    public unsafe void Initialize(nint windowHandle)
    {
        // ImGuiImplGLFW and ImGuiImplOpenGL3 are compiled into separate native DLLs,
        // each with their own GImGui context pointer. We must share the context created
        // by cimgui.dll with both backend DLLs before calling their Init functions.
        ImGuiImplGLFW.SetCurrentContext(ImGui.GetCurrentContext());
        ImGuiImplOpenGL3.SetCurrentContext(ImGui.GetCurrentContext());

        ImGuiImplGLFW.InitForOpenGL((GLFWwindow*)windowHandle, true);
        ImGuiImplOpenGL3.Init("#version 330 core");
    }

    public void NewFrame()
    {
        ImGuiImplOpenGL3.NewFrame();
        ImGuiImplGLFW.NewFrame();
    }

    public void RenderDrawData()
    {
        ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
    }
}
