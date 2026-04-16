using BetaSharp.Client.Rendering;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class SystemWindow(DebugWindowContext ctx) : DebugWindow
{
    public override string Title => "System";
    public override DebugDock DefaultDock => DebugDock.Right;

    protected override void OnDraw()
    {
        DebugSystemSnapshot s = ctx.DebugSystemSnapshot;
        RendererBackendStateSnapshot rendererState = ctx.RendererBackendState;

        ImGui.Text("Build: " + BetaSharp.Version);
        ImGui.Text($"OS:     {s.OsDescription}");
        ImGui.Text($"Runtime:{s.DotNetRuntime}");
        ImGui.Text($"Renderer (Requested): {rendererState.RequestedBackend}");
        ImGui.Text($"Renderer (Active):    {rendererState.ActiveBackend}");
        ImGui.Text($"Display Backend:      {rendererState.DisplayBackend}");
        ImGui.Text($"Display SwapBuffers:  {(rendererState.DisplaySupportsWindowBufferSwap ? "Yes" : "No")}");
        ImGui.Text($"Display GL Context:   {(rendererState.DisplayHasOpenGlContext ? "Yes" : "No")}");
        ImGui.Text($"ImGui Backend:        {rendererState.ImGuiBackend}");
        ImGui.Text($"Presentation Backend: {rendererState.PresentationBackend}");
        ImGui.Text($"Presentation Target:  {rendererState.PresentationTargetWidth}x{rendererState.PresentationTargetHeight}");
        ImGui.Text($"Presentation SkipBlit:{(rendererState.IsPresentationBlitSkipped ? " Yes" : " No")}");
        ImGui.Text($"Runtime Uses Swap:    {(rendererState.RuntimeCapabilities.UsesDisplaySwapBuffers ? "Yes" : "No")}");
        ImGui.Text($"Runtime Uses GL Ctx:  {(rendererState.RuntimeCapabilities.UsesOpenGlContext ? "Yes" : "No")}");
        ImGui.Text($"Renderer Runtime Init:{(rendererState.IsRuntimeInitialized ? " Yes" : " No")}");
        ImGui.Text($"Legacy GL Render Path:{(rendererState.SupportsLegacyOpenGlRenderPath ? " Yes" : " No")}");
        ImGui.Text($"Screenshot Capture:   {(rendererState.SupportsScreenshotCapture ? "Yes" : "No")}");

        if (rendererState.IsFallbackActive)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.8f, 0.35f, 1f), "Renderer fallback active");
        }

        if (!string.IsNullOrWhiteSpace(rendererState.FallbackReason))
        {
            ImGui.TextDisabled($"Fallback reason: {rendererState.FallbackReason}");
        }

        if (ImGui.CollapsingHeader("GPU", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text($"Name:       {s.GpuName}");
            ImGui.Text($"VRAM:       {s.GpuVram}");
            ImGui.Text($"OpenGL:     {s.OpenGlVersion}");
            ImGui.Text($"GLSL:       {s.GlslVersion}");
            ImGui.Text($"Driver:     {s.DriverVersion}");
        }

        if (ImGui.CollapsingHeader("CPU", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text($"Name:  {s.CpuName}");
            ImGui.Text($"Cores: {s.CpuCoreCount}");
        }
    }
}
