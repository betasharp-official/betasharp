using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class SystemWindow(DebugWindowContext ctx) : DebugWindow
{
    public override string Title => "System";
    public override DebugDock DefaultDock => DebugDock.Right;

    protected override void OnDraw()
    {
        DebugSystemSnapshot s = ctx.DebugSystemSnapshot;

        ImGui.Text("Build: " + BetaSharp.Version);
        ImGui.Text($"OS:     {s.OsDescription}");
        ImGui.Text($"Runtime:{s.DotNetRuntime}");
        ImGui.Text($"Renderer (Requested): {ctx.RequestedRendererBackend}");
        ImGui.Text($"Renderer (Active):    {ctx.ActiveRendererBackend}");
        ImGui.Text($"Display Backend:      {ctx.DisplayRendererBackend}");
        ImGui.Text($"ImGui Backend:        {ctx.ImGuiRendererBackend}");
        ImGui.Text($"Presentation Backend: {ctx.PresentationRendererBackend}");

        if (ctx.RequestedRendererBackend != ctx.ActiveRendererBackend)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.8f, 0.35f, 1f), "Renderer fallback active");
        }

        if (!string.IsNullOrWhiteSpace(ctx.RendererFallbackReason))
        {
            ImGui.TextDisabled($"Fallback reason: {ctx.RendererFallbackReason}");
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
