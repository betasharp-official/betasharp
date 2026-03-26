using System.ComponentModel;
using BetaSharp.Client.Diagnostics;

namespace BetaSharp.Client.Guis.Debug.Components;

[DisplayName("System")]
[Description("Shows info about your system.")]
public class DebugSystem : DebugComponent
{
    public DebugSystem() { }

    public override void Draw(DebugContext ctx)
    {
        DebugSystemSnapshot systemSnapshot = ctx.Game.GetDebugSystemSnapshot();
        ctx.String($"CPU: {FormatCpuInfo(systemSnapshot)}");
        ctx.String($"GPU: {systemSnapshot.GpuName} (VRAM: {systemSnapshot.GpuVram})");
        ctx.String($"OpenGL: {systemSnapshot.OpenGlVersion}");
        ctx.String($"GLSL: {systemSnapshot.GlslVersion}");
        ctx.String($"Driver: {systemSnapshot.DriverVersion}");
        ctx.String($"OS: {systemSnapshot.OsDescription}");
        ctx.String($".NET: {systemSnapshot.DotNetRuntime}");
    }

    private static string FormatCpuInfo(DebugSystemSnapshot systemSnapshot)
    {
        string coreLabel = systemSnapshot.CpuCoreCount == 1 ? "core" : "cores";
        if (systemSnapshot.CpuName == DebugTelemetry.UnknownValue)
        {
            return $"{systemSnapshot.CpuCoreCount} {coreLabel}";
        }

        return $"{systemSnapshot.CpuName} ({systemSnapshot.CpuCoreCount} {coreLabel})";
    }

    public override DebugComponent Duplicate()
    {
        return new DebugSystem()
        {
            Right = Right
        };
    }
}
