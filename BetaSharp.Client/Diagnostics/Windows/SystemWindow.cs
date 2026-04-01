using ImGuiNET;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class SystemWindow(BetaSharp game) : DebugWindow
{
    public override string Title => "System";

    protected override void OnDraw()
    {
        DebugSystemSnapshot s = game.DebugSystemSnapshot;

        ImGui.Text("Build: " + BetaSharp.Version);
        ImGui.Text($"OS:     {s.OsDescription}");
        ImGui.Text($"Runtime:{s.DotNetRuntime}");

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
